namespace DonoDaBola.Core.Simulation;

// Opções da partida. Sem nada, a partida é a mesma do M1: escalação automática e sem cartões.
public sealed record MatchOptions
{
    public Lineup? HomeLineup { get; init; }
    public Lineup? AwayLineup { get; init; }
    public DisciplineRules Discipline { get; init; } = DisciplineRules.Off;

    // Sequência própria dos cartões: não toca no Random dos gols. Sem ela, não há cartões.
    public Random? Cards { get; init; }
}

// Simulação por eventos (GDD §4.4): a cada minuto, posse → chance → finalização → gol, defesa ou fora;
// depois, a disciplina de cada time (amarelo e vermelho direto), com a sequência de cartões.
public static class MatchEngine
{
    // Pontos de calibração; o ajuste fino com médias reais é do M4.
    public const double HomeAdvantage = 1.1;    // multiplica meio e ataque do mandante
    public const double ChanceRate = 0.25;      // chance por minuto quando ataque = defesa adversária, ×2
    public const double BaseOnTarget = 0.4;     // chance de acertar o alvo em duelo equilibrado
    public const double OnTargetSpread = 0.3;   // quanto a diferença finalização × defesa mexe no acerto
    public const double GoalFactor = 1.1;       // no alvo: gol = GoalFactor × finalização / (finalização + goleiro)
    public const double RedCardStrength = 0.85; // cada expulsão multiplica defesa, meio e ataque do time
    public const int Minutes = 90;

    private sealed class Side(Club club, Lineup lineup, double defense, double midfield, double attack, double keeper)
    {
        public Club Club { get; } = club;
        public Lineup Lineup { get; } = lineup;
        public double Defense { get; set; } = defense;
        public double Midfield { get; set; } = midfield;
        public double Attack { get; set; } = attack;
        public double Keeper { get; } = keeper;
        public HashSet<Player> SentOff { get; } = [];
        public HashSet<Player> Booked { get; } = [];
    }

    public static MatchResult Simulate(Club home, Club away, Random random) => Simulate(home, away, random, new MatchOptions());

    public static MatchResult Simulate(Club home, Club away, Random random, MatchOptions options)
    {
        var h = CreateSide(home, options.HomeLineup ?? Lineup.Pick442(home), HomeAdvantage);
        var a = CreateSide(away, options.AwayLineup ?? Lineup.Pick442(away), 1.0);
        var events = new List<MatchEvent>();
        int homeGoals = 0, awayGoals = 0;

        for (var minute = 1; minute <= Minutes; minute++)
        {
            var (attack, defense) = random.NextDouble() < h.Midfield / (h.Midfield + a.Midfield) ? (h, a) : (a, h);
            if (random.NextDouble() < ChanceRate * attack.Attack / (attack.Attack + defense.Defense))
            {
                var shooter = PickShooter(attack, random);
                double finishing = shooter.Technical.Finishing;
                var onTarget = BaseOnTarget + OnTargetSpread * (finishing / (finishing + defense.Defense) - 0.5);
                var type = random.NextDouble() >= onTarget ? MatchEventType.Miss
                    : random.NextDouble() < GoalFactor * finishing / (finishing + defense.Keeper) ? MatchEventType.Goal
                    : MatchEventType.Save;

                events.Add(new(minute, type, attack.Club, shooter, defense.Lineup.Goalkeeper));
                if (type == MatchEventType.Goal && attack == h) homeGoals++;
                else if (type == MatchEventType.Goal) awayGoals++;
            }

            if (options.Cards is { } cards)
            {
                Book(h, minute, cards, options.Discipline, events);
                Book(a, minute, cards, options.Discipline, events);
            }
        }

        return new(home, away, h.Lineup, a.Lineup, homeGoals, awayGoals, events);
    }

