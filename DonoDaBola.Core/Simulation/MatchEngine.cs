namespace DonoDaBola.Core.Simulation;

// Simulação por eventos (GDD §4.4): a cada minuto, posse → chance → finalização → gol, defesa ou fora.
public static class MatchEngine
{
    // Pontos de calibração; o ajuste fino com médias reais é do M4.
    public const double HomeAdvantage = 1.1;    // multiplica meio e ataque do mandante
    public const double ChanceRate = 0.25;      // chance por minuto quando ataque = defesa adversária, ×2
    public const double BaseOnTarget = 0.4;     // chance de acertar o alvo em duelo equilibrado
    public const double OnTargetSpread = 0.3;   // quanto a diferença finalização × defesa mexe no acerto
    public const double GoalFactor = 1.1;       // no alvo: gol = GoalFactor × finalização / (finalização + goleiro)
    public const int Minutes = 90;

    private sealed record Side(Club Club, Lineup Lineup, double Defense, double Midfield, double Attack, double Keeper);

    public static MatchResult Simulate(Club home, Club away, Random random)
    {
        var h = CreateSide(home, HomeAdvantage);
        var a = CreateSide(away, 1.0);
        var events = new List<MatchEvent>();
        int homeGoals = 0, awayGoals = 0;

        for (var minute = 1; minute <= Minutes; minute++)
        {
            var (attack, defense) = random.NextDouble() < h.Midfield / (h.Midfield + a.Midfield) ? (h, a) : (a, h);
            if (random.NextDouble() >= ChanceRate * attack.Attack / (attack.Attack + defense.Defense))
                continue;

            var shooter = PickShooter(attack.Lineup, random);
            double finishing = shooter.Technical.Finishing;
            var onTarget = BaseOnTarget + OnTargetSpread * (finishing / (finishing + defense.Defense) - 0.5);
            var type = random.NextDouble() >= onTarget ? MatchEventType.Miss
                : random.NextDouble() < GoalFactor * finishing / (finishing + defense.Keeper) ? MatchEventType.Goal
                : MatchEventType.Save;

            events.Add(new(minute, type, attack.Club, shooter, defense.Lineup.Goalkeeper));
            if (type == MatchEventType.Goal && attack == h) homeGoals++;
            else if (type == MatchEventType.Goal) awayGoals++;
        }

        return new(home, away, h.Lineup, a.Lineup, homeGoals, awayGoals, events);
    }

    private static Side CreateSide(Club club, double advantage)
    {
        var lineup = Lineup.Pick442(club);
        double Sector(IEnumerable<Player> players, Position position) => players.Average(p => Lineup.Rate(p, position));
        return new(club, lineup,
            Sector(lineup.Defenders, Position.Defender),
            Sector(lineup.Midfielders, Position.Midfielder) * advantage,
            Sector(lineup.Forwards, Position.Forward) * advantage,
            Lineup.Rate(lineup.Goalkeeper, Position.Goalkeeper));
    }

    // Sorteio ponderado pela função na escalação (atacante 3, meio 1,5, defensor 0,5) × finalização.
    private static Player PickShooter(Lineup lineup, Random random)
    {
        var candidates = lineup.Forwards.Select(p => (p, w: 3.0))
            .Concat(lineup.Midfielders.Select(p => (p, w: 1.5)))
            .Concat(lineup.Defenders.Select(p => (p, w: 0.5)))
            .Select(c => (c.p, w: c.w * c.p.Technical.Finishing))
            .ToList();

        var roll = random.NextDouble() * candidates.Sum(c => c.w);
        foreach (var (player, weight) in candidates)
            if ((roll -= weight) < 0)
                return player;
        return candidates[^1].p;
    }
}
