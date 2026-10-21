using DonoDaBola.Core;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Data;

// Carrega um seed JSON (data/seed.json ou exportado) num banco vazio.
public static class SeedImporter
{
    // Devolve o número de clubes importados. Seed inválido lança exceção e não grava nada.
    public static int Import(GameDatabase db, Stream json)
    {
        if (!db.IsEmpty)
            throw new InvalidOperationException("O seed só pode ser importado num banco vazio.");

        var seed = SeedFile.FromJson(json); // valida campos e limites dos modelos
        if (seed.Version != SeedFile.CurrentVersion)
            throw new InvalidDataException($"Versão de seed {seed.Version} não suportada (esperada {SeedFile.CurrentVersion}).");
        if (seed.Clubs.FirstOrDefault(c => !Lineup.HasNatural442(c)) is { } incomplete)
            throw new InvalidDataException($"O elenco de {incomplete.Id} não permite escalar um 4-4-2.");

        db.Insert(seed.Clubs); // id repetido viola a chave primária e desfaz a transação
        return seed.Clubs.Count;
    }
}
