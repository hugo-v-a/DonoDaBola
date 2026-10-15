using System.Runtime.CompilerServices;

namespace DonoDaBola.Core;

// Escala dos atributos e do potencial dos jogadores (GDD §6).
public static class Rating
{
    public const int Min = 1;
    public const int Max = 20;

    // Devolve o valor se estiver em 1–20; senão lança, com o nome do atributo.
    public static int Check(int value, [CallerMemberName] string attribute = "") =>
        value is >= Min and <= Max
            ? value
            : throw new ArgumentOutOfRangeException(attribute, value, $"{attribute} deve estar entre {Min} e {Max}.");
}
