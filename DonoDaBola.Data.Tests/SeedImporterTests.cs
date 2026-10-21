using System.Text;
using DonoDaBola.Core;
using DonoDaBola.Core.Tests;

namespace DonoDaBola.Data.Tests;

public class SeedImporterTests
{
    private static Stream Json(SeedFile seed) => new MemoryStream(Encoding.UTF8.GetBytes(seed.ToJson()));

    [Fact]
    public void Import_LoadsSeedIntoEmptyDatabase()
    {
        using var db = GameDatabase.Open("Data Source=:memory:");

        var count = SeedImporter.Import(db, Json(new SeedFile([TestPlayers.CreateClub("a"), TestPlayers.CreateClub("b")])));

        Assert.Equal(2, count);
        Assert.Equal(36, db.Count(new PlayerQuery()));
    }

    [Fact]
    public void Export_ProducesAnImportableSeed()
    {
        using var source = GameDatabase.Open("Data Source=:memory:");
        SeedImporter.Import(source, Json(new SeedFile([TestPlayers.CreateClub("a")])));
        using var target = GameDatabase.Open("Data Source=:memory:");

        SeedImporter.Import(target, Json(source.Export()));

        Assert.Equal(source.GetAll(), target.GetAll(), new ClubComparer());
    }

    public static TheoryData<string, SeedFile> InvalidSeeds => new()
    {
        { "versão", new SeedFile([TestPlayers.CreateClub("a")]) { Version = 2 } },
        { "4-4-2", new SeedFile([TestPlayers.CreateClub("a") is var c ? c with { Squad = [.. c.Squad.Where(p => p.Position != Position.Goalkeeper)] } : null!]) },
        { "id repetido", new SeedFile([TestPlayers.CreateClub("a"), TestPlayers.CreateClub("a")]) },
    };

    [Theory]
    [MemberData(nameof(InvalidSeeds))]
    public void Import_RejectsInvalidSeedWithoutWriting(string reason, SeedFile seed)
    {
        using var db = GameDatabase.Open("Data Source=:memory:");

        Assert.ThrowsAny<Exception>(() => SeedImporter.Import(db, Json(seed)));
        Assert.True(db.IsEmpty, reason);
    }

    [Fact]
    public void Import_RefusesNonEmptyDatabase()
    {
        using var db = GameDatabase.Open("Data Source=:memory:");
        SeedImporter.Import(db, Json(new SeedFile([TestPlayers.CreateClub("a")])));

        Assert.Throws<InvalidOperationException>(() => SeedImporter.Import(db, Json(new SeedFile([TestPlayers.CreateClub("b")]))));
    }

    // Club guarda o elenco numa lista; compara o conteúdo, não a referência.
    private sealed class ClubComparer : IEqualityComparer<Club>
    {
        public bool Equals(Club? x, Club? y) =>
            x!.Id == y!.Id && x.Name == y.Name && x.ShortName == y.ShortName && x.Squad.SequenceEqual(y.Squad);
        public int GetHashCode(Club obj) => obj.Id.GetHashCode();
    }
}

public class DefaultSeedTests
{
    // O seed publicado (data/seed.json) vai embutido no Data: funciona sem caminho de arquivo, inclusive no celular.
    [Fact]
    public void ImportDefault_Loads20Clubs()
    {
        using var db = GameDatabase.Open("Data Source=:memory:");

        Assert.Equal(20, SeedImporter.ImportDefault(db));
        Assert.Equal(20, db.GetAll().Count);
        Assert.True(db.Count(new PlayerQuery()) >= 20 * 25);
    }
}
