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
    public void Pick442_RejectsSquadUnder11()
    {
        var club = TestPlayers.CreateClub("a");
        club = club with { Squad = [.. club.Squad.Take(10)] };

        Assert.Throws<ArgumentException>(() => Lineup.Pick442(club));
    }
}
