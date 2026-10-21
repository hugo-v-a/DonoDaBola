using System.Text.Json;

namespace DonoDaBola.Core.Tests;

public class ModelTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Rating_AcceptsBounds(int value) => Assert.Equal(value, Rating.Check(value));

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-5)]
    public void Rating_RejectsOutOfRange(int value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Rating.Check(value, "Passing"));
        Assert.Equal("Passing", ex.ParamName);
    }

    public static TheoryData<string, Func<Player, Player>> InvalidChanges => new()
    {
        { "Finishing", p => p with { Technical = p.Technical with { Finishing = 21 } } },
        { "Composure", p => p with { Mental = p.Mental with { Composure = 0 } } },
        { "Pace", p => p with { Physical = p.Physical with { Pace = 25 } } },
        { "Reflexes", p => p with { Goalkeeping = p.Goalkeeping with { Reflexes = -1 } } },
        { "Potential", p => p with { Potential = 30 } },
    };

    [Theory]
    [MemberData(nameof(InvalidChanges))]
    public void EachAttributeGroup_RejectsOutOfRange(string attribute, Func<Player, Player> change)
    {
        var player = TestPlayers.Create("p1", Position.Midfielder);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => change(player));
        Assert.Equal(attribute, ex.ParamName);
    }

    private const string PlayerJson = """
        {
          "Id": "clu01-p01", "Name": "Fulano", "Position": "Meio-campista", "BirthDate": "2004-03-10", "Potential": 16,
          "Technical": { "Passing": 14, "Dribbling": 12, "Finishing": 9, "Crossing": 10,
                         "Tackling": 8, "Marking": 7, "Heading": 6, "SetPieces": 11 },
          "Mental": { "Vision": 15, "Positioning": 10, "Decisions": 13,
                      "Leadership": 5, "Composure": 12, "Aggression": 8 },
          "Physical": { "Pace": 13, "Stamina": 14, "Strength": 9, "Agility": 12 },
          "Goalkeeping": { "Reflexes": 2, "Positioning": 1, "RushingOut": 1, "Kicking": 3 }
        }
        """;

    [Fact]
    public void Player_DeserializesFromJson()
    {
        var player = JsonSerializer.Deserialize<Player>(PlayerJson)!;

        Assert.Equal("clu01-p01", player.Id);
        Assert.Equal(Position.Midfielder, player.Position);
        Assert.Equal(14, player.Technical.Passing);
        Assert.Equal(15, player.Mental.Vision);
        Assert.Equal(1, player.Goalkeeping.RushingOut);
    }

    [Fact]
    public void Player_SerializesPositionInPortuguese()
    {
        var json = JsonSerializer.Serialize(TestPlayers.Create("g1", Position.Goalkeeper));

        Assert.Contains("\"Position\":\"Goleiro\"", json);
    }

    [Fact]
    public void Player_JsonWithOutOfRangeAttribute_Throws()
    {
        var json = PlayerJson.Replace("\"Passing\": 14", "\"Passing\": 25");

        Assert.Throws<ArgumentOutOfRangeException>(() => JsonSerializer.Deserialize<Player>(json));
    }

    [Fact]
    public void Club_HoldsSquad()
    {
        var squad = new[] { TestPlayers.Create("p1", Position.Goalkeeper), TestPlayers.Create("p2", Position.Forward) };

        var club = new Club { Id = "clu01", Name = "Clube Um", ShortName = "UM", Squad = squad };

        Assert.Equal(2, club.Squad.Count);
        Assert.Equal("p2", club.Squad[1].Id);
    }
}
