using System.Text.Json;

namespace DonoDaBola.Core.Tests;

// Campos de perfil do jogador: nascimento/idade, altura, peso, pé, nacionalidade e origem do dado.
public class PlayerProfileTests
{
    private static Player Born(int year, int month, int day) =>
        TestPlayers.Create("p1", Position.Defender) with { BirthDate = new DateOnly(year, month, day) };

    [Theory]
    [InlineData(2026, 5, 10, 26)] // dia do aniversário
    [InlineData(2026, 5, 9, 25)]  // véspera
    [InlineData(2026, 12, 31, 26)]
    public void AgeOn_CountsCompletedYears(int y, int m, int d, int expected) =>
        Assert.Equal(expected, Born(2000, 5, 10).AgeOn(new DateOnly(y, m, d)));

    // Convenção: em ano não bissexto, quem nasceu em 29/02 faz aniversário em 28/02 (como DateOnly.AddYears).
    [Fact]
    public void AgeOn_LeapDayBirthday()
    {
        var player = Born(2004, 2, 29);

        Assert.Equal(21, player.AgeOn(new DateOnly(2026, 2, 27)));
        Assert.Equal(22, player.AgeOn(new DateOnly(2026, 2, 28)));
    }

    [Fact]
    public void AgeOn_UsesTheGameClock()
    {
        IGameClock clock = new FixedClock(new DateOnly(2030, 6, 1));

        Assert.Equal(30, Born(2000, 1, 1).AgeOn(clock.Today));
    }

    [Theory]
    [InlineData(150)]
    [InlineData(215)]
    [InlineData(null)]
    public void HeightCm_AcceptsRangeOrNull(int? height) =>
        Assert.Equal(height, TestPlayers.Create("p1", Position.Goalkeeper).With(height: height).HeightCm);

    [Theory]
    [InlineData(149)]
    [InlineData(216)]
    public void HeightCm_RejectsOutOfRange(int height)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestPlayers.Create("p1", Position.Goalkeeper).With(height: height));
        Assert.Equal("HeightCm", ex.ParamName);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(121)]
    public void WeightKg_RejectsOutOfRange(int weight)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestPlayers.Create("p1", Position.Goalkeeper) with { WeightKg = weight });
        Assert.Equal("WeightKg", ex.ParamName);
    }

    [Theory]
    [InlineData("br")]
    [InlineData("BRA")]
    [InlineData("")]
    public void Nationality_RequiresIsoAlpha2(string code) =>
        Assert.Throws<ArgumentException>(() => TestPlayers.Create("p1", Position.Forward) with { Nationality = code });

    [Fact]
    public void NewFields_HaveDefaults()
    {
        var player = TestPlayers.Create("p1", Position.Forward);

        Assert.Null(player.HeightCm);
        Assert.Null(player.WeightKg);
        Assert.Equal(PreferredFoot.Right, player.PreferredFoot);
        Assert.Equal("BR", player.Nationality);
        Assert.Equal(DataSource.Generated, player.DataSource);
    }

    [Fact]
    public void NewFields_RoundTripInPortugueseJson()
    {
        var player = TestPlayers.Create("p1", Position.Forward) with
        {
            HeightCm = 178, WeightKg = 74, PreferredFoot = PreferredFoot.Left, Nationality = "AR", DataSource = DataSource.Wikidata,
        };

        var json = JsonSerializer.Serialize(player);
        var back = JsonSerializer.Deserialize<Player>(json)!;

        Assert.Contains("\"PreferredFoot\":\"esquerdo\"", json);
        Assert.Contains("\"DataSource\":\"wikidata\"", json);
        Assert.Contains("\"BirthDate\":\"2000-01-01\"", json);
        Assert.Equal(player, back);
    }
}

file static class PlayerExtensions
{
    public static Player With(this Player p, int? height) => p with { HeightCm = height };
}
