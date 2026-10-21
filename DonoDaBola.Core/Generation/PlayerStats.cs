using System.Text.Json;

namespace DonoDaBola.Core.Generation;

// Distribuições agregadas por posição (data/player-stats.json). Sem nomes, IDs ou indivíduos (ADR 0003).
public record PlayerStats(int SampleSize, IReadOnlyList<PositionStats> Positions, IReadOnlyList<NationalityShare> Nationalities)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, NewLine = "\n", RespectNullableAnnotations = true };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static PlayerStats FromJson(string json) =>
        JsonSerializer.Deserialize<PlayerStats>(json, Options) ?? throw new JsonException("Estatísticas vazias.");

    public PositionStats For(Position position) =>
        Positions.SingleOrDefault(p => p.Position == position)
        ?? throw new InvalidOperationException($"Sem estatísticas para {position}.");

    // Agrega amostras individuais (vindas do Wikidata pela ferramenta SeedBuilder).
    // A idade não entra: o Wikidata não identifica o elenco atual (ADR 0003); ela é parâmetro do PlayerGenerator.
    public static PlayerStats FromSamples(IReadOnlyCollection<PlayerSample> samples, int topNationalities = 10)
    {
        var positions = samples.GroupBy(s => s.Position).OrderBy(g => g.Key).Select(g =>
        {
            if (!g.Any(s => s.HeightCm is not null) || !g.Any(s => s.WeightKg is not null))
                throw new InvalidDataException($"A amostra de {g.Key} não tem altura ou peso; o gerador sairia com 150 cm / 50 kg.");
            var (heightMean, heightSd) = MeanSd(g.Where(s => s.HeightCm is not null).Select(s => s.HeightCm!.Value));
            var (weightMean, weightSd) = MeanSd(g.Where(s => s.WeightKg is not null).Select(s => s.WeightKg!.Value));
            return new PositionStats(g.Key, g.Count(), heightMean, heightSd, weightMean, weightSd);
        }).ToList();
        if (positions.Count != Enum.GetValues<Position>().Length)
            throw new InvalidDataException("A amostra não cobre as quatro posições.");

        var known = samples.Where(s => s.Nationality is not null).ToList();
        var nationalities = known.GroupBy(s => s.Nationality!)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(topNationalities)
            .Select(g => new NationalityShare(g.Key, Math.Round(g.Count() / (double)known.Count, 4)))
            .ToList();

        return new PlayerStats(samples.Count, positions, nationalities);
    }

    private static (double Mean, double Sd) MeanSd(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return (0, 0);
        var mean = list.Average();
        var sd = list.Count < 2 ? 0 : Math.Sqrt(list.Sum(v => (v - mean) * (v - mean)) / (list.Count - 1));
        return (Math.Round(mean, 2), Math.Round(sd, 2));
    }
}

public record PositionStats(Position Position, int Count, double HeightMean, double HeightSd, double WeightMean, double WeightSd);

public record NationalityShare(string Code, double Share);

// Um jogador real da amostra; só existe em memória dentro da ferramenta, nunca é gravado.
public record PlayerSample(Position Position, double? HeightCm, double? WeightKg, string? Nationality);
