// Ferramenta manual, fora do jogo: gera os dados iniciais em data/ (ADR 0003). Rode a partir da raiz do repositório.
//   dotnet run --project tools/DonoDaBola.SeedBuilder -- stats   (consulta o Wikidata e grava data/player-stats.json)
//   dotnet run --project tools/DonoDaBola.SeedBuilder -- seed    (gera data/seed.json a partir das estatísticas, sem internet)
using DonoDaBola.Core.Generation;
using DonoDaBola.SeedBuilder;

const string StatsPath = "data/player-stats.json", SeedPath = "data/seed.json";
var seed = Option("--seed", 2026);
var seasonStart = new DateOnly(Option("--season", 2026), 1, 1);

switch (args.FirstOrDefault())
{
    case "stats":
        using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) })
        {
            Console.WriteLine("Consultando o Wikidata (uma consulta, pode levar até 60 s)...");
            var samples = Wikidata.ParseResults(await Wikidata.QueryAsync(http, Wikidata.BuildQuery()));
            var stats = PlayerStats.FromSamples(samples);
            File.WriteAllText(StatsPath, stats.ToJson() + "\n");
            Console.WriteLine($"{stats.SampleSize} jogadores na amostra; estatísticas gravadas em {StatsPath}.");
        }
        break;

    case "seed":
        {
            var stats = PlayerStats.FromJson(File.ReadAllText(StatsPath));
            var league = LeagueGenerator.Create(new Random(seed), stats, seasonStart);
            File.WriteAllText(SeedPath, league.ToJson() + "\n");
            Console.WriteLine($"{league.Clubs.Count} clubes e {league.Clubs.Sum(c => c.Squad.Count)} jogadores gravados em {SeedPath} (seed {seed}).");
        }
        break;

    default:
        Console.WriteLine("Uso: SeedBuilder stats | seed [--seed 2026] [--season 2026]");
        return 1;
}
return 0;

int Option(string name, int fallback)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var value) ? value : fallback;
}
