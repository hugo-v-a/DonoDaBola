using System.Diagnostics;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Tests;

public class MatchEngineTests
{
    private static readonly Club Home = TestPlayers.CreateClub("casa");
    private static readonly Club Away = TestPlayers.CreateClub("fora");

    [Fact]
    public void SameSeed_ProducesSameMatch()
    {
        var a = MatchEngine.Simulate(Home, Away, new Random(7));
        var b = MatchEngine.Simulate(Home, Away, new Random(7));

        Assert.Equal((a.HomeGoals, a.AwayGoals), (b.HomeGoals, b.AwayGoals));
        Assert.Equal(a.Events, b.Events);
        Assert.NotEmpty(a.Events);
    }

    [Fact]
    public void Score_MatchesGoalEvents()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var r = MatchEngine.Simulate(Home, Away, new Random(seed));

            Assert.Equal(r.HomeGoals, r.Events.Count(e => e.Type == MatchEventType.Goal && e.Club == Home));
            Assert.Equal(r.AwayGoals, r.Events.Count(e => e.Type == MatchEventType.Goal && e.Club == Away));
        }
    }

    [Fact]
    public void Events_AreOrderedAndInsideMatch()
    {
        var r = MatchEngine.Simulate(Home, Away, new Random(3));

        Assert.All(r.Events, e => Assert.InRange(e.Minute, 1, 90));
        Assert.Equal(r.Events.OrderBy(e => e.Minute), r.Events);
        Assert.All(r.Events, e =>
        {
            var (own, rival) = e.Club == Home ? (r.HomeLineup, r.AwayLineup) : (r.AwayLineup, r.HomeLineup);
            Assert.Contains(e.Player, own.Players);
            Assert.NotEqual(Position.Goalkeeper, e.Player.Position);
            Assert.Equal(rival.Goalkeeper, e.Goalkeeper);
        });
    }

    [Fact]
    public void StrongerTeam_WinsMostMatches()
    {
        var strong = TestPlayers.CreateClub("forte", 15);
        var weak = TestPlayers.CreateClub("fraco", 8);
        var wins = 0;

        for (var seed = 0; seed < 1000; seed++)
        {
            var strongAtHome = seed % 2 == 0;
            var r = strongAtHome
                ? MatchEngine.Simulate(strong, weak, new Random(seed))
                : MatchEngine.Simulate(weak, strong, new Random(seed));
            var (mine, theirs) = strongAtHome ? (r.HomeGoals, r.AwayGoals) : (r.AwayGoals, r.HomeGoals);
            if (mine > theirs) wins++;
        }

        Assert.True(wins > 700, $"O time forte venceu só {wins} de 1000.");
    }

    [Fact]
    public void BalancedTeams_AverageGoalsIsPlausible()
    {
        var goals = Enumerable.Range(0, 1000)
            .Select(seed => MatchEngine.Simulate(Home, Away, new Random(seed)))
            .Average(r => r.HomeGoals + r.AwayGoals);

        Assert.InRange(goals, 1.5, 3.5);
    }

    [Fact]
    public void HomeTeam_HasSmallAdvantage()
    {
        var results = Enumerable.Range(0, 1000).Select(seed => MatchEngine.Simulate(Home, Away, new Random(seed))).ToList();

        Assert.True(results.Count(r => r.HomeGoals > r.AwayGoals) > results.Count(r => r.AwayGoals > r.HomeGoals));
    }

    [Fact]
    public void Season_SimulatesQuickly()
    {
        var watch = Stopwatch.StartNew();
        for (var seed = 0; seed < 380; seed++)
            MatchEngine.Simulate(Home, Away, new Random(seed));

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"380 partidas levaram {watch.Elapsed.TotalSeconds:F2} s.");
    }
}
