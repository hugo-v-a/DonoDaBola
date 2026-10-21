using System.Reflection;
using DonoDaBola.Core;
using DonoDaBola.Core.Tests;
using Microsoft.Data.Sqlite;

namespace DonoDaBola.Data.Tests;

public class GameDatabaseTests
{
    private static GameDatabase NewDb() => GameDatabase.Open("Data Source=:memory:");

    // Cada atributo com um valor diferente: uma troca de colunas no mapeamento quebra a ida e volta.
    private static Player Full(string id) => TestPlayers.Create(id, Position.Forward) with
    {
        BirthDate = new DateOnly(1999, 7, 3), HeightCm = 181, WeightKg = 77, Potential = 17,
        PreferredFoot = PreferredFoot.Left, Nationality = "UY", DataSource = DataSource.Wikidata,
        Technical = new() { Passing = 1, Dribbling = 2, Finishing = 3, Crossing = 4, Tackling = 5, Marking = 6, Heading = 7, SetPieces = 8 },
        Mental = new() { Vision = 9, Positioning = 10, Decisions = 11, Leadership = 12, Composure = 13, Aggression = 14 },
        Physical = new() { Pace = 15, Stamina = 16, Strength = 18, Agility = 19 },
        Goalkeeping = new() { Reflexes = 20, Positioning = 3, RushingOut = 5, Kicking = 7 },
    };

    [Fact]
    public void Open_AppliesSchemaOnce()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ddb-{Guid.NewGuid():N}.db");
        try
        {
            using (var db = GameDatabase.Open($"Data Source={path};Pooling=False"))
                Assert.Equal(GameDatabase.SchemaVersion, db.UserVersion);
            using (var again = GameDatabase.Open($"Data Source={path};Pooling=False"))
                Assert.Equal(GameDatabase.SchemaVersion, again.UserVersion);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Open_RejectsNewerSchema()
    {
        using var conn = new SqliteConnection("Data Source=newer;Mode=Memory;Cache=Shared");
        conn.Open();
        new SqliteCommand("PRAGMA user_version = 99", conn).ExecuteNonQuery();

        Assert.Throws<InvalidOperationException>(() => GameDatabase.Open("Data Source=newer;Mode=Memory;Cache=Shared"));
    }

    [Fact]
    public void Clubs_RoundTripWithAllFields()
    {
        using var db = NewDb();
        var club = TestPlayers.CreateClub("a") is var c ? c with { Squad = [.. c.Squad, Full("extra")] } : null!;

        db.Insert([club]);

        Assert.Equal(club.Squad.OrderBy(p => p.Id), db.Get("a")!.Squad.OrderBy(p => p.Id));
        Assert.Equal(["a"], db.GetAll().Select(x => x.Id));
        Assert.Null(db.Get("nao-existe"));
    }

    [Theory]
    [InlineData("passing", "21")]
    [InlineData("gk_kicking", "0")]
    [InlineData("height_cm", "140")]
    [InlineData("weight_kg", "130")]
    [InlineData("preferred_foot", "'x'")]
    [InlineData("birth_date", "'03/07/1999'")]
    [InlineData("nationality", "'br'")]
    [InlineData("data_source", "'inventado'")]
    public void Schema_ChecksRejectInvalidValues(string column, string value)
    {
        using var db = NewDb();
        db.Insert([TestPlayers.CreateClub("a")]);

        var ex = Assert.Throws<SqliteException>(() =>
            new SqliteCommand($"UPDATE player SET {column} = {value} WHERE id = 'a-Forward-0'", db.Connection).ExecuteNonQuery());
        Assert.Contains("CHECK", ex.Message);
    }

    [Fact]
    public void Schema_RejectsUnknownClub()
    {
        using var db = NewDb();
        db.Insert([TestPlayers.CreateClub("a")]);

        var ex = Assert.Throws<SqliteException>(() =>
            new SqliteCommand("UPDATE player SET club_id = 'zzz' WHERE id = 'a-Forward-0'", db.Connection).ExecuteNonQuery());
        Assert.Contains("FOREIGN KEY", ex.Message);
    }

    [Fact]
    public void Find_PagesAndFilters()
    {
        using var db = NewDb();
        db.Insert([TestPlayers.CreateClub("a"), TestPlayers.CreateClub("b")]);

        var all = db.Find(new PlayerQuery(Limit: 200));
        var page2 = db.Find(new PlayerQuery(Offset: 10, Limit: 10));
        var defendersOfB = db.Find(new PlayerQuery(ClubId: "b", Position: Position.Defender));

        Assert.Equal(36, all.Count);
        Assert.Equal(all.Skip(10).Take(10), page2);
        Assert.Equal(6, defendersOfB.Count);
        Assert.All(defendersOfB, p => Assert.StartsWith("b-Defender", p.Id));
        Assert.Equal(6, db.Count(new PlayerQuery(ClubId: "b", Position: Position.Defender)));
        Assert.Equal(36, db.Count(new PlayerQuery()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Find_RejectsInvalidLimit(int limit)
    {
        using var db = NewDb();

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Find(new PlayerQuery(Limit: limit)));
    }

    // O banco é o save: a data gravada não pode depender do idioma/calendário do aparelho.
    [Fact]
    public void BirthDate_IsStoredInIsoRegardlessOfCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new("th-TH"); // calendário budista
            using var db = NewDb();
            db.Insert([TestPlayers.CreateClub("a")]);

            var raw = new SqliteCommand("SELECT birth_date FROM player LIMIT 1", db.Connection).ExecuteScalar();
            Assert.Equal("2000-01-01", raw);
            Assert.Equal(new DateOnly(2000, 1, 1), db.GetAll()[0].Squad[0].BirthDate);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Data_DoesNotReferenceStride()
    {
        var data = typeof(GameDatabase).Assembly;

        Assert.DoesNotContain(data.GetReferencedAssemblies(), a => a.Name!.StartsWith("Stride"));
    }
}
