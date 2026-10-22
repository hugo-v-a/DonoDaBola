using DonoDaBola.Core.Competition;

namespace DonoDaBola.Core.Tests;

public class ScheduleTests
{
    private static readonly string[] Ids = Enumerable.Range(1, 20).Select(i => $"clu{i:00}").ToArray();
    private static IReadOnlyList<Fixture> Generate(int seed) => Schedule.Generate(Ids, new Random(seed));

    [Fact]
    public void HasThirtyEightRoundsOfTenMatches()
    {
        var fixtures = Generate(1);
        Assert.Equal(380, fixtures.Count);
        Assert.Equal(Enumerable.Range(1, 38), fixtures.Select(f => f.Round).Distinct());
        Assert.All(fixtures.GroupBy(f => f.Round), r => Assert.Equal(Enumerable.Range(0, 10), r.Select(f => f.Index)));
    }

    [Fact]
    public void EveryPairMeetsTwice_OnceAtEachHome()
    {
        var fixtures = Generate(2);
        foreach (var a in Ids)
            foreach (var b in Ids.Where(b => b != a))
                Assert.Single(fixtures, f => f.HomeId == a && f.AwayId == b);
    }

    [Fact]
    public void EachClub_NineteenHomeAndNineteenAway_OncePerRound()
    {
        var fixtures = Generate(3);
        foreach (var id in Ids)
        {
            Assert.Equal(19, fixtures.Count(f => f.HomeId == id));
            Assert.Equal(19, fixtures.Count(f => f.AwayId == id));
        }
        Assert.All(fixtures.GroupBy(f => f.Round), r => Assert.Equal(20, r.SelectMany(f => new[] { f.HomeId, f.AwayId }).Distinct().Count()));
    }

    [Fact]
    public void SecondHalf_MirrorsFirstHalf_WithHomeSwapped()
    {
        var fixtures = Generate(4);
        foreach (var f in fixtures.Where(f => f.Round <= 19))
            Assert.Contains(fixtures, m => m.Round == f.Round + 19 && m.HomeId == f.AwayId && m.AwayId == f.HomeId);
    }

    [Fact]
    public void NoLongHomeOrAwayStreaks()
    {
        var fixtures = Generate(5);
        foreach (var id in Ids)
        {
            var home = fixtures.Where(f => f.HomeId == id || f.AwayId == id).OrderBy(f => f.Round).Select(f => f.HomeId == id).ToList();
            Assert.True(LongestStreak(home[..19]) <= 2, $"{id} no turno");
            Assert.True(LongestStreak(home[19..]) <= 2, $"{id} no returno");
            Assert.True(LongestStreak(home) <= 3, $"{id} na virada");
        }
    }

    [Fact]
    public void SameSeed_SameSchedule_OtherSeed_Different()
    {
        Assert.Equal(Generate(42), Generate(42));
        Assert.NotEqual(Generate(42), Generate(43));
    }

    [Fact]
    public void OddOrTooFewClubs_Throws()
    {
        Assert.Throws<ArgumentException>(() => Schedule.Generate(Ids[..19], new Random(1)));
        Assert.Throws<ArgumentException>(() => Schedule.Generate(["a", "a"], new Random(1)));
    }

    private static int LongestStreak(List<bool> values)
    {
        int best = 0, run = 0;
        for (var i = 0; i < values.Count; i++)
        {
            run = i > 0 && values[i] == values[i - 1] ? run + 1 : 1;
            best = Math.Max(best, run);
        }
        return best;
    }
}
