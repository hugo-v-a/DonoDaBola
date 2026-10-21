using System.Text.Json;

namespace DonoDaBola.Core;

// Formato do seed em JSON (data/seed.json): carga inicial do banco, importação e exportação.
public record SeedFile(IReadOnlyList<Club> Clubs)
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        NewLine = "\n", // mesmo arquivo em Windows e Linux (o seed é regenerado e comparado nos testes)
        RespectNullableAnnotations = true, // "Name": null no arquivo é rejeitado
        RespectRequiredConstructorParameters = true,
    };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static SeedFile FromJson(string json) =>
        JsonSerializer.Deserialize<SeedFile>(json, Options) ?? throw new JsonException("Seed vazio.");

    public static SeedFile FromJson(Stream json) =>
        JsonSerializer.Deserialize<SeedFile>(json, Options) ?? throw new JsonException("Seed vazio.");
}
