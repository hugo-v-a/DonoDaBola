namespace DonoDaBola.Core.Simulation;

public enum MatchEventType
{
    Goal, // gol
    Save, // finalização no alvo defendida pelo goleiro
    Miss, // finalização para fora
}

// Um lance da partida. Club e Player são de quem finalizou; Goalkeeper é o goleiro adversário.
public record MatchEvent(int Minute, MatchEventType Type, Club Club, Player Player, Player Goalkeeper);

public record MatchResult(
    Club Home,
    Club Away,
    Lineup HomeLineup,
    Lineup AwayLineup,
    int HomeGoals,
    int AwayGoals,
    IReadOnlyList<MatchEvent> Events);
