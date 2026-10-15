# Progresso – Hugo (Simulação, Core)

## Onde estou

Marco **M1 – Uma partida**.

| Entrega | Situação |
|---|---|
| PR 1 – Modelos (`Player`, `Club`, atributos) | PR aberto: _link no PR_ |
| PR 2 – Escalação 4-4-2 e motor de eventos | em andamento (`feature/simulacao`) |

## Como usar

Tudo está no namespace `DonoDaBola.Core`.

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

## Decisões tomadas

- **Atributos 1–20 obrigatórios.** Todos os campos precisam existir; fora de 1–20 lança `ArgumentOutOfRangeException` com o nome do atributo (no código, no `with` e no JSON). O potencial usa a mesma escala.
- **Todo jogador tem os quatro grupos**, inclusive o de goleiro (jogador de linha recebe valores baixos).
- **Posições no código em inglês, no JSON em português:** `Goleiro`, `Defensor`, `Meio-campista`, `Atacante`.
- **Ids são strings** (sugestão de formato: `clu01`, `clu01-p07`).
- **Idade não é validada** por enquanto.
- **Aleatoriedade:** sem interface própria. Quem precisar de sorteio recebe um `System.Random` criado com seed (`new Random(42)`); mesma seed, mesma sequência. Vale também para o gerador de atributos do Renan.
- Moral, condição física, contrato e finanças **ainda não existem** nos modelos (M2+).

## Dependências

- **Renan pode usar agora:** `Player`, `Club`, `Position`, `Rating` e o formato JSON acima. No carregador, use `new JsonSerializerOptions { RespectNullableAnnotations = true }`, para que um `null` no arquivo (ex.: `"Name": null`) seja rejeitado.
- **Mateus pode usar agora:** os modelos para montar dados falsos na tela de Elenco. Os eventos da partida chegam no PR 2.
- **Preciso do Renan:** os 20 clubes genéricos em JSON nesse formato, para testar a simulação com dados de verdade.

## Próximos passos

1. PR 2: escalação automática 4-4-2 e motor de eventos da partida (`MatchResult` com placar e eventos estruturados).
2. M2: simular as rodadas dos outros clubes; cartões, lesões, moral e condição física.
