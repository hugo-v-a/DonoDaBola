using DonoDaBola.Core.Competition;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Tests;

// Desempate do Art. 15 do regulamento: cada caso empata nos critérios anteriores e decide no critério testado.
public class StandingsTests
{
    private static readonly Dictionary<string, Club> Clubs =
        new[] { "a", "b", "c", "d" }.ToDictionary(id => id, id => TestPlayers.CreateClub(id));

    // Chave de sorteio: menor fica à frente. Por padrão, a ordem alfabética invertida (d, c, b, a),
    // para o sorteio nunca coincidir por acaso com a ordem esperada pelos outros critérios.
    private static readonly Dictionary<string, int> Keys = new() { ["d"] = 0, ["c"] = 1, ["b"] = 2, ["a"] = 3 };

    // Chaves com a à frente, para os casos em que a ordem padrão coincidiria com a esperada.
    private static readonly Dictionary<string, int> AFirst = new() { ["a"] = 0, ["b"] = 1, ["c"] = 2, ["d"] = 3 };

    private static SeasonMatch M(string home, string away, int? hg, int? ag, int round = 1) =>
        new(round, 0, new DateOnly(2026, 1, 28), Clubs[home], Clubs[away], hg, ag);

    private static string[] Order(IEnumerable<SeasonMatch> matches, string[]? only = null, Dictionary<string, int>? keys = null, IReadOnlyList<SeasonCard>? cards = null) =>
        Standings.Compute((only ?? ["a", "b", "c", "d"]).Select(id => Clubs[id]), matches.ToList(), keys ?? Keys, cards)
            .Select(r => r.Club.Id).ToArray();

    private static SeasonCard Card(string club, MatchEventType type, int round = 1) => new(round, 0, 30, club, $"{club}-x", type);

    [Fact]
    public void Points_ThreeOneZero_AndTotals()
    {
        var rows = Standings.Compute(Clubs.Values, [M("a", "b", 2, 0), M("c", "d", 1, 1), M("b", "c", 0, 3)], Keys);
        var a = rows.Single(r => r.Club.Id == "a");
        var c = rows.Single(r => r.Club.Id == "c");
        var b = rows.Single(r => r.Club.Id == "b");
        Assert.Equal((1, 1, 0, 0, 2, 0, 2, 3), (a.Played, a.Won, a.Drawn, a.Lost, a.GoalsFor, a.GoalsAgainst, a.GoalDifference, a.Points));
        Assert.Equal((2, 1, 1, 0, 4, 1, 3, 4), (c.Played, c.Won, c.Drawn, c.Lost, c.GoalsFor, c.GoalsAgainst, c.GoalDifference, c.Points));
        Assert.Equal((2, 0, 0, 2, 0, 5, -5, 0), (b.Played, b.Won, b.Drawn, b.Lost, b.GoalsFor, b.GoalsAgainst, b.GoalDifference, b.Points));
        Assert.Equal(["c", "a", "d", "b"], rows.Select(r => r.Club.Id));
        Assert.Equal([1, 2, 3, 4], rows.Select(r => r.Position));
    }

    [Fact]
    public void Matches_NotPlayed_AreIgnored()
    {
        var rows = Standings.Compute(Clubs.Values, [M("a", "b", null, null)], Keys);
        Assert.All(rows, r => Assert.Equal(0, r.Played));
    }

    [Fact]
    public void Criterion1_Wins()
    {
        // a: 1V 1D (3 pts, saldo -2); b: 0V 3E (3 pts, saldo 0, 10 gols pró). b ganharia no saldo, mas vitórias vêm antes.
        var order = Order([M("a", "c", 1, 0), M("c", "a", 3, 0, 3), M("b", "c", 3, 3), M("b", "d", 3, 3), M("b", "c", 4, 4, 2)], ["a", "b"]);
        Assert.Equal(["a", "b"], order);
    }

    [Fact]
    public void Criterion2_GoalDifference()
    {
        // a e b: 1 vitória cada; a vence por 3, b por 1 com mais gols pró.
        Assert.Equal(["a", "b"], Order([M("a", "c", 3, 0), M("b", "d", 5, 4)], ["a", "b"]));
    }

    [Fact]
    public void Criterion3_GoalsFor()
    {
        // Mesmo saldo (+1); b fez mais gols. As chaves põem a à frente no sorteio, para o sorteio não mascarar.
        Assert.Equal(["b", "a"], Order([M("a", "c", 1, 0), M("b", "d", 3, 2)], ["a", "b"], AFirst));
    }

    [Fact]
    public void Criterion4_HeadToHead_Points()
    {
        // a e b: 1V 1E 1D, saldo 0, 2 gols pró. No confronto direto a venceu (2-1) e empatou (0-0) com b.
        // (Também passaria pelo saldo do confronto: em dois jogos, mais pontos sempre dão saldo melhor.)
        var matches = new[] { M("a", "b", 2, 1), M("b", "a", 0, 0, 2), M("c", "a", 1, 0, 3), M("b", "c", 1, 0, 4) };
        Assert.Equal(["a", "b"], Order(matches, ["a", "b"]));
    }

    [Fact]
    public void Criterion4_HeadToHead_GoalDifference()
    {
        // Confronto direto: 1 vitória para cada (3 pts a 3), mas b venceu por 3 e a por 1.
        // Na geral: mesmas vitórias (2), mesmo saldo (+3) e mesmos gols pró (6).
        var matches = new[] { M("a", "b", 1, 0), M("b", "a", 3, 0, 2), M("a", "c", 5, 0, 3), M("b", "d", 3, 2, 4) };
        Assert.Equal(["b", "a"], Order(matches, ["a", "b"], AFirst));
    }

