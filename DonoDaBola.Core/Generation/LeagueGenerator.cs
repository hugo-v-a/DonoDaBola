namespace DonoDaBola.Core.Generation;

// Os 20 clubes genéricos da Série A, com nomes fictícios e níveis variados (favoritos e times fracos).
public static class LeagueGenerator
{
    // Nível médio de atributos de cada clube, na mesma ordem de Clubs.
    public static readonly int[] Levels = [15, 15, 14, 14, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 9, 9, 9, 8, 8];

    private static readonly (string Name, string ShortName)[] Clubs =
    [
        ("Esporte Clube Serra Verde", "SVE"), ("Atlético Vale do Sol", "AVS"), ("Sociedade Esportiva Porto Alto", "SPA"),
        ("Clube Náutico Maré Alta", "NMA"), ("União Esportiva Campo Dourado", "UCD"), ("Grêmio Recreativo Ventania", "GRV"),
        ("Associação Atlética Pedra Branca", "AAP"), ("Esporte Clube Lagoa Azul", "ELA"), ("Real Sociedade Monte Claro", "RMC"),
        ("Ferroviário Trilho de Ouro", "FTO"), ("Operário Mãos de Ferro", "OMF"), ("Independente Céu Aberto", "ICA"),
        ("América do Cerrado", "AMC"), ("Estrela Guia Futebol Clube", "EGF"), ("Comercial Vila Aurora", "CVA"),
        ("Juventude Areia Fina", "JAF"), ("Tropical Futebol Clube", "TRO"), ("Unidos da Colina", "UDC"),
        ("Vento Sul Futebol Clube", "VSF"), ("Mangueiral Atlético Clube", "MAC"),
    ];

    // Elenco de 28: sempre acima do mínimo do 4-4-2 (1 GOL, 4 DEF, 4 MEI, 2 ATA).
    private static readonly (Position Position, int Count)[] Composition =
        [(Position.Goalkeeper, 3), (Position.Defender, 9), (Position.Midfielder, 10), (Position.Forward, 6)];

    public static SeedFile Create(Random random, PlayerStats stats, DateOnly seasonStart)
    {
        var names = new NameGenerator(random);
        var clubs = Clubs.Select((c, i) =>
        {
            var id = $"clu{i + 1:00}";
            var number = 0;
            var squad = Composition
                .SelectMany(x => Enumerable.Repeat(x.Position, x.Count))
                .Select(position => PlayerGenerator.Create(random, stats, $"{id}-p{++number:00}", names.Next(), position, Levels[i], seasonStart))
                .ToList();
            return new Club { Id = id, Name = c.Name, ShortName = c.ShortName, Squad = squad };
        }).ToList();
        return new SeedFile(clubs);
    }
}
