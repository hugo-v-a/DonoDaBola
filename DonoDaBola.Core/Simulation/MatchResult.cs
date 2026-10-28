namespace DonoDaBola.Core.Simulation;

public enum MatchEventType
{
    Goal,          // gol
    Save,          // finalização no alvo defendida pelo goleiro
    Miss,          // finalização para fora
    YellowCard,    // cartão amarelo
    SecondYellow,  // segundo amarelo do mesmo jogador: expulsão
    RedCard,       // vermelho direto: expulsão
}

// Um lance da partida. Nas finalizações, Club e Player são de quem finalizou e Goalkeeper é o goleiro adversário.
// Nos cartões, Club e Player são de quem recebeu o cartão, e Goalkeeper é nulo.
public record MatchEvent(int Minute, MatchEventType Type, Club Club, Player Player, Player? Goalkeeper)
{
    public bool IsShot => Type is MatchEventType.Goal or MatchEventType.Save or MatchEventType.Miss;
}

public record MatchResult(
    Club Home,
    Club Away,
    Lineup HomeLineup,
    Lineup AwayLineup,
    int HomeGoals,
    int AwayGoals,
    IReadOnlyList<MatchEvent> Events);
