namespace DonoDaBola.Core.Generation;

// Nomes fictícios: combinações sorteadas de nomes e sobrenomes brasileiros comuns (nunca nomes reais copiados).
public sealed class NameGenerator(Random random)
{
    private static readonly string[] FirstNames =
    [
        "Adriano", "Alan", "Alex", "André", "Anderson", "Arthur", "Bernardo", "Breno", "Bruno", "Caio",
        "Carlos", "César", "Cristiano", "Daniel", "Danilo", "Davi", "Diego", "Douglas", "Eduardo", "Emerson",
        "Enzo", "Everton", "Fábio", "Felipe", "Fernando", "Flávio", "Gabriel", "Gilberto", "Guilherme", "Gustavo",
        "Heitor", "Henrique", "Hugo", "Igor", "Ítalo", "Jefferson", "João", "Jonas", "Jorge", "José",
        "Júlio", "Kaio", "Kauã", "Leandro", "Leonardo", "Lucas", "Luciano", "Luiz", "Marcelo", "Marcos",
        "Mateus", "Matheus", "Maurício", "Miguel", "Murilo", "Nathan", "Nícolas", "Otávio", "Paulo", "Pedro",
        "Rafael", "Raul", "Renan", "Renato", "Ricardo", "Rodrigo", "Rogério", "Samuel", "Sérgio", "Thiago",
        "Tiago", "Vinícius", "Vitor", "Wallace", "Wellington", "Wesley", "Willian", "Yago",
    ];

    private static readonly string[] LastNames =
    [
        "Almeida", "Alves", "Amaral", "Andrade", "Araújo", "Azevedo", "Barbosa", "Barros", "Batista", "Borges",
        "Brito", "Campos", "Cardoso", "Carvalho", "Castro", "Cavalcanti", "Correia", "Costa", "Cruz", "Cunha",
        "Dias", "Duarte", "Farias", "Fernandes", "Ferreira", "Figueiredo", "Fonseca", "Freitas", "Garcia", "Gomes",
        "Gonçalves", "Guimarães", "Lima", "Lopes", "Macedo", "Machado", "Marques", "Martins", "Medeiros", "Melo",
        "Mendes", "Miranda", "Monteiro", "Moraes", "Moreira", "Moura", "Nascimento", "Neves", "Nogueira", "Nunes",
        "Oliveira", "Pacheco", "Peixoto", "Pereira", "Pinheiro", "Pinto", "Queiroz", "Ramos", "Reis", "Ribeiro",
        "Rocha", "Rodrigues", "Sales", "Sampaio", "Santana", "Santos", "Siqueira", "Silva", "Soares", "Souza",
        "Tavares", "Teixeira", "Toledo", "Vasconcelos", "Viana", "Vieira",
    ];

    private readonly HashSet<string> _used = [];

    // Próximo nome ainda não usado por este gerador.
    public string Next()
    {
        while (true)
        {
            var name = $"{FirstNames[random.Next(FirstNames.Length)]} {LastNames[random.Next(LastNames.Length)]}";
            if (_used.Add(name))
                return name;
        }
    }

    // O nome é uma combinação das listas acima (usado no teste que garante que o seed não tem nomes reais copiados).
    public static bool IsGenerated(string name) =>
        name.Split(' ') is [var first, var last] && FirstNames.Contains(first) && LastNames.Contains(last);
}