    private static Side CreateSide(Club club, Lineup lineup, double advantage)
    {
        double Sector(IEnumerable<Player> players, Position position) => players.Average(p => Lineup.Rate(p, position));
        return new(club, lineup,
            Sector(lineup.Defenders, Position.Defender),
            Sector(lineup.Midfielders, Position.Midfielder) * advantage,
            Sector(lineup.Forwards, Position.Forward) * advantage,
            Lineup.Rate(lineup.Goalkeeper, Position.Goalkeeper));
    }

    // Um sorteio de amarelo e um de vermelho direto por time e minuto. Quem recebe: peso agressividade × função.
    // ponytail: goleiro só leva amarelo (sem substituições não há quem ir para o gol); revisar no M3.
    private static void Book(Side side, int minute, Random cards, DisciplineRules rules, List<MatchEvent> events)
    {
        if (cards.NextDouble() < rules.YellowPerTeamMinute && Pick(side, cards, keepers: true, rules.BookedCaution) is { } booked)
        {
            if (side.Booked.Add(booked))
                events.Add(new(minute, MatchEventType.YellowCard, side.Club, booked, null));
            else
                SendOff(side, booked, minute, MatchEventType.SecondYellow, events);
        }
        if (cards.NextDouble() < rules.DirectRedPerTeamMinute && Pick(side, cards, keepers: false, 1.0) is { } red)
            SendOff(side, red, minute, MatchEventType.RedCard, events);
    }

    private static void SendOff(Side side, Player player, int minute, MatchEventType type, List<MatchEvent> events)
    {
        events.Add(new(minute, type, side.Club, player, null));
        side.SentOff.Add(player);
        side.Defense *= RedCardStrength;
        side.Midfield *= RedCardStrength;
        side.Attack *= RedCardStrength;
    }

    private static Player? Pick(Side side, Random random, bool keepers, double bookedWeight)
    {
        var lineup = side.Lineup;
        // Goleiro já advertido sai do sorteio: um 2º amarelo o expulsaria sem ter quem ir para o gol.
        var keeper = keepers && !side.Booked.Contains(lineup.Goalkeeper);
        var candidates = (keeper ? [(lineup.Goalkeeper, 0.2)] : Array.Empty<(Player, double)>())
            .Concat(lineup.Defenders.Select(p => (p, 1.5)))
            .Concat(lineup.Midfielders.Select(p => (p, 1.2)))
            .Concat(lineup.Forwards.Select(p => (p, 0.8)))
            .Where(c => !side.SentOff.Contains(c.Item1))
            .Select(c => (Player: c.Item1, Weight: c.Item2 * c.Item1.Mental.Aggression * (side.Booked.Contains(c.Item1) ? bookedWeight : 1)))
            .ToList();
        return candidates.Count == 0 ? null : Weighted(candidates, random);
    }

    // Sorteio ponderado pela função na escalação (atacante 3, meio 1,5, defensor 0,5) × finalização.
    // Expulsos não finalizam (sem expulsão, a lista e o sorteio são os mesmos do M1).
    private static Player PickShooter(Side side, Random random)
    {
        var lineup = side.Lineup;
        var candidates = lineup.Forwards.Select(p => (p, w: 3.0))
            .Concat(lineup.Midfielders.Select(p => (p, w: 1.5)))
            .Concat(lineup.Defenders.Select(p => (p, w: 0.5)))
            .Where(c => !side.SentOff.Contains(c.p))
            .Select(c => (Player: c.p, Weight: c.w * c.p.Technical.Finishing))
            .ToList();
        return Weighted(candidates, random);
    }

    // ponytail: lista vazia (10 expulsos) lançaria aqui; na prática não acontece.
    private static Player Weighted(List<(Player Player, double Weight)> candidates, Random random)
    {
        var roll = random.NextDouble() * candidates.Sum(c => c.Weight);
        foreach (var (player, weight) in candidates)
            if ((roll -= weight) < 0)
                return player;
        return candidates[^1].Player;
    }
}
