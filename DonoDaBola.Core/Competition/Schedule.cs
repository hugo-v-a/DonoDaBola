namespace DonoDaBola.Core.Competition;

// Um jogo do calendário. Index vai de 0 a 9 dentro da rodada.
public record Fixture(int Round, int Index, string HomeId, string AwayId);

// Calendário de pontos corridos, gerado (os clubes são fictícios, não usamos a tabela da CBF).
public static class Schedule
{
    // Turno pelo método do círculo, com o mando da fatoração de de Werra (poucas sequências de mando igual);
    // returno espelhado: a rodada n-1+r repete a rodada r com os mandos trocados (ADR 0004).
    public static IReadOnlyList<Fixture> Generate(IReadOnlyList<string> clubIds, Random random)
    {
        var ids = clubIds.Order(StringComparer.Ordinal).ToArray();
        if (ids.Length < 2 || ids.Length % 2 != 0 || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("O calendário precisa de um número par de clubes distintos.", nameof(clubIds));
        random.Shuffle(ids);

        var n = ids.Length;
        var m = n - 1;              // clubes que giram; ids[m] fica fixo no centro do círculo
        var firstHalf = new List<Fixture>();
        for (var r = 0; r < m; r++)
        {
            var index = 0;
            void Add(int home, int away) => firstHalf.Add(new(r + 1, index++, ids[home], ids[away]));

            if (r % 2 == 0) Add(m, r); else Add(r, m);
            for (var k = 1; k < n / 2; k++)
            {
                int a = (r + k) % m, b = (r - k + m) % m;
                if (k % 2 == 1) Add(a, b); else Add(b, a);
            }
        }

        return firstHalf.Concat(firstHalf.Select(f => new Fixture(f.Round + m, f.Index, f.AwayId, f.HomeId))).ToList();
    }
}
