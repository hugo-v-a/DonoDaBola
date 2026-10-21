using DonoDaBola.Core;
using DonoDaBola.Core.Tests;

namespace DonoDaBola.Data.Tests;

public sealed class SaveGameStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ddb-saves-{Guid.NewGuid():N}");
    private readonly GameDatabase _db = GameDatabase.Open("Data Source=:memory:");

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void SaveThenLoad_RestoresTheGame()
    {
        var store = new SaveGameStore(_db, _dir);
        _db.Insert([TestPlayers.CreateClub("a")]);
        store.Save("slot1");

        _db.Insert([TestPlayers.CreateClub("b")]);
        Assert.Equal(2, _db.GetAll().Count);

        store.Load("slot1");
        Assert.Equal(["a"], _db.GetAll().Select(c => c.Id));
    }

    [Fact]
    public void ListAndDelete()
    {
        var store = new SaveGameStore(_db, _dir);
        store.Save("b");
        store.Save("a");

        Assert.Equal(["a", "b"], store.List());

        store.Delete("a");
        Assert.Equal(["b"], store.List());
    }

    [Fact]
    public void Load_MissingSlot_Throws()
    {
        var store = new SaveGameStore(_db, _dir);

        Assert.Throws<FileNotFoundException>(() => store.Load("nada"));
    }

    [Fact]
    public void Load_EmptyFile_KeepsCurrentGame()
    {
        var store = new SaveGameStore(_db, _dir);
        _db.Insert([TestPlayers.CreateClub("a")]);
        Directory.CreateDirectory(_dir);
        File.WriteAllBytes(Path.Combine(_dir, "vazio.db"), []);

        Assert.Throws<InvalidDataException>(() => store.Load("vazio"));
        Assert.Equal(["a"], _db.GetAll().Select(c => c.Id));
    }

    [Fact]
    public void Load_SaveFromNewerVersion_KeepsCurrentGame()
    {
        var store = new SaveGameStore(_db, _dir);
        _db.Insert([TestPlayers.CreateClub("a")]);
        store.Save("futuro");
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(_dir, "futuro.db")};Pooling=False"))
        {
            conn.Open();
            new Microsoft.Data.Sqlite.SqliteCommand("PRAGMA user_version = 99", conn).ExecuteNonQuery();
        }
        _db.Insert([TestPlayers.CreateClub("b")]);

        Assert.Throws<InvalidDataException>(() => store.Load("futuro"));
        Assert.Equal(["a", "b"], _db.GetAll().Select(c => c.Id));
    }

    [Fact]
    public void List_HidesFilesThatAreNotValidSlots()
    {
        var store = new SaveGameStore(_db, _dir);
        store.Save("ok");
        File.WriteAllBytes(Path.Combine(_dir, "meu save.db"), []);

        Assert.Equal(["ok"], store.List());
    }

    [Fact]
    public void SlotNames_IgnoreCase()
    {
        var store = new SaveGameStore(_db, _dir);
        store.Save("Carreira");

        Assert.Equal(["carreira"], store.List());
        store.Load("CARREIRA");
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("nul")]
    [InlineData("com1")]
    [InlineData("../fora")]
    [InlineData("a/b")]
    [InlineData("")]
    [InlineData("C:")]
    public void InvalidSlotNames_AreRejected(string slot)
    {
        var store = new SaveGameStore(_db, _dir);

        Assert.Throws<ArgumentException>(() => store.Save(slot));
    }
}
