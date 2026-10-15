# Progresso – Hugo (Simulação, Core)

## Onde estou

Marco **M1 – Uma partida**.

| Entrega | Situação |
|---|---|
| PR 1 – Modelos (`Player`, `Club`, atributos) | PR aberto: [#1](https://github.com/hugo-v-a/DonoDaBola/pull/1) |
| PR 2 – Escalação 4-4-2 e motor de eventos | PR aberto: _link no PR_ (mesclar depois do #1) |

## Como usar

Modelos em `DonoDaBola.Core`; escalação e partida em `DonoDaBola.Core.Simulation`.

**Criar um jogador e um clube (Renan):**

```csharp
var player = new Player
{
    Id = "clu01-p01", Name = "Fulano", Position = Position.Midfielder, Age = 22, Potential = 16,
    Technical = new() { Passing = 14, Dribbling = 12, Finishing = 9, Crossing = 10,
                        Tackling = 8, Marking = 7, Heading = 6, SetPieces = 11 },
    Mental = new() { Vision = 15, Positioning = 10, Decisions = 13, Leadership = 5, Composure = 12, Aggression = 8 },
    Physical = new() { Pace = 13, Stamina = 14, Strength = 9, Agility = 12 },
    Goalkeeping = new() { Reflexes = 2, Positioning = 1, RushingOut = 1, Kicking = 3 },
};
var club = new Club { Id = "clu01", Name = "Clube Um", ShortName = "UM", Squad = [player] };
```

**Ler do JSON:** `JsonSerializer.Deserialize<Player>(json)` (ou `Club`) funciona direto. Exemplo de jogador em JSON:

```json
{
  "Id": "clu01-p01", "Name": "Fulano", "Position": "Meio-campista", "Age": 22, "Potential": 16,
  "Technical": { "Passing": 14, "Dribbling": 12, "Finishing": 9, "Crossing": 10,
                 "Tackling": 8, "Marking": 7, "Heading": 6, "SetPieces": 11 },
  "Mental": { "Vision": 15, "Positioning": 10, "Decisions": 13, "Leadership": 5, "Composure": 12, "Aggression": 8 },
  "Physical": { "Pace": 13, "Stamina": 14, "Strength": 9, "Agility": 12 },
  "Goalkeeping": { "Reflexes": 2, "Positioning": 1, "RushingOut": 1, "Kicking": 3 }
}
```

**Validar um valor avulso:** `Rating.Check(valor)` devolve o valor ou lança `ArgumentOutOfRangeException`. `Rating.Min` = 1, `Rating.Max` = 20.

**Escalação automática:** `Lineup.Pick442(club)` devolve `Goalkeeper`, `Defenders` (4), `Midfielders` (4), `Forwards` (2) e `Players` (os 11). `Lineup.Rate(player, position)` dá a nota do jogador naquela função.

**Simular uma partida e percorrer os eventos (Mateus):**

```csharp
using DonoDaBola.Core.Simulation;

var result = MatchEngine.Simulate(home, away, new Random(42)); // mesma seed = mesma partida

Console.WriteLine($"{result.Home.ShortName} {result.HomeGoals} x {result.AwayGoals} {result.Away.ShortName}");
foreach (var e in result.Events) // já em ordem de minuto, de 1 a 90
{
    var text = e.Type switch
    {
        MatchEventType.Goal => $"{e.Minute}' GOL do {e.Club.ShortName}! {e.Player.Name}",
        MatchEventType.Save => $"{e.Minute}' {e.Goalkeeper.Name} defende o chute de {e.Player.Name}",
        MatchEventType.Miss => $"{e.Minute}' {e.Player.Name} chuta para fora",
        _ => "",
    };
}
```

Cada `MatchEvent` tem `Minute`, `Type` (`Goal`, `Save`, `Miss`), `Club` (quem finalizou), `Player` (quem finalizou) e `Goalkeeper` (goleiro adversário). Para saber se o lance é do mandante: `e.Club == result.Home`. As escalações estão em `result.HomeLineup` e `result.AwayLineup`.

## Decisões tomadas

- **Atributos 1–20 obrigatórios.** Todos os campos precisam existir; fora de 1–20 lança `ArgumentOutOfRangeException` com o nome do atributo (no código, no `with` e no JSON). O potencial usa a mesma escala.
- **Todo jogador tem os quatro grupos**, inclusive o de goleiro (jogador de linha recebe valores baixos).
- **Posições no código em inglês, no JSON em português:** `Goleiro`, `Defensor`, `Meio-campista`, `Atacante`.
- **Ids são strings** (sugestão de formato: `clu01`, `clu01-p07`).
- **Idade não é validada** por enquanto.
- **Aleatoriedade:** sem interface própria. Quem precisar de sorteio recebe um `System.Random` criado com seed (`new Random(42)`); mesma seed, mesma sequência. Vale também para o gerador de atributos do Renan.
- Moral, condição física, contrato e finanças **ainda não existem** nos modelos (M2+).
- **Partida em 90 intervalos de 1 minuto**, sem acréscimos. Só há eventos de finalização (gol, defesa, fora); intervalo (45') e fim (90') a interface deduz. Sem assistências por enquanto.
- **Escalação 4-4-2 fixa** com os melhores de cada posição; se faltar gente numa posição, entra o melhor restante nessa função. Elenco com menos de 11 lança `ArgumentException`.
- **Calibração atual** (times de nota 10, 1.000 jogos): 2,46 gols e 11,3 finalizações por jogo; mandante vence 37%, empate 28%, visitante 35%. Nota 15 × nota 8: o forte vence 90%. As constantes ficam no topo do `MatchEngine`; ajuste fino no M4.

## Dependências

- **Renan pode usar agora:** `Player`, `Club`, `Position`, `Rating` e o formato JSON acima. No carregador, use `new JsonSerializerOptions { RespectNullableAnnotations = true }`, para que um `null` no arquivo (ex.: `"Name": null`) seja rejeitado.
- **Mateus pode usar agora:** os modelos para dados falsos na tela de Elenco e, com o PR 2, `MatchEngine.Simulate` e os eventos para a tela de narração. O texto da narração fica na interface; o Core só entrega os dados.
- **Preciso do Renan:** os 20 clubes genéricos em JSON nesse formato, para testar a simulação com dados de verdade.

## Próximos passos

1. Revisar e mesclar os PRs 1 e 2 com o grupo.
2. M2: simular as rodadas dos outros clubes; cartões, lesões, moral e condição física.
3. M4: calibrar com médias reais do Brasileirão (gols por jogo, porcentagem de vitórias do mandante), junto com o Renan.
