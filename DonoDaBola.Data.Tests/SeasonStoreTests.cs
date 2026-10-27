using DonoDaBola.Core;
using DonoDaBola.Core.Competition;
using DonoDaBola.Core.Tests;
using Microsoft.Data.Sqlite;

namespace DonoDaBola.Data.Tests;

// Temporada salva no banco (esquema v2): retomada, gravação atômica da rodada, saves e migração da v1.
public sealed class SeasonStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ddb-temporada-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static GameDatabase SeededDb()
    {
        var db = GameDatabase.Open("Data Source=:memory:");
        SeedImporter.ImportDefault(db);
        return db;
    }

    private static void Play(Season season, int rounds)
    {
        for (var i = 0; i < rounds && !season.IsFinished; i++)
            season.AdvanceRound();
    }

    private static string Shape(Season s) =>
        string.Join(";", s.Table().Select(r => $"{r.Club.Id}:{r.Points}:{r.GoalsFor}:{r.GoalsAgainst}"))
        + "|" + string.Join(";", s.TopScorers(10).Select(t => $"{t.Player.Id}:{t.Goals}"));

    [Fact]
    public void LoadSeason_WithoutSeason_ReturnsNull()
    {
        using var db = SeededDb();
        ISeasonStore store = db;
        Assert.Null(store.LoadSeason());
    }

    [Fact]
    public void SaveAndLoad_MidSeason_ContinuesExactlyLikeAnUninterruptedSeason()
    {
        using var db = SeededDb();
        var straight = Season.Create(db.GetAll(), 42);
        Play(straight, 38);

        var season = Season.Create(db.GetAll(), 42);
        Play(season, 1);
        db.SaveSeason(season);   // grava a temporada nova
        Play(season, 18);
        db.SaveSeason(season);   // grava só as rodadas novas
        db.SaveSeason(season);   // idempotente

        var loaded = db.LoadSeason()!;
        Assert.Equal(20, loaded.CurrentRound);
        Assert.Equal(Shape(season), Shape(loaded));
        Assert.Equal(season.DrawKeys.OrderBy(k => k.Key), loaded.DrawKeys.OrderBy(k => k.Key));
        Assert.Equal(season.Config, loaded.Config);

        Play(loaded, 38);
        Assert.Equal(Shape(straight), Shape(loaded));
    }

    [Fact]
    public void Draw_IsStable_AfterSaveAndLoad()
    {
        using var db = SeededDb();
        var season = Season.Create(db.GetAll(), 7);
        db.SaveSeason(season); // antes da 1ª rodada todos empatam em tudo: só o sorteio ordena
        var order = season.Table().Select(r => r.Club.Id).ToList();
        Assert.Equal(order, db.LoadSeason()!.Table().Select(r => r.Club.Id));
        Assert.Equal(order, db.LoadSeason()!.Table().Select(r => r.Club.Id));
    }

    [Fact]
    public void SaveSeason_FailureMidRound_LeavesTheRoundUnwritten()
    {
        using var db = SeededDb();
        var season = Season.Create(db.GetAll(), 3);
        Play(season, 2);

        // Gol de um jogador que existe só em memória: a gravação desse gol viola a chave estrangeira
        // depois de os jogos anteriores da rodada 2 já terem sido gravados na transação.
        var last = season.Goals.Where(g => g.Round == 2).MaxBy(g => g.Index)!;
        Assert.True(last.Index > 0, "o gol precisa estar no meio da rodada");
        var ghost = TestPlayers.Create("fantasma", Position.Forward);
        var clubs = season.Matches.Select(m => m.Home).DistinctBy(c => c.Id)
            .Select(c => c.Id == last.ClubId ? c with { Squad = [.. c.Squad, ghost] } : c).ToList();
        var goals = season.Goals.Select(g => g == last ? g with { PlayerId = ghost.Id } : g).ToList();
        var broken = Season.Restore(clubs, season.Seed, season.Config, season.Matches, goals, season.DrawKeys);

        var ok = Season.Create(db.GetAll(), 3);
        Play(ok, 1);
        db.SaveSeason(ok); // rodada 1 gravada
        Assert.Throws<SqliteException>(() => db.SaveSeason(broken));

        var loaded = db.LoadSeason()!;
        Assert.Equal(2, loaded.CurrentRound); // a rodada 2 não ficou pela metade
        Assert.All(loaded.Round(2), m => Assert.Null(m.HomeGoals));
        Assert.DoesNotContain(loaded.Goals, g => g.Round == 2);

        // Depois do rollback a conexão segue boa: a temporada certa grava a rodada 2.
        db.SaveSeason(season);
        Assert.Equal(3, db.LoadSeason()!.CurrentRound);
    }

    [Fact]
    public void SaveSeason_StaleSeason_Throws_AndKeepsTheDatabase()
    {
        using var db = SeededDb();
        var season = Season.Create(db.GetAll(), 4);
        Play(season, 5);
        db.SaveSeason(season);

        var stale = Season.Create(db.GetAll(), 4); // mesma seed, mas parado na rodada 3
        Play(stale, 3);
        Assert.Throws<InvalidOperationException>(() => db.SaveSeason(stale));
        Assert.Equal(6, db.LoadSeason()!.CurrentRound);
    }

    [Fact]
    public void SaveSeason_DifferentFixtures_Throws()
    {
        using var db = SeededDb();
        var season = Season.Create(db.GetAll(), 5);
        db.SaveSeason(season);
        Play(season, 1);

        // Mesma seed, mas com o mando do 1º jogo trocado: o placar não pode cair no jogo errado.
        var swapped = season.Matches.Select((m, i) => i == 0 ? m with { Home = m.Away, Away = m.Home } : m);
        var other = Season.Restore(db.GetAll(), season.Seed, season.Config, swapped, season.Goals, season.DrawKeys);
        Assert.Throws<InvalidOperationException>(() => db.SaveSeason(other));
        Assert.Equal(1, db.LoadSeason()!.CurrentRound);
    }

    [Theory]
    [InlineData("UPDATE season SET start_date = '28/01/2026'")]                  // data fora do formato
    [InlineData("UPDATE season SET resume_date = '2026-01-01'")]                 // volta antes da pausa
    [InlineData("DELETE FROM season_club WHERE draw_key = 0")]                   // falta a chave de um clube
    [InlineData("UPDATE season_match SET home_goals = 1, away_goals = 0 WHERE round = 5 AND idx = 3")] // rodada pela metade
    public void LoadSeason_CorruptRows_ThrowInvalidData(string corruption)
    {
        using var db = SeededDb();
        var season = Season.Create(db.GetAll(), 6);
        Play(season, 2);
        db.SaveSeason(season);

        using (var cmd = new SqliteCommand(corruption, db.Connection))
            cmd.ExecuteNonQuery();

        Assert.Throws<InvalidDataException>(() => db.LoadSeason());
    }

    [Fact]
    public void SaveSeason_OtherSeason_Throws()
    {
        using var db = SeededDb();
        db.SaveSeason(Season.Create(db.GetAll(), 1));
        Assert.Throws<InvalidOperationException>(() => db.SaveSeason(Season.Create(db.GetAll(), 2)));
    }

    [Fact]
    public void SaveSlot_MidSeason_LoadGoesBackAndContinuesTheSame()
    {
        using var db = GameDatabase.OpenInDirectory(_dir);
        SeedImporter.ImportDefault(db);
        var saves = new SaveGameStore(db, Path.Combine(_dir, "saves"));

        var straight = Season.Create(db.GetAll(), 11);
        Play(straight, 38);

        var season = Season.Create(db.GetAll(), 11);
        Play(season, 19);
        db.SaveSeason(season);
        saves.Save("meio");

        Play(season, 6);
        db.SaveSeason(season);
        Assert.Equal(26, db.LoadSeason()!.CurrentRound);

        saves.Load("meio");
        var resumed = db.LoadSeason()!;
        Assert.Equal(20, resumed.CurrentRound);
        Play(resumed, 38);
        Assert.Equal(Shape(straight), Shape(resumed));
    }

    [Fact]
    public void VersionOneDatabase_OpensInVersionTwo_WithDataIntact()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "v1.db");
        var club = TestPlayers.CreateClub("velho");
        CreateVersionOne(path, club);

        using (var db = GameDatabase.Open($"Data Source={path};Pooling=False"))
        {
            Assert.Equal(2, db.UserVersion);
            var read = db.Get("velho")!;
            Assert.Equal(club.Squad.Select(p => p.Id), read.Squad.Select(p => p.Id));
            Assert.Equal(club.Squad.Select(p => (p.BirthDate, p.Technical.Finishing)), read.Squad.Select(p => (p.BirthDate, p.Technical.Finishing)));
            Assert.Null(db.LoadSeason());
        }

        // Save da v1 carregado num jogo v2 sobe de versão.
        using var current = GameDatabase.Open("Data Source=:memory:");
        var saves = new SaveGameStore(current, _dir);
        File.Move(path, Path.Combine(_dir, "antigo.db"));
        saves.Load("antigo");
        Assert.Equal(2, current.UserVersion);
        Assert.Equal(["velho"], current.GetAll().Select(c => c.Id));
    }

    // Banco como a versão 1 deixava: só o primeiro script de migração, com um clube.
    private static void CreateVersionOne(string path, Club club)
    {
        using var conn = new SqliteConnection($"Data Source={path};Pooling=False");
        conn.Open();
        using (var cmd = new SqliteCommand($"{Schema.Migrations[0]}\nPRAGMA user_version = 1;", conn))
            cmd.ExecuteNonQuery();
        using (var cmd = new SqliteCommand("INSERT INTO club (id, name, short_name) VALUES ($id, $n, $s)", conn))
        {
            cmd.Parameters.AddWithValue("$id", club.Id);
            cmd.Parameters.AddWithValue("$n", club.Name);
            cmd.Parameters.AddWithValue("$s", club.ShortName);
            cmd.ExecuteNonQuery();
        }
        foreach (var p in club.Squad)
        {
            using var cmd = new SqliteCommand(Schema.InsertPlayer, conn);
            Schema.BindPlayer(cmd, p, club.Id);
            cmd.ExecuteNonQuery();
        }
    }
}
