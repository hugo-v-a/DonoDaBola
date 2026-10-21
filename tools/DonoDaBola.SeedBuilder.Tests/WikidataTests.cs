using DonoDaBola.Core;
using DonoDaBola.Core.Generation;

namespace DonoDaBola.SeedBuilder.Tests;

public class WikidataTests
{
    [Theory]
    [InlineData("goalkeeper", Position.Goalkeeper)]
    [InlineData("defender", Position.Defender)]
    [InlineData("centre-back", Position.Defender)]
    [InlineData("full-back", Position.Defender)]
    [InlineData("stopper", Position.Defender)]
    [InlineData("midfielder", Position.Midfielder)]
    [InlineData("defensive midfielder", Position.Midfielder)]
    [InlineData("wing half", Position.Midfielder)]
    [InlineData("forward", Position.Forward)]
    [InlineData("winger", Position.Forward)]
    [InlineData("attacker", Position.Forward)]
    [InlineData("centre-forward", Position.Forward)]
    public void ClassifyPosition_MapsWikidataLabels(string label, Position expected) =>
        Assert.Equal(expected, Wikidata.ClassifyPosition(label));

    [Theory]
    [InlineData(null)]
    [InlineData("coach")]
    public void ClassifyPosition_UnknownIsNull(string? label) => Assert.Null(Wikidata.ClassifyPosition(label));

    // Resposta gravada no formato do endpoint (application/sparql-results+json), sem rede no teste.
    private const string Response = """
        { "head": { "vars": ["pos", "height", "mass", "nat"] },
          "results": { "bindings": [
            { "pos": { "type": "literal", "value": "goalkeeper" }, "height": { "type": "literal", "value": "1.91" },
              "mass": { "type": "literal", "value": "86" }, "nat": { "type": "literal", "value": "BR" } },
            { "pos": { "type": "literal", "value": "winger" }, "nat": { "type": "literal", "value": "ar" } },
            { "pos": { "type": "literal", "value": "coach" }, "height": { "type": "literal", "value": "1.80" } },
            { "height": { "type": "literal", "value": "1.75" } },
            { "pos": { "type": "literal", "value": "defender" }, "height": { "type": "literal", "value": "180" },
              "mass": { "type": "literal", "value": "750" }, "nat": { "type": "literal", "value": "BR" } }
          ] } }
        """;

    [Fact]
    public void ParseResults_ConvertsUnitsAndSkipsUnknownPositions()
    {
        var samples = Wikidata.ParseResults(Response);

        Assert.Equal(3, samples.Count);
        Assert.Equal(new PlayerSample(Position.Goalkeeper, 191, 86, "BR"), samples[0]);
        Assert.Equal(new PlayerSample(Position.Forward, null, null, "AR"), samples[1]);
        // 180 "metros" e 750 kg são erros de cadastro: descartados, para não distorcer as médias.
        Assert.Equal(new PlayerSample(Position.Defender, null, null, "BR"), samples[2]);
    }

    [Fact]
    public void Query_ProjectsNoIdentifiers()
    {
        var select = Wikidata.BuildQuery().Split("WHERE")[0];

        Assert.DoesNotContain("?p ", select.Replace("(", " ").Replace(")", " ") + " ");
        Assert.Contains("GROUP BY ?p", Wikidata.BuildQuery());
        Assert.Contains("wdt:P1532", Wikidata.BuildQuery()); // país pelo esporte vem antes da cidadania (dupla nacionalidade)
        Assert.Contains("DonoDaBolaSeedBuilder/", Wikidata.UserAgent);
        Assert.Contains("github.com/hugo-v-a/DonoDaBola", Wikidata.UserAgent);
    }
}
