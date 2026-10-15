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

    // Clube com 18 jogadores de mesma nota: 2 goleiros, 6 defensores, 6 meio-campistas e 4 atacantes.
    public static Club CreateClub(string id, int rating = 10)
    {
        var squad = new List<Player>();
        foreach (var (position, count) in new[] { (Position.Goalkeeper, 2), (Position.Defender, 6), (Position.Midfielder, 6), (Position.Forward, 4) })
            for (var i = 0; i < count; i++)
                squad.Add(Create($"{id}-{position}-{i}", position, rating));
        return new Club { Id = id, Name = $"Clube {id}", ShortName = id.ToUpperInvariant(), Squad = squad };
    }
}
