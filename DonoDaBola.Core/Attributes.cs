namespace DonoDaBola.Core;

// Atributos na escala 1–20 (GDD §6). Cada valor é validado ao ser atribuído, inclusive na leitura de JSON.

public record TechnicalAttributes
{
    public required int Passing { get; init => field = Rating.Check(value); }
    public required int Dribbling { get; init => field = Rating.Check(value); }
    public required int Finishing { get; init => field = Rating.Check(value); }
    public required int Crossing { get; init => field = Rating.Check(value); }
    public required int Tackling { get; init => field = Rating.Check(value); }
    public required int Marking { get; init => field = Rating.Check(value); }
    public required int Heading { get; init => field = Rating.Check(value); }
    public required int SetPieces { get; init => field = Rating.Check(value); }
}

public record MentalAttributes
{
    public required int Vision { get; init => field = Rating.Check(value); }
    public required int Positioning { get; init => field = Rating.Check(value); }
    public required int Decisions { get; init => field = Rating.Check(value); }
    public required int Leadership { get; init => field = Rating.Check(value); }
    public required int Composure { get; init => field = Rating.Check(value); }
    public required int Aggression { get; init => field = Rating.Check(value); }
}

public record PhysicalAttributes
{
    public required int Pace { get; init => field = Rating.Check(value); }
    public required int Stamina { get; init => field = Rating.Check(value); }
    public required int Strength { get; init => field = Rating.Check(value); }
    public required int Agility { get; init => field = Rating.Check(value); }
}

public record GoalkeepingAttributes
{
    public required int Reflexes { get; init => field = Rating.Check(value); }
    public required int Positioning { get; init => field = Rating.Check(value); }
    public required int RushingOut { get; init => field = Rating.Check(value); }
    public required int Kicking { get; init => field = Rating.Check(value); }
}
