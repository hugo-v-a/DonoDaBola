using System.Text.Json;

namespace DonoDaBola.Core.Tests;

public class SeedFileTests
{
    [Fact]
    public void RoundTrip_KeepsClubsAndPlayers()
    {
        var seed = new SeedFile([TestPlayers.CreateClub("a"), TestPlayers.CreateClub("b")]);

        var back = SeedFile.FromJson(seed.ToJson());

        Assert.Equal(SeedFile.CurrentVersion, back.Version);
        Assert.Equal(2, back.Clubs.Count);
        Assert.Equal(seed.Clubs[1].Squad, back.Clubs[1].Squad);
    }

    [Fact]
    public void FromJson_RejectsExplicitNull()
    {
        var json = new SeedFile([TestPlayers.CreateClub("a")]).ToJson().Replace("\"Name\": \"Clube a\"", "\"Name\": null");

        Assert.Throws<JsonException>(() => SeedFile.FromJson(json));
    }

    [Fact]
    public void FromJson_RejectsPlayerWithoutBirthDate()
    {
        var json = new SeedFile([TestPlayers.CreateClub("a")]).ToJson().Replace("\"BirthDate\": \"2000-01-01\",", "");

        Assert.Throws<JsonException>(() => SeedFile.FromJson(json));
    }
}
