using System.Globalization;
using System.Net;
using System.Text.Json;
using DonoDaBola.Core;
using DonoDaBola.Core.Generation;

namespace DonoDaBola.SeedBuilder;

// Consulta ao Wikidata (CC0) para montar as distribuições por posição (ADR 0003).
// Endpoint, User-Agent e limites: https://www.mediawiki.org/wiki/Wikidata_Query_Service/User_Manual
// e https://foundation.wikimedia.org/wiki/Policy:Wikimedia_Foundation_User-Agent_Policy
public static class Wikidata
{
    public const string Endpoint = "https://query.wikidata.org/sparql";
    public const string UserAgent = "DonoDaBolaSeedBuilder/1.0 (https://github.com/hugo-v-a/DonoDaBola; trabalho academico UNEMAT) dotnet-httpclient-bot";

    // Os 20 clubes da Série A 2026. Ficam só aqui, na ferramenta; nunca vão para data/.
    public static readonly string[] SerieA2026Clubs =
    [
        "Q506832", "Q541744", "Q188277", "Q80845", "Q80955", "Q270995", "Q198032", "Q2536715", "Q17479", "Q2622870",
        "Q38568", "Q35933", "Q80987", "Q80964", "Q5014111", "Q80958", "Q478317", "Q221695", "Q2552872", "Q274465",
    ];

    // Uma linha por jogador (GROUP BY ?p), sem projetar o identificador: a resposta não traz QIDs nem nomes.
    // Altura e massa normalizadas para unidades SI (metro e quilograma).
    public static string BuildQuery() => $$"""
        SELECT (SAMPLE(?posLabel) AS ?pos) (SAMPLE(?h) AS ?height) (SAMPLE(?m) AS ?mass) (SAMPLE(?iso) AS ?nat) WHERE {
          VALUES ?club { {{string.Join(" ", SerieA2026Clubs.Select(q => "wd:" + q))}} }
          ?p wdt:P54 ?club; wdt:P106 wd:Q937857; wdt:P569 ?birth.
          FILTER(YEAR(?birth) >= 1985 && YEAR(?birth) <= 2008)
          OPTIONAL { ?p wdt:P413 ?posItem. ?posItem rdfs:label ?posLabel FILTER(LANG(?posLabel) = "en") }
          OPTIONAL { ?p p:P2048/psv:P2048/wikibase:quantityNormalized/wikibase:quantityAmount ?h }
          OPTIONAL { ?p p:P2067/psv:P2067/wikibase:quantityNormalized/wikibase:quantityAmount ?m }
          OPTIONAL { ?p wdt:P27/wdt:P297 ?iso }
        } GROUP BY ?p
        """;

    // Rótulos em inglês de P413 (posição) para as quatro posições do jogo.
    public static Position? ClassifyPosition(string? label) => label?.ToLowerInvariant() switch
    {
        null => null,
        var l when l.Contains("goalkeeper") => Position.Goalkeeper,
        var l when l.Contains("midfielder") || l.Contains("half") => Position.Midfielder,
        var l when l.Contains("back") || l.Contains("defender") || l.Contains("stopper") || l.Contains("sweeper") || l.Contains("libero") => Position.Defender,
        var l when l.Contains("forward") || l.Contains("striker") || l.Contains("winger") || l.Contains("attacker") => Position.Forward,
        _ => null,
    };

    public static List<PlayerSample> ParseResults(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var samples = new List<PlayerSample>();
        foreach (var row in doc.RootElement.GetProperty("results").GetProperty("bindings").EnumerateArray())
        {
            string? Value(string name) => row.TryGetProperty(name, out var v) ? v.GetProperty("value").GetString() : null;
            double? Number(string name) => Value(name) is { } s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

            if (ClassifyPosition(Value("pos")) is not { } position)
                continue;
            var nationality = Value("nat")?.ToUpperInvariant();
            samples.Add(new PlayerSample(
                position,
                Number("height") is { } meters ? Math.Round(meters * 100) : null,
                Number("mass"),
                nationality is [>= 'A' and <= 'Z', >= 'A' and <= 'Z'] ? nationality : null));
        }
        return samples;
    }

    // Uma consulta por vez. Em 429/503, espera o Retry-After (ou 60 s) e tenta de novo, até 3 vezes.
    public static async Task<string> QueryAsync(HttpClient http, string sparql)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new FormUrlEncodedContent([new("query", sparql)]),
            };
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.ParseAdd("application/sparql-results+json");

            using var response = await http.SendAsync(request);
            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable && attempt < 3)
            {
                var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                Console.WriteLine($"Wikidata pediu para esperar ({(int)response.StatusCode}); nova tentativa em {wait.TotalSeconds:F0} s.");
                await Task.Delay(wait);
                continue;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
