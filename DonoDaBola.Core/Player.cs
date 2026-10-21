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

[JsonConverter(typeof(JsonStringEnumConverter<PreferredFoot>))]
public enum PreferredFoot
{
    [JsonStringEnumMemberName("direito")] Right,
    [JsonStringEnumMemberName("esquerdo")] Left,
    [JsonStringEnumMemberName("ambos")] Both,
}

// Origem dos dados de perfil: gerados com seed ou carregados de dados reais (ADR 0003).
[JsonConverter(typeof(JsonStringEnumConverter<DataSource>))]
public enum DataSource
{
    [JsonStringEnumMemberName("gerado")] Generated,
    [JsonStringEnumMemberName("wikidata")] Wikidata,
}

public record Player
{
    public const int MinHeightCm = 150, MaxHeightCm = 215, MinWeightKg = 50, MaxWeightKg = 120;

    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Position Position { get; init; }

    // A idade nunca é guardada: calcule com AgeOn(clock.Today).
    public required DateOnly BirthDate { get; init; }

    public int? HeightCm { get; init => field = InRange(value, MinHeightCm, MaxHeightCm); }
    public int? WeightKg { get; init => field = InRange(value, MinWeightKg, MaxWeightKg); }
    public PreferredFoot PreferredFoot { get; init; } = PreferredFoot.Right;

    // Código ISO 3166-1 alfa-2 do país (ex.: "BR").
    public string Nationality
    {
        get;
        init => field = value is [>= 'A' and <= 'Z', >= 'A' and <= 'Z']
            ? value
            : throw new ArgumentException($"Nationality deve ser um código ISO de 2 letras maiúsculas, não '{value}'.", nameof(Nationality));
    } = "BR";

    public DataSource DataSource { get; init; } = DataSource.Generated;

    public required int Potential { get; init => field = Rating.Check(value); }
    public required TechnicalAttributes Technical { get; init; }
    public required MentalAttributes Mental { get; init; }
    public required PhysicalAttributes Physical { get; init; }
    public required GoalkeepingAttributes Goalkeeping { get; init; }

    // Anos completos na data informada (normalmente a data do jogo, vinda do IGameClock).
    public int AgeOn(DateOnly date) => BirthDate.ToAge(date);

    private static int? InRange(int? value, int min, int max, [System.Runtime.CompilerServices.CallerMemberName] string name = "") =>
        value is null || (value >= min && value <= max)
            ? value
            : throw new ArgumentOutOfRangeException(name, value, $"{name} deve estar entre {min} e {max}.");
}

internal static class DateOnlyExtensions
{
    // Anos completos entre o nascimento e a data. Em ano não bissexto, quem nasceu em 29/02 completa em 28/02.
    public static int ToAge(this DateOnly birth, DateOnly date)
    {
        var age = date.Year - birth.Year;
        return date < birth.AddYears(age) ? age - 1 : age;
    }
}
