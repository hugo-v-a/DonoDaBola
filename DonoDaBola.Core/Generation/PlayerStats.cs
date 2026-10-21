namespace DonoDaBola.Core.Generation;

// Distribuições agregadas por posição (data/player-stats.json). Sem nomes, IDs ou indivíduos (ADR 0003).
public record PlayerStats(int SampleSize, IReadOnlyList<PositionStats> Positions, IReadOnlyList<NationalityShare> Nationalities)
{
    public PositionStats For(Position position) =>
        Positions.SingleOrDefault(p => p.Position == position)
        ?? throw new InvalidOperationException($"Sem estatísticas para {position}.");

    // Agrega amostras individuais (vindas do Wikidata pela ferramenta SeedBuilder). Idade calculada na data de referência.
    public static PlayerStats FromSamples(IReadOnlyCollection<PlayerSample> samples, DateOnly reference, int topNationalities = 10)
    {
        var positions = samples.GroupBy(s => s.Position).OrderBy(g => g.Key).Select(g =>
        {
            var (ageMean, ageSd) = MeanSd(g.Select(s => (double)s.BirthDate.ToAge(reference)));
            var (heightMean, heightSd) = MeanSd(g.Where(s => s.HeightCm is not null).Select(s => s.HeightCm!.Value));
            var (weightMean, weightSd) = MeanSd(g.Where(s => s.WeightKg is not null).Select(s => s.WeightKg!.Value));
            return new PositionStats(g.Key, g.Count(), ageMean, ageSd, heightMean, heightSd, weightMean, weightSd);
        }).ToList();

        var known = samples.Where(s => s.Nationality is not null).ToList();
        var nationalities = known.GroupBy(s => s.Nationality!)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
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

public record PositionStats(Position Position, int Count, double AgeMean, double AgeSd, double HeightMean, double HeightSd, double WeightMean, double WeightSd);

public record NationalityShare(string Code, double Share);

// Um jogador real da amostra; só existe em memória dentro da ferramenta, nunca é gravado.
public record PlayerSample(Position Position, DateOnly BirthDate, double? HeightCm, double? WeightKg, string? Nationality);
