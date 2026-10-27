namespace DonoDaBola.Core.Simulation;

// Regras de disciplina adotadas (ADR 0005): não confirmadas pela CBF, porque o Manual de Competições 2026 não foi
// encontrado; vêm de regulamentos de federações de 2026 e de casos reais da Série A.
public sealed record DisciplineRules
{
    public int YellowsForSuspension { get; init; } = 3;    // 3º amarelo da série suspende
    public int RedCardSuspension { get; init; } = 1;       // jogos por vermelho direto
    public int SecondYellowSuspension { get; init; } = 1;  // jogos por expulsão com 2º amarelo

    // Desempate (critérios 5 e 6): true = a expulsão por 2º amarelo conta 1 vermelho + 1 amarelo (o primeiro);
    // false = só o vermelho. Escolha nossa: o Art. 15 do REC não define.
    public bool SecondYellowCountsAsYellow { get; init; } = true;

    // Calibração (por time e por minuto, antes do peso de agressividade). Meta por jogo, somando os dois times:
    // 4,5–6,0 amarelos e 0,15–0,40 vermelho (Brasileirão: 5,4 e 0,25–0,28).
    public double YellowPerTeamMinute { get; init; } = 0.03;
    public double DirectRedPerTeamMinute { get; init; } = 0.0007;
    // Quem já tem amarelo se cuida: o peso dele no sorteio do amarelo é multiplicado por isto. Sem isso, o
    // 2º amarelo saía em 0,63 por jogo (real: cerca de 0,1).
    public double BookedCaution { get; init; } = 0.15;

    public static readonly DisciplineRules Default = new();
    public static readonly DisciplineRules Off = new() { YellowPerTeamMinute = 0, DirectRedPerTeamMinute = 0 };
}
