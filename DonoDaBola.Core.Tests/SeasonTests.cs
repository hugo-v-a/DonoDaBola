using System.Diagnostics;
using DonoDaBola.Core.Competition;
using DonoDaBola.Core.Generation;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Tests;

public class SeasonTests
{
    // Liga gerada uma vez: os 20 clubes genéricos, níveis 15 (clu01) a 8 (clu20).
    private static readonly IReadOnlyList<Club> League =
        LeagueGenerator.Create(new Random(7), GenerationTests.Stats, SeasonConfig.Default.Start).Clubs;

    private static Season Play(int seed, int rounds = 38)
    {
        var season = Season.Create(League, seed);
        for (var i = 0; i < rounds; i++)
            season.AdvanceRound();
        return season;
    }

    private static string Shape(Season s) =>
        string.Join(";", s.Table().Select(r => $"{r.Club.Id}:{r.Points}:{r.GoalsFor}:{r.GoalsAgainst}"));

    [Fact]
    public void MatchSeed_ReferenceValue()
    {
        // Mudar a fórmula muda todas as temporadas salvas: este valor fixo avisa.
        Assert.Equal(42_007_192, Season.MatchSeed(42, 7, 3));
        Assert.Equal(2_146_521_996, Season.MatchSeed(int.MaxValue, 38, 9)); // estoura sem erro (unchecked)
    }