    [Fact]
    public void Criterion4_CountsClubsStillTied_AfterCriteria1To3()
    {
        // a, b, c com 3 pontos; c sai pelas vitórias (0V 3E). Restam a e b (1V 1D, saldo 0, 1 gol pró):
        // o confronto direto vale entre os dois que continuam empatados, e a venceu b (o sorteio poria b à frente).
        var matches = new[] { M("a", "b", 1, 0), M("d", "a", 1, 0, 2), M("b", "d", 1, 0, 3), M("c", "d", 0, 0), M("d", "c", 0, 0, 2), M("c", "d", 0, 0, 3) };
        Assert.Equal(["a", "b", "c"], Order(matches, ["a", "b", "c"]));
    }

    [Fact]
    public void Criterion4_Ignored_WithThreeTied()
    {
        // a, b, c: 2V 2D, 2 gols pró e 2 contra. Entre os três, a venceu b e c: num "mini-campeonato" a
        // ficaria em 1º. Com 3 empatados o confronto direto não vale e decide o sorteio (chaves c=0, b=1, a=2).
        var matches = new[]
        {
            M("a", "b", 1, 0), M("a", "c", 1, 0, 2), M("d", "a", 1, 0, 3), M("d", "a", 1, 0, 4),
            M("b", "d", 1, 0, 2), M("b", "d", 1, 0, 3), M("d", "b", 1, 0, 4),
            M("c", "d", 1, 0, 1), M("c", "d", 1, 0, 3), M("d", "c", 1, 0, 4),
        };
        var keys = new Dictionary<string, int> { ["c"] = 0, ["b"] = 1, ["a"] = 2, ["d"] = 3 };
        Assert.Equal(["c", "b", "a"], Order(matches, ["a", "b", "c"], keys));
    }

    [Fact]
    public void Criterion7_Draw_UsesStoredKeys()
    {
        // Tudo igual, inclusive o confronto direto (1 a 1 em casa e fora): decide a chave guardada.
        var matches = new[] { M("a", "b", 1, 1), M("b", "a", 1, 1, 2) };
        Assert.Equal(["b", "a"], Order(matches, ["a", "b"]));
        Assert.Equal(["a", "b"], Order(matches, ["a", "b"], new() { ["a"] = 0, ["b"] = 1 }));
    }

    [Fact]
    public void HeadToHead_MidSeason_OnlyPlayedMatchesCount()
    {
        // A volta (b x a) ainda não foi jogada. Na geral, a e b têm 1V 1D e 2 gols pró e 2 contra;
        // a ida (a 2 x 1 b) decide o confronto direto, contra o sorteio, que poria b à frente.
        var matches = new[] { M("a", "b", 2, 1), M("d", "a", 1, 0, 2), M("b", "d", 1, 0, 3), M("b", "a", null, null, 4) };
        Assert.Equal(["a", "b"], Order(matches, ["a", "b"]));
    }

    // Tudo igual até o confronto direto (1 a 1 em casa e fora); o sorteio poria b à frente.
    private static readonly SeasonMatch[] Level = [M("a", "b", 1, 1), M("b", "a", 1, 1, 2)];

    [Fact]
    public void Criterion5_FewerRedCards()
    {
        // b tem 1 vermelho (e menos amarelos que a): vermelhos vêm antes dos amarelos.
        var cards = new[] { Card("b", MatchEventType.RedCard), Card("a", MatchEventType.YellowCard), Card("a", MatchEventType.YellowCard, 2) };
        Assert.Equal(["a", "b"], Order(Level, ["a", "b"], cards: cards));
    }

    [Fact]
    public void Criterion5_SecondYellowCountsAsRed()
    {
        var cards = new[] { Card("b", MatchEventType.YellowCard), Card("b", MatchEventType.SecondYellow), Card("a", MatchEventType.YellowCard), Card("a", MatchEventType.YellowCard, 2) };
        Assert.Equal(["a", "b"], Order(Level, ["a", "b"], cards: cards));
    }

    [Fact]
    public void Criterion6_FewerYellowCards()
    {
        var cards = new[] { Card("b", MatchEventType.YellowCard), Card("b", MatchEventType.YellowCard, 2), Card("a", MatchEventType.YellowCard) };
        Assert.Equal(["a", "b"], Order(Level, ["a", "b"], cards: cards));
    }

    [Fact]
    public void Cards_OfMatchesNotPlayed_DoNotCount()
    {
        // O cartão de b é de um jogo sem placar (dado inconsistente): não conta, e decide o sorteio (b à frente).
        var matches = new[] { M("a", "b", 1, 1), M("b", "a", null, null, 2) };
        Assert.Equal(["b", "a"], Order(matches, ["a", "b"], cards: [Card("b", MatchEventType.RedCard, 2)]));
    }

    [Fact]
    public void TieBreakers_FollowRegulationOrder()
    {
        Assert.Equal(
            ["Vitórias", "Saldo de gols", "Gols pró", "Confronto direto (pontos)", "Confronto direto (saldo)", "Cartões vermelhos", "Cartões amarelos", "Sorteio"],
            Standings.TieBreakers.Select(t => t.Name));
    }

    [Theory]
    [InlineData(1, Zone.Libertadores)]
    [InlineData(4, Zone.Libertadores)]
    [InlineData(5, Zone.LibertadoresPreliminary)]
    [InlineData(6, Zone.SulAmericana)]
    [InlineData(11, Zone.SulAmericana)]
    [InlineData(12, Zone.None)]
    [InlineData(16, Zone.None)]
    [InlineData(17, Zone.Relegation)]
    [InlineData(20, Zone.Relegation)]
    public void Zones(int position, Zone zone) => Assert.Equal(zone, Standings.ZoneOf(position));
}
