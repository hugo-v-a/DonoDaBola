namespace DonoDaBola.Core;

public record Club
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ShortName { get; init; }
    public required IReadOnlyList<Player> Squad { get; init; }
}
