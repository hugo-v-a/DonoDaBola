namespace DonoDaBola.Core.Competition;

// Datas da temporada. São parâmetros porque as fontes divergem sobre a volta da pausa da Copa (ADR 0004).
public sealed record SeasonConfig
{
    public const int Rounds = 38;

    // Série A 2026: 1ª rodada em 28/01, pausa depois da 18ª, 19ª em 22/07; com 7 dias, a 38ª cai em 02/12.
    public static readonly SeasonConfig Default = new(new DateOnly(2026, 1, 28), 18, new DateOnly(2026, 7, 22));

    public SeasonConfig(DateOnly start, int breakAfterRound, DateOnly resumeOn, int daysBetweenRounds = 7)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(daysBetweenRounds, 1);
        if (breakAfterRound is < 1 or >= Rounds)
            throw new ArgumentOutOfRangeException(nameof(breakAfterRound), breakAfterRound, $"A pausa deve vir depois de uma rodada entre 1 e {Rounds - 1}.");
        (Start, BreakAfterRound, ResumeOn, DaysBetweenRounds) = (start, breakAfterRound, resumeOn, daysBetweenRounds);
        if (resumeOn <= DateOf(breakAfterRound))
            throw new ArgumentOutOfRangeException(nameof(resumeOn), resumeOn, $"A volta precisa ser depois da {breakAfterRound}ª rodada ({DateOf(breakAfterRound)}).");
    }

    public DateOnly Start { get; }
    public int BreakAfterRound { get; }
    public DateOnly ResumeOn { get; }
    public int DaysBetweenRounds { get; }

    public DateOnly DateOf(int round)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(round, Rounds);
        return round <= BreakAfterRound
            ? Start.AddDays(DaysBetweenRounds * (round - 1))
            : ResumeOn.AddDays(DaysBetweenRounds * (round - BreakAfterRound - 1));
    }
}