    [Fact]
    public void Dates_FollowConfig_WithWorldCupBreak()
    {
        var c = SeasonConfig.Default;
        Assert.Equal(new DateOnly(2026, 1, 28), c.DateOf(1));
        Assert.Equal(new DateOnly(2026, 5, 27), c.DateOf(18));
        Assert.Equal(new DateOnly(2026, 7, 22), c.DateOf(19));
        Assert.Equal(new DateOnly(2026, 12, 2), c.DateOf(38));

        var other = new SeasonConfig(new DateOnly(2027, 2, 3), 10, new DateOnly(2027, 6, 2), 4);
        Assert.Equal(new DateOnly(2027, 3, 11), other.DateOf(10));
        Assert.Equal(new DateOnly(2027, 6, 6), other.DateOf(12));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.DateOf(39));
    }

    [Fact]
    public void Config_Invalid_Throws()
    {
        var start = new DateOnly(2026, 1, 28);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeasonConfig(start, 0, start.AddDays(200)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeasonConfig(start, 38, start.AddDays(400)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeasonConfig(start, 18, start.AddDays(100))); // volta antes da 18ª
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeasonConfig(start, 18, start.AddDays(200), 0));
    }

    [Fact]
    public void Clock_BeforeDuringAndAfter()
    {
        var season = Season.Create(League, 1);
        Assert.Equal(1, season.CurrentRound);
        Assert.Equal(new DateOnly(2026, 1, 28), season.Today); // antes do início: data da 1ª rodada

        for (var i = 0; i < 18; i++) season.AdvanceRound();
        Assert.Equal(new DateOnly(2026, 7, 22), season.Today); // depois da pausa: data da 19ª

        for (var i = 18; i < 38; i++) season.AdvanceRound();
        Assert.True(season.IsFinished);
        Assert.Equal(39, season.CurrentRound);
        Assert.Equal(new DateOnly(2026, 12, 2), season.Today); // encerrada: fica na data da 38ª

        var ex = Assert.Throws<InvalidOperationException>(() => season.AdvanceRound());
        Assert.Contains("38", ex.Message);
    }

    [Fact]
    public void Age_AdvancesWithTheSeasonClock()
    {
        var season = Season.Create(League, 1);
        var player = TestPlayers.Create("aniversariante", Position.Forward) with { BirthDate = new DateOnly(2000, 6, 15) };
        IGameClock clock = season;
        Assert.Equal(25, player.AgeOn(clock.Today));
        for (var i = 0; i < 38; i++) season.AdvanceRound();
        Assert.Equal(26, player.AgeOn(clock.Today));
    }

    [Fact]
    public void AdvanceRound_PlaysTheTenMatches_WithEvents()
    {
        var season = Season.Create(League, 3);
        var results = season.AdvanceRound();

        Assert.Equal(10, results.Count);
        var round = season.Round(1);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal((round[i].Home, round[i].Away), (results[i].Home, results[i].Away));
            Assert.Equal((round[i].HomeGoals, round[i].AwayGoals), ((int?)results[i].HomeGoals, (int?)results[i].AwayGoals));
        }
        Assert.All(season.Round(2), m => Assert.Null(m.HomeGoals));
        Assert.Equal(2, season.CurrentRound);

        // Cada jogo usa a própria seed derivada: dá para refazer um jogo isolado.
        var again = MatchEngine.Simulate(round[4].Home, round[4].Away, new Random(Season.MatchSeed(3, 1, 4)));
        Assert.Equal(results[4].Events.Count, again.Events.Count);
    }

    [Fact]
    public void Views_RoundMatchesOfAndTable()
    {
        var season = Play(5, rounds: 10);
        var mine = season.MatchesOf("clu01");
        Assert.Equal(38, mine.Count);
        Assert.Equal(Enumerable.Range(1, 38), mine.Select(m => m.Round));
        Assert.Equal(10, mine.Count(m => m.HomeGoals is not null));
        Assert.All(mine, m => Assert.Equal(season.DateOf(m.Round), m.Date));
        Assert.Throws<ArgumentException>(() => season.MatchesOf("nao-existe"));
        Assert.Throws<ArgumentOutOfRangeException>(() => season.Round(0));

        var table = season.Table();
        Assert.Equal(20, table.Count);
        Assert.All(table, r => Assert.Equal(10, r.Played));
        Assert.Equal(table.Sum(r => r.GoalsFor), table.Sum(r => r.GoalsAgainst));
    }

    [Fact]
    public void TopScorers_MatchGoalsOfTheSeason()
    {
        var season = Play(6, rounds: 12);
        var scorers = season.TopScorers(limit: 500);

        Assert.Equal(season.Table().Sum(r => r.GoalsFor), scorers.Sum(s => s.Goals));
        Assert.Equal(scorers.OrderByDescending(s => s.Goals).ThenBy(s => s.Player.Id, StringComparer.Ordinal), scorers);
        Assert.Throws<ArgumentOutOfRangeException>(() => season.TopScorers(0));
        Assert.All(scorers, s => Assert.Contains(s.Player, s.Club.Squad));
        Assert.Equal(5, season.TopScorers(limit: 5).Count);
    }

    [Fact]
    public void SameSeed_SameFinalTable_InAFewSeconds()
    {
        var watch = Stopwatch.StartNew();
        var first = Play(42);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"380 jogos em {watch.Elapsed.TotalSeconds:F1} s");
        Assert.Equal(Shape(first), Shape(Play(42)));
        Assert.NotEqual(Shape(first), Shape(Play(43)));
    }

    [Fact]
    public void Restore_MidSeason_ContinuesExactlyTheSame()
    {
        var straight = Play(11);

        var half = Play(11, rounds: 19);
        var resumed = Season.Restore(League, half.Seed, half.Config, half.Matches, half.Goals, half.DrawKeys);
        Assert.Equal(20, resumed.CurrentRound);
        Assert.Equal(Shape(half), Shape(resumed)); // sorteio estável: mesma ordem depois de restaurar
        while (!resumed.IsFinished) resumed.AdvanceRound();

        Assert.Equal(Shape(straight), Shape(resumed));
        Assert.Equal(straight.TopScorers(10).Select(s => (s.Player.Id, s.Goals)), resumed.TopScorers(10).Select(s => (s.Player.Id, s.Goals)));
    }

    [Fact]
    public void Restore_HalfPlayedRound_Throws()
    {
        var season = Play(12, rounds: 3);
        var broken = season.Matches.Select(m => m.Round == 3 && m.Index == 5 ? m with { HomeGoals = null, AwayGoals = null } : m).ToList();
        Assert.Throws<InvalidDataException>(() => Season.Restore(League, season.Seed, season.Config, broken, season.Goals, season.DrawKeys));
    }

    [Fact]
    public void Restore_CorruptData_Throws()
    {
        var s = Play(14, rounds: 2);
        Season Restore(IEnumerable<SeasonMatch>? matches = null, IEnumerable<SeasonGoal>? goals = null, Dictionary<string, int>? keys = null) =>
            Season.Restore(League, s.Seed, s.Config, matches ?? s.Matches, goals ?? s.Goals, keys ?? s.DrawKeys.ToDictionary());

        // placar pela metade
        Assert.Throws<InvalidDataException>(() => Restore(matches: s.Matches.Select((m, i) => i == 3 ? m with { AwayGoals = null } : m)));
        // jogo repetido no lugar de outro (continua com 380)
        Assert.Throws<InvalidDataException>(() => Restore(matches: s.Matches.Select((m, i) => i == 4 ? s.Matches[3] : m)));
        // chaves de sorteio repetidas
        var keys = s.DrawKeys.ToDictionary();
        keys[keys.Keys.First()] = keys.Values.Last();
        Assert.Throws<InvalidDataException>(() => Restore(keys: keys));
        // gol de jogador que não é do clube
        Assert.Throws<InvalidDataException>(() => Restore(goals: s.Goals.Append(new SeasonGoal(1, 0, 10, "clu01", "clu02-p01"))));
    }

    [Fact]
    public void Draw_IsStable_AcrossReads()
    {
        var season = Season.Create(League, 13);
        Assert.Equal(20, season.DrawKeys.Values.Distinct().Count());
        // Antes da 1ª rodada todos empatam em tudo: só o sorteio ordena, e a ordem não muda entre leituras.
        var first = season.Table().Select(r => r.Club.Id).ToList();
        Assert.Equal(first, season.Table().Select(r => r.Club.Id));
        Assert.Equal(season.DrawKeys.OrderBy(k => k.Value).Select(k => k.Key), first);
    }

    [Fact]
    public void StrongerClubs_FinishHigher_OnAverage()
    {
        const int seeds = 20;
        var positions = League.ToDictionary(c => c.Id, _ => 0.0);
        var champions = new HashSet<string>();
        for (var seed = 1; seed <= seeds; seed++)
        {
            var table = Play(seed).Table();
            champions.Add(table[0].Club.Id);
            foreach (var row in table)
                positions[row.Club.Id] += row.Position / (double)seeds;
        }

        // Spearman entre o nível do clube (LeagueGenerator.Levels) e a posição média nas 20 seeds.
        // Nível maior = posição menor, então a correlação esperada é negativa.
        var rho = Spearman(LeagueGenerator.Levels.Select(l => (double)l).ToArray(), League.Select(c => positions[c.Id]).ToArray());
        Assert.True(rho < -0.7, $"Spearman = {rho:F2}");
        Assert.True(champions.Count > 1, "o campeão foi sempre o mesmo clube");
    }

    // Pearson sobre os postos (empates recebem o posto médio).
    private static double Spearman(double[] x, double[] y)
    {
        static double[] Ranks(double[] v) => v.Select(a => v.Count(b => b < a) + (v.Count(b => b == a) + 1) / 2.0).ToArray();
        var (rx, ry) = (Ranks(x), Ranks(y));
        var (mx, my) = (rx.Average(), ry.Average());
        var cov = rx.Zip(ry).Sum(p => (p.First - mx) * (p.Second - my));
        return cov / Math.Sqrt(rx.Sum(a => (a - mx) * (a - mx)) * ry.Sum(b => (b - my) * (b - my)));
    }
}
