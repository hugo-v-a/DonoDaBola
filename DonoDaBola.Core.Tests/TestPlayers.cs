namespace DonoDaBola.Core.Tests;

// Fábrica de jogadores para os testes: todos os atributos com o mesmo valor.
public static class TestPlayers
{
    public static Player Create(string id, Position position, int rating = 10) => new()
    {
        Id = id,
        Name = $"Jogador {id}",
        Position = position,
        Age = 25,
        Potential = rating,
        Technical = new()
        {
            Passing = rating, Dribbling = rating, Finishing = rating, Crossing = rating,
            Tackling = rating, Marking = rating, Heading = rating, SetPieces = rating,
        },
        Mental = new()
        {
            Vision = rating, Positioning = rating, Decisions = rating,
            Leadership = rating, Composure = rating, Aggression = rating,
        },
        Physical = new() { Pace = rating, Stamina = rating, Strength = rating, Agility = rating },
        Goalkeeping = new() { Reflexes = rating, Positioning = rating, RushingOut = rating, Kicking = rating },
    };
}
