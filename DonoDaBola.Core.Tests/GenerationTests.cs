using DonoDaBola.Core.Generation;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Tests;

public class GenerationTests
{
    private static readonly DateOnly SeasonStart = new(2026, 1, 1);

    // Estatísticas fixas para os testes (as reais vêm de data/player-stats.json).
    public static readonly PlayerStats Stats = new(
        SampleSize: 1000,
        Positions:
        [
            new(Position.Goalkeeper, 100, AgeMean: 27, AgeSd: 5, HeightMean: 189, HeightSd: 4, WeightMean: 84, WeightSd: 5),
            new(Position.Defender, 300, AgeMean: 26, AgeSd: 4, HeightMean: 183, HeightSd: 6, WeightMean: 77, WeightSd: 6),
            new(Position.Midfielder, 350, AgeMean: 26, AgeSd: 4, HeightMean: 176, HeightSd: 6, WeightMean: 71, WeightSd: 6),
            new(Position.Forward, 250, AgeMean: 25, AgeSd: 4, HeightMean: 178, HeightSd: 7, WeightMean: 74, WeightSd: 7),
        ],
        Nationalities: [new("BR", 0.9), new("AR", 0.05), new("UY", 0.05)]);

    private static SeedFile League(int seed) => LeagueGenerator.Create(new Random(seed), Stats, SeasonStart);

    [Fact]
    public void SameSeed_ProducesSameLeague()
    {
        Assert.Equal(League(2026).ToJson(), League(2026).ToJson());
        Assert.NotEqual(League(2026).ToJson(), League(7).ToJson());
    }

    [Fact]
    public void League_Has20ClubsWithRealisticSquads()
    {
        var league = League(2026);

        Assert.Equal(20, league.Clubs.Count);
        Assert.Equal(20, league.Clubs.Select(c => c.Name).Distinct().Count());
        Assert.All(league.Clubs, c => Assert.InRange(c.Squad.Count, 25, 32));
        Assert.Equal(league.Clubs.Sum(c => c.Squad.Count), league.Clubs.SelectMany(c => c.Squad).Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void EveryClub_CanField442WithARealGoalkeeper()
    {
        foreach (var club in League(2026).Clubs)
        {
            Assert.True(Lineup.HasNatural442(club), club.Name);
            var keeper = Lineup.Pick442(club).Goalkeeper;
            Assert.Equal(Position.Goalkeeper, keeper.Position);
            Assert.True(Lineup.Rate(keeper, Position.Goalkeeper) > Lineup.Rate(keeper, Position.Forward), keeper.Name);
        }
    }

    [Fact]
    public void GeneratedFields_StayWithinLimits()
    {
        var players = League(2026).Clubs.SelectMany(c => c.Squad).ToList();

        Assert.All(players, p =>
        {
            Assert.InRange(p.HeightCm!.Value, Player.MinHeightCm, Player.MaxHeightCm);
            Assert.InRange(p.WeightKg!.Value, Player.MinWeightKg, Player.MaxWeightKg);
            Assert.InRange(p.AgeOn(SeasonStart), 16, 40);
            Assert.Equal(DataSource.Generated, p.DataSource);
            Assert.Contains(p.Nationality, Stats.Nationalities.Select(n => n.Code));
        });
        // Atributos fora de 1–20 nem chegam a existir: o modelo lança ao criar. Basta conferir que todos foram criados.
        Assert.Equal(League(2026).Clubs.Sum(c => c.Squad.Count), players.Count);
    }

    [Fact]
    public void StrongerClubs_HaveHigherAverageRatings()
    {
        var league = League(2026);
        var byLevel = league.Clubs.Zip(LeagueGenerator.Levels)
            .Select(x => (Level: x.Second, Rating: x.First.Squad.Average(p => Lineup.Rate(p, p.Position))))
            .ToList();

        var strong = byLevel.Where(x => x.Level >= 14).Average(x => x.Rating);
        var weak = byLevel.Where(x => x.Level <= 9).Average(x => x.Rating);
        Assert.True(strong > weak + 2, $"forte {strong:F1} x fraco {weak:F1}");
    }

    [Fact]
    public void Heights_FollowPositionStats()
    {
        var players = League(2026).Clubs.SelectMany(c => c.Squad).ToList();
        double AvgHeight(Position pos) => players.Where(p => p.Position == pos).Average(p => p.HeightCm!.Value);

        Assert.True(AvgHeight(Position.Goalkeeper) > AvgHeight(Position.Midfielder) + 8);
    }

    [Fact]
    public void Feet_AreMostlyRight()
    {
        var players = League(2026).Clubs.SelectMany(c => c.Squad).ToList();
        var right = players.Count(p => p.PreferredFoot == PreferredFoot.Right) / (double)players.Count;

        Assert.InRange(right, 0.65, 0.85);
        Assert.Contains(players, p => p.PreferredFoot == PreferredFoot.Left);
    }

    [Fact]
    public void Names_AreFictitiousCombinationsAndUnique()
    {
        var names = League(2026).Clubs.SelectMany(c => c.Squad).Select(p => p.Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.All(names, n => Assert.True(NameGenerator.IsGenerated(n), n));
    }

    [Fact]
    public void Stats_FromSamples_AggregatesPerPosition()
    {
        var reference = new DateOnly(2026, 1, 1);
        PlayerSample[] samples =
        [
            new(Position.Goalkeeper, new DateOnly(1996, 1, 1), 190, 85, "BR"),
            new(Position.Goalkeeper, new DateOnly(2000, 1, 1), 186, null, "AR"),
            new(Position.Forward, new DateOnly(2004, 1, 1), null, 70, "BR"),
            new(Position.Forward, new DateOnly(2002, 1, 1), 175, 72, null),
        ];

        var stats = PlayerStats.FromSamples(samples, reference);
        var gk = stats.Positions.Single(p => p.Position == Position.Goalkeeper);

        Assert.Equal(4, stats.SampleSize);
        Assert.Equal(2, gk.Count);
        Assert.Equal(28, gk.AgeMean);
        Assert.Equal(188, gk.HeightMean);
        Assert.Equal(85, gk.WeightMean);
        Assert.Equal(2 / 3.0, stats.Nationalities.Single(n => n.Code == "BR").Share, 3);
    }
}
