using DonoDaBola.Core;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Data.Tests;

// Caminho completo Data -> Core, igual ao que a Stride vai fazer: pasta -> banco -> seed -> repositório -> partida.
public sealed class IntegrationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ddb-integracao-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void FullPath_SeedToMatch()
    {
        MatchResult first;
        using (var db = GameDatabase.OpenInDirectory(_dir))
        {
            Assert.True(db.IsEmpty);
            SeedImporter.ImportDefault(db);

            IClubRepository clubs = db;
            var home = clubs.Get("clu01")!;
            var away = clubs.Get("clu02")!;

            first = MatchEngine.Simulate(home, away, new Random(42));
            var second = MatchEngine.Simulate(home, away, new Random(42));

            Assert.Equal(first.HomeGoals, first.Events.Count(e => e.Type == MatchEventType.Goal && e.Club.Id == home.Id));
            Assert.Equal(first.AwayGoals, first.Events.Count(e => e.Type == MatchEventType.Goal && e.Club.Id == away.Id));
            Assert.Equal(first.Events.Select(e => e.Minute).Order(), first.Events.Select(e => e.Minute));
            Assert.Equal((first.HomeGoals, first.AwayGoals), (second.HomeGoals, second.AwayGoals));
            Assert.Equal(first.Events, second.Events);
        }

        // Reabrir a mesma pasta não reimporta: o banco já é o jogo salvo.
        using var reopened = GameDatabase.OpenInDirectory(_dir);
        Assert.False(reopened.IsEmpty);
        Assert.Equal(20, reopened.GetAll().Count);

        // Clubes lidos de novo do disco geram a mesma partida: pega coluna mal mapeada ou ordem de elenco alterada.
        // Compara por projeção (o record Club compara o elenco por referência).
        var again = MatchEngine.Simulate(reopened.Get("clu01")!, reopened.Get("clu02")!, new Random(42));
        static object Shape(MatchResult r) =>
            (r.HomeGoals, r.AwayGoals, string.Join(";", r.Events.Select(e => $"{e.Minute}{e.Type}{e.Player.Id}{e.Goalkeeper.Id}")));
        Assert.Equal(Shape(first), Shape(again));
    }
}
