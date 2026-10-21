# Progresso – Hugo (Core e Data: simulação + dados e competição)

## Onde estou

Desde 21/10/2026 cuido também da frente **Dados e competição**, que era do Renan, com a autorização dele (marcos M1 a M4). Mudança de direção pedida pelo professor: os dados vão para um banco **SQLite**, e o jogo passa a ser pensado para **celular** (ADRs em `docs/decisoes/`).

Marco **M1 – Uma partida**: simulação pronta na `main`. Faltam a persistência (PR A) e os 20 clubes genéricos (PR B); o marco fecha com a narração na Stride (Mateus).

| Entrega | Situação |
|---|---|
| PR 1 – Modelos (`Player`, `Club`, atributos) | na `main` ([#1](https://github.com/hugo-v-a/DonoDaBola/pull/1)) |
| PR 2 – Escalação 4-4-2 e motor de eventos | na `main` ([#2](https://github.com/hugo-v-a/DonoDaBola/pull/2)) |
| CI – testes do Core no GitHub Actions | na `main` ([#3](https://github.com/hugo-v-a/DonoDaBola/pull/3)) |
| PR A – Persistência SQLite, perfil do jogador, seed e saves | PR aberto: _link no PR_ |
| PR B – Gerador, SeedBuilder e 20 clubes genéricos | a seguir (`feature/dados-iniciais`) |

## Como usar

Modelos e interfaces em `DonoDaBola.Core`; escalação e partida em `DonoDaBola.Core.Simulation`; banco em `DonoDaBola.Data`.

### O que mudou no `Player` (PR A) – atenção, Mateus

| Membro | Mudança |
|---|---|
| `Age` | **removido.** A idade não é guardada: use `player.AgeOn(clock.Today)` |
| `BirthDate` (`DateOnly`) | **novo e obrigatório**: troque `Age = 25` por `BirthDate = new DateOnly(2001, 3, 10)` |
| `HeightCm`, `WeightKg` (`int?`) | novos, opcionais. Limites: 150–215 cm e 50–120 kg |
| `PreferredFoot` | novo: `Right` (padrão), `Left`, `Both`; no JSON `direito`, `esquerdo`, `ambos` |
| `Nationality` | novo: código ISO de 2 letras, padrão `"BR"` |
| `DataSource` | novo: `Generated` (padrão) ou `Wikidata`; no JSON `gerado`, `wikidata` |

O resto (atributos, `Position`, `Club`, `Lineup`, `MatchEngine`, eventos) não mudou.

### Acessar os dados pelo banco (Mateus)

O projeto `DonoDaBola.Game` precisa de `<ProjectReference Include="..\DonoDaBola.Data\DonoDaBola.Data.csproj" />`. Eu não mexo nos projetos da interface, então essa linha fica com você.

```csharp
using DonoDaBola.Core;
using DonoDaBola.Data;

// Pasta gravável da plataforma (Windows, Android, iOS). Nunca use caminho fixo.
// Não testado: GetAbsolutePath devolve o caminho no sistema da pasta "local" da plataforma; confirme no primeiro teste.
var folder = Stride.Core.IO.VirtualFileSystem.ApplicationLocal.GetAbsolutePath("");
using var db = GameDatabase.OpenInDirectory(folder);
if (db.IsEmpty)
    SeedImporter.Import(db, File.OpenRead("seed.json")); // no PR B o seed vem embutido; até lá, use um arquivo de teste

IClubRepository clubs = db;
IPlayerRepository players = db;

var all = clubs.GetAll();                                   // 20 clubes, com elenco
var page = players.Find(new PlayerQuery(ClubId: "clu01", Offset: 0, Limit: 30)); // elenco paginado
var total = players.Count(new PlayerQuery(Position: Position.Forward));

ISaveGameStore saves = new SaveGameStore(db, Path.Combine(folder, "saves"));
saves.Save("carreira1");  // nomes: letras, números, '-' e '_'
saves.Load("carreira1");
```

- `PlayerQuery.Limit` vai de 1 a 200; listas longas são sempre paginadas.
- A ordem é por clube e depois goleiros, defensores, meio-campistas e atacantes.
- Para mostrar a idade, use um `IGameClock`. Até o relógio do jogo chegar (M2), use uma data fixa da temporada, por exemplo `new DateOnly(2026, 1, 1)`.
- Exportar o banco para JSON: `db.Export().ToJson()`.

**Criar um jogador e um clube (Renan):**

```csharp
var player = new Player
{
    Id = "clu01-p01", Name = "Fulano", Position = Position.Midfielder, BirthDate = new DateOnly(2004, 3, 10), Potential = 16,
    HeightCm = 176, PreferredFoot = PreferredFoot.Left, // opcionais
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
  "Id": "clu01-p01", "Name": "Fulano", "Position": "Meio-campista", "BirthDate": "2004-03-10", "Potential": 16,
  "HeightCm": 176, "WeightKg": 70, "PreferredFoot": "esquerdo", "Nationality": "BR", "DataSource": "gerado",
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
- **Idade nunca é guardada:** só a data de nascimento; `AgeOn(data)` calcula a idade. Em ano não bissexto, quem nasceu em 29/02 faz aniversário em 28/02.
- **Banco SQLite é a fonte de verdade e o save** (ADR 0001). O seed em JSON tem o formato `{ "Clubs": [...], "Version": 1 }` e só é importado num banco vazio. A importação recusa versão errada, elenco sem 4-4-2 natural (1 GOL, 4 DEF, 4 MEI, 2 ATA) e campos nulos.
- **Portabilidade mobile** (ADR 0002): as regras valem para todos e estão no CLAUDE.md e no template de PR.
- **Dados dos jogadores** (ADR 0003): o Wikidata entra só como amostra estatística; todos os jogadores do seed são gerados, com nomes fictícios.
- **Aleatoriedade:** sem interface própria. Quem precisar de sorteio recebe um `System.Random` criado com seed (`new Random(42)`); mesma seed, mesma sequência. Vale também para o gerador de atributos do Renan.
- Moral, condição física, contrato e finanças **ainda não existem** nos modelos (M2+).
- **Partida em 90 intervalos de 1 minuto**, sem acréscimos. Só há eventos de finalização (gol, defesa, fora); intervalo (45') e fim (90') a interface deduz. Sem assistências por enquanto.
- **Escalação 4-4-2 fixa** com os melhores de cada posição; se faltar gente numa posição, entra o melhor restante nessa função. Elenco com menos de 11 lança `ArgumentException`.
- **CI:** todo PR roda `dotnet test DonoDaBola.Core.Tests` no GitHub Actions (check `core-tests`). Os projetos da Stride não são compilados no CI; compilem localmente antes de abrir o PR.
- **Calibração atual** (times de nota 10, 1.000 jogos): 2,46 gols e 11,3 finalizações por jogo; mandante vence 37%, empate 28%, visitante 35%. Nota 15 × nota 8: o forte vence 90%. As constantes ficam no topo do `MatchEngine`; ajuste fino no M4.

## Dependências

- **Renan:** a frente dele passou para mim; o carregador de JSON virou o `SeedImporter` (PR A).
- **Mateus pode usar agora:** os modelos para dados falsos na tela de Elenco e `MatchEngine.Simulate` com os eventos para a tela de narração. O texto da narração fica na interface; o Core só entrega os dados.

## Próximos passos

1. PR B: gerador de atributos e nomes, `tools/DonoDaBola.SeedBuilder` (Wikidata → estatísticas) e os 20 clubes genéricos em `data/seed.json`, embutido no Data.
2. Fechar o M1: exemplo de integração (`MatchEngine.Simulate` com clubes do banco) para o Mateus.
3. Roteiro completo do Core (M2 a M4): `docs/plans/2026-10-21-persistencia-e-dados.md`, seção 12.
4. M2: simular as rodadas dos outros clubes; cartões, lesões, moral e condição física.
5. M4: calibrar com médias reais do Brasileirão (gols por jogo, porcentagem de vitórias do mandante), junto com o Renan.
