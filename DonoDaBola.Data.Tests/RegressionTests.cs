using DonoDaBola.Core.Competition;

namespace DonoDaBola.Data.Tests;

// Tabela final da seed 42 com os 20 clubes do seed.json, gravada na main 6676904 (antes dos cartões).
// Mesmos clubes e seed do Sandbox (--temporada --seed 42), mas com DisciplineRules.Off (o Sandbox usa Default).
public class RegressionTests
{
    public const string Season42 =
        "clu04:84:75:25;clu02:79:77:31;clu03:73:58:30;clu01:73:58:31;clu06:69:62:34;clu12:64:43:38;clu05:62:63:44;" +
        "clu09:61:51:38;clu08:58:51:43;clu14:50:46:48;clu10:46:40:52;clu07:45:49:53;clu20:43:38:56;clu11:42:42:50;" +
        "clu13:42:44:56;clu18:36:43:60;clu15:30:26:54;clu17:30:29:64;clu19:30:28:65;clu16:27:26:77";

    [Fact]
    public void SeedJsonSeason42_SameAsReference()
    {
        using var db = GameDatabase.Open("Data Source=:memory:");
        SeedImporter.ImportDefault(db);
        var season = Season.Create(db.GetAll(), 42, discipline: DonoDaBola.Core.Simulation.DisciplineRules.Off);
        while (!season.IsFinished) season.AdvanceRound();
        Assert.Equal(Season42, string.Join(";", season.Table().Select(x => $"{x.Club.Id}:{x.Points}:{x.GoalsFor}:{x.GoalsAgainst}")));
    }
}
