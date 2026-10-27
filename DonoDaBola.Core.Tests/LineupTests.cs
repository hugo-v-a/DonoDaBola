using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Tests;

public class LineupTests
{
    [Fact]
    public void Pick442_PicksFormation()
    {
        var lineup = Lineup.Pick442(TestPlayers.CreateClub("a"));

        Assert.Equal(Position.Goalkeeper, lineup.Goalkeeper.Position);
        Assert.Equal(4, lineup.Defenders.Count(p => p.Position == Position.Defender));
        Assert.Equal(4, lineup.Midfielders.Count(p => p.Position == Position.Midfielder));
        Assert.Equal(2, lineup.Forwards.Count(p => p.Position == Position.Forward));
    }

    [Fact]
    public void Pick442_PicksBestPerPosition()
    {
        var club = TestPlayers.CreateClub("a");
        var defenders = Enumerable.Range(0, 6).Select(i => TestPlayers.Create($"d{i}", Position.Defender, 5 + i * 2));
        club = club with { Squad = [.. club.Squad.Where(p => p.Position != Position.Defender), .. defenders] };

        var lineup = Lineup.Pick442(club);

        Assert.Equal(["d5", "d4", "d3", "d2"], lineup.Defenders.Select(p => p.Id));
    }

    [Fact]
    public void Pick442_FillsShortPosition()
    {
        var club = TestPlayers.CreateClub("a");
        club = club with { Squad = [.. club.Squad.Where(p => p.Position != Position.Forward).Take(13), TestPlayers.Create("f0", Position.Forward)] };

        var lineup = Lineup.Pick442(club);
        var all = lineup.Players.ToList();

        Assert.Equal(11, all.Distinct().Count());
        Assert.Equal(2, lineup.Forwards.Count);
        Assert.Contains(lineup.Forwards, p => p.Id == "f0");
    }

    [Fact]
    public void HasNatural442_RequiresMinimumPerPosition()
    {
        var club = TestPlayers.CreateClub("a");
        Assert.True(Lineup.HasNatural442(club));

        var noKeeper = club with { Squad = [.. club.Squad.Where(p => p.Position != Position.Goalkeeper)] };
        var oneForward = club with { Squad = [.. club.Squad.Where(p => p.Position != Position.Forward), TestPlayers.Create("f0", Position.Forward)] };
        Assert.False(Lineup.HasNatural442(noKeeper));
        Assert.False(Lineup.HasNatural442(oneForward));
    }

    [Fact]
    public void Pick442_RejectsSquadUnder11()
    {
        var club = TestPlayers.CreateClub("a");
        club = club with { Squad = [.. club.Squad.Take(10)] };

        Assert.Throws<ArgumentException>(() => Lineup.Pick442(club));
    }
}

public class LineupAvailabilityTests
{
    private static readonly Club Club = RegressionTests.ClubOf("clu01");

    [Fact]
    public void NoUnavailable_SameAsPick442()
    {
        var a = Lineup.Pick442(Club);
        var b = Lineup.Pick442(Club, new HashSet<string>());
        Assert.Equal(a.Players, b.Players);
    }

    [Fact]
    public void Unavailable_AreLeftOut_StartingGoalkeeperToo()
    {
        var first = Lineup.Pick442(Club);
        var off = new HashSet<string> { first.Goalkeeper.Id, first.Forwards[0].Id };
        var lineup = Lineup.Pick442(Club, off);
        Assert.Equal(11, lineup.Players.Count());
        Assert.DoesNotContain(lineup.Players, p => off.Contains(p.Id));
        Assert.Equal(Position.Goalkeeper, lineup.Goalkeeper.Position);
    }

    [Fact]
    public void MassUnavailability_StillEleven()
    {
        // 20 dos 28 fora: sobram 8, e o time é completado com indisponíveis do elenco.
        var off = Club.Squad.Take(20).Select(p => p.Id).ToHashSet();
        var lineup = Lineup.Pick442(Club, off);
        Assert.Equal(11, lineup.Players.Distinct().Count());
        Assert.All(Club.Squad.Skip(20), p => Assert.Contains(p, lineup.Players));
    }
}
