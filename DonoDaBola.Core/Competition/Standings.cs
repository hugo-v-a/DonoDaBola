namespace DonoDaBola.Core.Competition;

// Zonas fixas da tabela (REC 2026, Art. 6º a 8º), sem repasse de vagas por Copa do Brasil ou título continental.
public enum Zone
{
    None,
    Libertadores,            // 1º ao 4º, fase de grupos
    LibertadoresPreliminary, // 5º, fase preliminar
    SulAmericana,            // 6º ao 11º
    Relegation,              // 17º ao 20º
}

public record StandingRow(
    int Position, Club Club, int Played, int Won, int Drawn, int Lost,
    int GoalsFor, int GoalsAgainst, int Points, Zone Zone)
{
    public int GoalDifference => GoalsFor - GoalsAgainst;
}

// O que um critério de desempate enxerga: os clubes ainda empatados, os jogos já disputados e as chaves de sorteio.
public record TieContext(IReadOnlyList<StandingRow> Tied, IReadOnlyList<SeasonMatch> Played, IReadOnlyDictionary<string, int> DrawKeys);

// Critério de desempate: a maior chave fica à frente.
public record TieBreaker(string Name, Func<StandingRow, TieContext, int> Key);

public static class Standings
{
    public const int Win = 3, Draw = 1;

    // Ordem do Art. 15 do REC 2026. Os critérios 5 e 6 (menos cartões vermelhos e amarelos) entram antes do
    // sorteio quando a partida tiver cartões (M2): uma linha cada, sem mudar o resto.
    public static readonly IReadOnlyList<TieBreaker> TieBreakers =
    [
        new("Vitórias", (r, _) => r.Won),
        new("Saldo de gols", (r, _) => r.GoalDifference),
        new("Gols pró", (r, _) => r.GoalsFor),
        // §1º: pontos e depois saldo nos jogos entre os dois; §2º: com mais de 2 empatados, não vale.
        new("Confronto direto (pontos)", (r, c) => c.Tied.Count == 2 ? HeadToHead(r, c).Points : 0),
        new("Confronto direto (saldo)", (r, c) => c.Tied.Count == 2 ? HeadToHead(r, c).GoalDifference : 0),
        new("Sorteio", (r, c) => -c.DrawKeys[r.Club.Id]), // chave guardada na temporada: menor fica à frente
    ];

    public static Zone ZoneOf(int position) => position switch
    {
        <= 4 => Zone.Libertadores,
        5 => Zone.LibertadoresPreliminary,
        <= 11 => Zone.SulAmericana,
        >= 17 => Zone.Relegation,
        _ => Zone.None,
    };

    // Só os jogos com placar contam (no meio da temporada, inclusive no confronto direto).
    public static IReadOnlyList<StandingRow> Compute(IEnumerable<Club> clubs, IReadOnlyList<SeasonMatch> matches, IReadOnlyDictionary<string, int> drawKeys)
    {
        var played = matches.Where(m => m.HomeGoals is not null).ToList();
        var rows = clubs.Select(c => Totals(c, played)).ToList();
        return rows
            .GroupBy(r => r.Points)
            .OrderByDescending(g => g.Key)
            .SelectMany(g => Order(g.ToList(), 0, played, drawKeys))
            .Select((r, i) => r with { Position = i + 1, Zone = ZoneOf(i + 1) })
            .ToList();
    }

    // Reparte os empatados critério a critério; cada critério só vê quem continua empatado.
    private static IEnumerable<StandingRow> Order(List<StandingRow> tied, int criterion, List<SeasonMatch> played, IReadOnlyDictionary<string, int> drawKeys)
    {
        if (tied.Count == 1 || criterion == TieBreakers.Count)
            return tied;
        var context = new TieContext(tied, played, drawKeys);
        return tied
            .GroupBy(r => TieBreakers[criterion].Key(r, context))
            .OrderByDescending(g => g.Key)
            .SelectMany(g => Order(g.ToList(), criterion + 1, played, drawKeys));
    }

    private static StandingRow Totals(Club club, IEnumerable<SeasonMatch> played)
    {
        int won = 0, drawn = 0, lost = 0, goalsFor = 0, goalsAgainst = 0;
        foreach (var m in played)
        {
            if (m.Home.Id != club.Id && m.Away.Id != club.Id) continue;
            var (mine, theirs) = m.Home.Id == club.Id ? (m.HomeGoals!.Value, m.AwayGoals!.Value) : (m.AwayGoals!.Value, m.HomeGoals!.Value);
            goalsFor += mine;
            goalsAgainst += theirs;
            if (mine > theirs) won++; else if (mine == theirs) drawn++; else lost++;
        }
        return new(0, club, won + drawn + lost, won, drawn, lost, goalsFor, goalsAgainst, won * Win + drawn * Draw, Zone.None);
    }

    private static (int Points, int GoalDifference) HeadToHead(StandingRow row, TieContext context)
    {
        var other = context.Tied.Single(r => r.Club.Id != row.Club.Id).Club.Id;
        var duel = context.Played.Where(m => (m.Home.Id, m.Away.Id) == (row.Club.Id, other) || (m.Home.Id, m.Away.Id) == (other, row.Club.Id));
        var h = Totals(row.Club, duel);
        return (h.Points, h.GoalDifference);
    }
}
