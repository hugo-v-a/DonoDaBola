using System.Text.Json.Serialization;

namespace DonoDaBola.Core;

// No JSON a posição é escrita em português.
[JsonConverter(typeof(JsonStringEnumConverter<Position>))]
public enum Position
{
    [JsonStringEnumMemberName("Goleiro")] Goalkeeper,
    [JsonStringEnumMemberName("Defensor")] Defender,
    [JsonStringEnumMemberName("Meio-campista")] Midfielder,
    [JsonStringEnumMemberName("Atacante")] Forward,
}

public record Player
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Position Position { get; init; }
    public required int Age { get; init; }
    public required int Potential { get; init => field = Rating.Check(value); }
    public required TechnicalAttributes Technical { get; init; }
    public required MentalAttributes Mental { get; init; }
    public required PhysicalAttributes Physical { get; init; }
    public required GoalkeepingAttributes Goalkeeping { get; init; }
}
