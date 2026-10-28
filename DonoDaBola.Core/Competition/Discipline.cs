using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Competition;

// Um cartão da temporada. Type é YellowCard, SecondYellow ou RedCard; ClubId e PlayerId são de quem recebeu.
public record SeasonCard(int Round, int Index, int Minute, string ClubId, string PlayerId, MatchEventType Type);

public enum SuspensionReason { Yellows, SecondYellow, RedCard }

// Suspensão automática: o jogador fica fora das rodadas FromRound .. FromRound + Rounds - 1.
public record Suspension(string PlayerId, string ClubId, int FromRound, int Rounds, SuspensionReason Reason);

public enum UnavailableReason { Suspended }

// ReturnRound: primeira rodada em que o jogador volta a poder jogar.
public record Unavailable(Player Player, UnavailableReason Reason, int ReturnRound);

public record DisciplineRow(Club Club, int Yellows, int Reds, int Suspensions);

// Disciplina derivada só do histórico de cartões (é o que o banco guarda): nada de estado paralelo.
public static class Discipline
{
    public static bool IsCard(MatchEventType type) => type is MatchEventType.YellowCard or MatchEventType.SecondYellow or MatchEventType.RedCard;

    // Cartões que contam nos critérios 5 e 6 do desempate (Art. 15). Vermelho direto e expulsão por 2º amarelo
    // valem 1 vermelho; cada YellowCard vale 1 amarelo. O 2º amarelo não gera YellowCard, então a expulsão por
    // 2º amarelo conta 1 vermelho + 1 amarelo (o primeiro); sem SecondYellowCountsAsYellow, só o vermelho.
    public static (int Reds, int Yellows) TieBreakCards(IEnumerable<SeasonCard> cards, string clubId, DisciplineRules rules)
    {
        int reds = 0, yellows = 0, secondYellows = 0;
        foreach (var c in cards.Where(c => c.ClubId == clubId))
        {
            if (c.Type == MatchEventType.YellowCard) yellows++;
            else if (c.Type == MatchEventType.SecondYellow) { reds++; secondYellows++; }
            else if (c.Type == MatchEventType.RedCard) reds++;
        }
        return (reds, rules.SecondYellowCountsAsYellow ? yellows : yellows - secondYellows);
    }

    // Suspensões automáticas, em ordem de rodada. Por jogo do jogador:
    // - expulsão por 2º amarelo: suspende; os dois amarelos desse jogo não contam para a série;
    // - amarelos que contam completam a série: no YellowsForSuspension-ésimo, suspende e a série recomeça;
    // - vermelho direto: suspende (um amarelo antes dele no mesmo jogo conta para a série).
    // Suspensões do mesmo jogo são cumpridas em sequência (3º amarelo + vermelho = 2 rodadas).
    public static IReadOnlyList<Suspension> Suspensions(IEnumerable<SeasonCard> cards, DisciplineRules rules)
    {
        var result = new List<Suspension>();
        foreach (var player in cards.Where(c => IsCard(c.Type)).GroupBy(c => (c.PlayerId, c.ClubId)))
        {
            var series = 0;
            foreach (var match in player.GroupBy(c => c.Round).OrderBy(g => g.Key))
            {
                var next = match.Key + 1;
                void Suspend(int rounds, SuspensionReason reason)
                {
                    result.Add(new(player.Key.PlayerId, player.Key.ClubId, next, rounds, reason));
                    next += rounds;
                }

                var sentOffBySecondYellow = match.Any(c => c.Type == MatchEventType.SecondYellow);
                if (!sentOffBySecondYellow)
                {
                    series += match.Count(c => c.Type == MatchEventType.YellowCard);
                    if (series >= rules.YellowsForSuspension)
                    {
                        series = 0;
                        Suspend(1, SuspensionReason.Yellows);
                    }
                }
                if (sentOffBySecondYellow) Suspend(rules.SecondYellowSuspension, SuspensionReason.SecondYellow);
                if (match.Any(c => c.Type == MatchEventType.RedCard)) Suspend(rules.RedCardSuspension, SuspensionReason.RedCard);
            }
        }
        return result.OrderBy(s => s.FromRound).ThenBy(s => s.PlayerId, StringComparer.Ordinal).ToList();
    }

    // Rodada de volta de quem está suspenso na rodada informada; nulo se pode jogar.
    public static int? ReturnRound(IEnumerable<SeasonCard> cards, DisciplineRules rules, string playerId, int round) =>
        ReturnRounds(Suspensions(cards, rules), round).GetValueOrDefault(playerId) is var r and > 0 ? r : (int?)null;

    // Suspensos na rodada (id → rodada de volta), já juntando suspensões em sequência.
    internal static Dictionary<string, int> ReturnRounds(IEnumerable<Suspension> suspensions, int round)
    {
        var result = new Dictionary<string, int>();
        foreach (var s in suspensions.OrderBy(s => s.FromRound))
        {
            var end = s.FromRound + s.Rounds;
            if (s.FromRound <= round && round < end)
                result[s.PlayerId] = end;
            else if (result.TryGetValue(s.PlayerId, out var back) && back == s.FromRound)
                result[s.PlayerId] = end; // suspensão seguinte, emendada
        }
        return result;
    }
}
