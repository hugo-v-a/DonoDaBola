using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Sandbox;

// Uma frase em português por evento da partida. Mateus: esta classe não depende de console;
// pode ser copiada como está para a tela de narração da Stride. O que é de console (prefixo "12'",
// alinhamento, linha de intervalo) fica no Program.cs.
public static class NarrationFormatter
{
    public static string Describe(MatchEvent e, MatchResult result) => e.Type switch
    {
        MatchEventType.Goal => $"GOL do {e.Club.ShortName}! {e.Player.Name} balança a rede. {ScoreAfter(e, result)}",
        MatchEventType.Save => $"{e.Goalkeeper?.Name} defende a finalização de {e.Player.Name} ({e.Club.ShortName}).",
        MatchEventType.Miss => $"{e.Player.Name} ({e.Club.ShortName}) finaliza para fora.",
        MatchEventType.YellowCard => $"Cartão amarelo para {e.Player.Name} ({e.Club.ShortName}).",
        MatchEventType.SecondYellow => $"Segundo amarelo para {e.Player.Name} ({e.Club.ShortName}): expulso!",
        MatchEventType.RedCard => $"Cartão vermelho direto para {e.Player.Name} ({e.Club.ShortName})!",
        // Tipo novo sem frase (ex.: cartões no M2) falha alto, e o teste NarrationFormatterTests avisa.
        _ => throw new ArgumentOutOfRangeException(nameof(e), e.Type, "Tipo de evento sem narração."),
    };

    // Placar logo depois do evento, ex.: "SVE 2 x 1 AVS".
    private static string ScoreAfter(MatchEvent e, MatchResult r)
    {
        var upTo = r.Events.TakeWhile(x => x != e).Append(e).Where(x => x.Type == MatchEventType.Goal).ToList();
        var home = upTo.Count(x => x.Club.Id == r.Home.Id);
        return $"{r.Home.ShortName} {home} x {upTo.Count - home} {r.Away.ShortName}";
    }
}
