using System.Text.RegularExpressions;
using DonoDaBola.Core;
using DonoDaBola.Core.Generation;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.SeedBuilder.Tests;

// Confere os arquivos publicados em data/ (ADR 0003): sem dados identificáveis e reproduzíveis pela seed.
public class PublishedDataTests
{
    private static string RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DonoDaBola.slnx")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada."), relative));
    }

    private static readonly string SeedJson = RepoFile("data/seed.json");
    private static readonly string StatsJson = RepoFile("data/player-stats.json");

    [Theory]
    [InlineData("data/seed.json")]
    [InlineData("data/player-stats.json")]
    public void PublishedFiles_HaveNoWikidataIdentifiers(string file)
    {
        var text = RepoFile(file);

        Assert.DoesNotMatch(new Regex(@"\bQ\d{2,}\b"), text);
        Assert.DoesNotContain("wikidata.org", text);
    }

    [Fact]
    public void Seed_HasOnlyGeneratedNamesAndProfiles()
    {
        var players = SeedFile.FromJson(SeedJson).Clubs.SelectMany(c => c.Squad).ToList();

        Assert.All(players, p =>
        {
            Assert.True(NameGenerator.IsGenerated(p.Name), p.Name);
            Assert.Equal(DataSource.Generated, p.DataSource);
        });
    }

    [Fact]
    public void Seed_Has20ClubsThatCanField442()
    {
        var clubs = SeedFile.FromJson(SeedJson).Clubs;

        Assert.Equal(20, clubs.Count);
        Assert.All(clubs, c => Assert.True(Lineup.HasNatural442(c), c.Name));
    }

    [Fact]
    public void Stats_HaveNoIndividuals()
    {
        Assert.DoesNotContain("\"Name\"", StatsJson);
        Assert.DoesNotContain("BirthDate", StatsJson);
        Assert.True(PlayerStats.FromJson(StatsJson).SampleSize > 500);
    }

    // Regerar com a mesma seed e as mesmas estatísticas produz exatamente o seed publicado.
    [Fact]
    public void Seed_IsReproducibleFromStats()
    {
        var regenerated = LeagueGenerator.Create(new Random(2026), PlayerStats.FromJson(StatsJson), new DateOnly(2026, 1, 1));

        Assert.Equal(SeedJson.ReplaceLineEndings("\n").TrimEnd(), regenerated.ToJson());
    }
}
