# Progresso – Hugo (Core e Data: simulação + dados e competição)

## Onde estou

Desde 21/10/2026 cuido também da frente **Dados e competição**, que era do Renan, com a autorização dele (marcos M1 a M4). Mudança de direção pedida pelo professor: os dados vão para um banco **SQLite**, e o jogo passa a ser pensado para **celular** (ADRs em `docs/decisoes/`).

Marco **M1 – Uma partida**: a parte do Core está fechada. Simulação, persistência e os 20 clubes genéricos já estão na `main`, e o Sandbox prova o caminho completo Data → Core fora da Stride. O M1 do grupo fecha quando a narração estiver na tela da Stride (Mateus; ver o guia abaixo).

| Entrega | Situação |
|---|---|
| PR 1 – Modelos (`Player`, `Club`, atributos) | na `main` ([#1](https://github.com/hugo-v-a/DonoDaBola/pull/1)) |
| PR 2 – Escalação 4-4-2 e motor de eventos | na `main` ([#2](https://github.com/hugo-v-a/DonoDaBola/pull/2)) |
| CI – testes do Core no GitHub Actions | na `main` ([#3](https://github.com/hugo-v-a/DonoDaBola/pull/3)) |
| PR A – Persistência SQLite, perfil do jogador, seed e saves | na `main` ([#5](https://github.com/hugo-v-a/DonoDaBola/pull/5)) |
| PR B – Gerador, SeedBuilder e 20 clubes genéricos | na `main` ([#6](https://github.com/hugo-v-a/DonoDaBola/pull/6)) |
| Grafo do código (graphify) no `.gitignore` e no CLAUDE.md | na `main` ([#7](https://github.com/hugo-v-a/DonoDaBola/pull/7)) |
| Sandbox de integração + teste ponta a ponta | PR aberto: _link no PR_ |

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
    SeedImporter.ImportDefault(db); // 20 clubes genéricos, embutidos no Data (PR B)

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

**Criar um jogador e um clube à mão (testes, dados falsos):**

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

foreach (var e in result.Events) // já em ordem de minuto, de 1 a 90
{
    var text = $"{e.Minute}' {NarrationFormatter.Describe(e, result)}"; // frases prontas no Sandbox
}
```

A narração pronta está em `tools/DonoDaBola.Sandbox/NarrationFormatter.cs`, que pode ser copiada para a interface. No Sandbox, ela lança `ArgumentOutOfRangeException` para um tipo de evento sem frase, e o teste do Sandbox avisa quando entrar um tipo novo (cartões e lesões, no M2). **Esse teste só protege a cópia do Sandbox:** na cópia da Stride, troque o `throw` do caso padrão por uma frase genérica (ex.: `$"Lance de {e.Club.ShortName}"`), para um tipo novo não derrubar a tela no meio da partida. Quando eu acrescentar um tipo de evento, aviso aqui em "Como usar".

Cada `MatchEvent` tem `Minute`, `Type` (`Goal`, `Save`, `Miss`), `Club` (quem finalizou), `Player` (quem finalizou) e `Goalkeeper` (goleiro adversário). Para saber se o lance é do mandante: `e.Club == result.Home`. As escalações estão em `result.HomeLineup` e `result.AwayLineup`.

## Como rodar o Sandbox

O `tools/DonoDaBola.Sandbox` é um console fora da Stride. Ele abre o banco, importa o seed se o banco estiver vazio, lista clubes ou simula uma partida com narração. Nenhum projeto do jogo o referencia, então ele não entra no build mobile.

```powershell
dotnet run --project tools/DonoDaBola.Sandbox -- --listar
dotnet run --project tools/DonoDaBola.Sandbox -- --casa clu01 --fora clu20 --seed 42
dotnet run --project tools/DonoDaBola.Sandbox -- --db C:\temp\ddb --casa clu05 --fora clu06
```

| Argumento | Padrão | O que faz |
|---|---|---|
| `--db <pasta>` | `%TEMP%\DonoDaBola-sandbox` | Pasta do `game.db`. O `.gitignore` ignora `*.db`, então nenhum banco vai para o Git |
| `--listar` | — | Os 20 clubes, com id, sigla, média de atributos e nome |
| `--casa`, `--fora` | `clu01`, `clu02` | Clubes da partida |
| `--seed` | `42` | Mesma seed, mesma partida |

A saída da partida traz as escalações 4-4-2 (com idade em 01/01/2026 e nota na posição da escalação), a narração minuto a minuto, o placar final e um resumo por time: finalizações, gols, finalizações paradas pelo goleiro adversário ("No goleiro") e para fora. Uma opção sem valor (ex.: `--db` sozinho) dá erro.

## Guia de integração na Stride (Mateus)

O mesmo caminho do Sandbox (`tools/DonoDaBola.Sandbox/Program.cs`), dentro do jogo:

1. **Referência:** no `DonoDaBola.Game.csproj`, acrescente `<ProjectReference Include="..\DonoDaBola.Data\DonoDaBola.Data.csproj" />`. O Data já traz o Core.
2. **Pasta gravável:** use `VirtualFileSystem.ApplicationLocal.GetAbsolutePath("")` como pasta do banco. Nunca use caminho fixo.
3. **Abrir o banco uma vez**, num `StartupScript`: abra no `Start()` e feche no `Cancel()`. Para as outras telas usarem o mesmo banco, registre-o com `Services.AddService` e leia com `Services.GetService`. Esses nomes conferem com a API da Stride, mas o script não foi testado.
   ```csharp
   public class GameDataScript : StartupScript
   {
       private GameDatabase db;

       public override void Start()
       {
           var folder = VirtualFileSystem.ApplicationLocal.GetAbsolutePath("");
           db = GameDatabase.OpenInDirectory(folder);
           if (db.IsEmpty) SeedImporter.ImportDefault(db);
           Services.AddService(db); // nas outras telas: Services.GetService<GameDatabase>()
       }

       public override void Cancel() => db?.Dispose();
   }
   ```
4. **Simular por evento**, ao tocar em "Jogar", nunca no `Update()` de cada frame:
   ```csharp
   var result = MatchEngine.Simulate(clubs.Get("clu01")!, clubs.Get("clu02")!, new Random(seed));
   ```
5. **Percorrer os eventos:** `result.Events` já vem em ordem de minuto. Para cada evento, use `NarrationFormatter.Describe(e, result)` e mostre o texto num `TextBlock`. Copie a classe do Sandbox com o caso padrão sem `throw`, como explicado em "Como usar". Para uma narração "ao vivo", revele os eventos aos poucos, por exemplo um a cada X segundos de jogo; os dados já estão todos prontos.
6. **Escalações e idade:** use `result.HomeLineup.Players` e `player.AgeOn(new DateOnly(2026, 1, 1))` até o `IGameClock` chegar (M2).

**Ainda não testado dentro da Stride:**
- **SQLite nativo:** se o `e_sqlite3` (a biblioteca nativa do SQLite) é copiado e carregado no build da Stride, no Windows e principalmente no Android.
- **Caminho do `VirtualFileSystem`:** qual pasta real o `ApplicationLocal.GetAbsolutePath("")` devolve em cada plataforma, e se ela é gravável.

Se algum dos dois falhar, me avise com a mensagem de erro; a correção fica do lado do Data.

## Decisões tomadas

- **Atributos 1–20 obrigatórios.** Todos os campos precisam existir; fora de 1–20 lança `ArgumentOutOfRangeException` com o nome do atributo (no código, no `with` e no JSON). O potencial usa a mesma escala.
- **Todo jogador tem os quatro grupos**, inclusive o de goleiro (jogador de linha recebe valores baixos).
- **Posições no código em inglês, no JSON em português:** `Goleiro`, `Defensor`, `Meio-campista`, `Atacante`.
- **Ids são strings** (sugestão de formato: `clu01`, `clu01-p07`).
- **Idade nunca é guardada:** só a data de nascimento; `AgeOn(data)` calcula a idade. Em ano não bissexto, quem nasceu em 29/02 faz aniversário em 28/02.
- **Banco SQLite é a fonte de verdade e o save** (ADR 0001). O seed em JSON tem o formato `{ "Clubs": [...], "Version": 1 }` e só é importado num banco vazio. A importação recusa versão errada, elenco sem 4-4-2 natural (1 GOL, 4 DEF, 4 MEI, 2 ATA) e campos nulos.
- **Portabilidade mobile** (ADR 0002): as regras valem para todos e estão no CLAUDE.md e no template de PR.
- **Dados dos jogadores** (ADR 0003): o Wikidata entra só como amostra estatística de altura, peso e nacionalidade (2.740 jogadores). Todos os jogadores do seed são gerados, com nomes fictícios. A idade do elenco (média 26) e o pé preferido (75% destros) são parâmetros do gerador.
- **20 clubes genéricos** (`data/seed.json`): nomes fictícios (ex.: Esporte Clube Serra Verde, `SVE`), ids `clu01`…`clu20` e jogadores `clu01-p01`… Os níveis vão de 15 (favoritos) a 8 (candidatos ao rebaixamento). Cada elenco tem 28 jogadores: 3 GOL, 9 DEF, 10 MEI e 6 ATA.
- **Regerar os dados** (só se mudar o gerador): `dotnet run --project tools/DonoDaBola.SeedBuilder -- seed`. Com `-- stats`, consulta o Wikidata de novo. Mesma seed, mesmo arquivo; um teste confere isso.
- **Aleatoriedade:** sem interface própria. Quem precisar de sorteio recebe um `System.Random` criado com seed (`new Random(42)`); mesma seed, mesma sequência. Vale também para o gerador de atributos.
- Moral, condição física, contrato e finanças **ainda não existem** nos modelos (M2+).
- **Partida em 90 intervalos de 1 minuto**, sem acréscimos. Só há eventos de finalização (gol, defesa, fora); intervalo (45') e fim (90') a interface deduz. Sem assistências por enquanto.
- **Escalação 4-4-2 fixa** com os melhores de cada posição; se faltar gente numa posição, entra o melhor restante nessa função. Elenco com menos de 11 lança `ArgumentException`.
- **CI:** todo PR roda no GitHub Actions (check `core-tests`) os testes de `DonoDaBola.Core.Tests`, `DonoDaBola.Data.Tests` (inclui o teste de integração ponta a ponta), `tools/DonoDaBola.SeedBuilder.Tests` e `tools/DonoDaBola.Sandbox.Tests`. Os projetos da Stride não são compilados no CI; compilem localmente antes de abrir o PR.
- **Calibração atual** (times de nota 10, 1.000 jogos): 2,46 gols e 11,3 finalizações por jogo; mandante vence 37%, empate 28%, visitante 35%. Nota 15 × nota 8: o forte vence 90%. As constantes ficam no topo do `MatchEngine`; ajuste fino no M4.

## Dependências

- **Renan:** a frente dele passou para mim; o carregador de JSON virou o `SeedImporter` (PR A).
- **Mateus pode usar agora:** o banco com os 20 clubes, os repositórios paginados, `MatchEngine.Simulate` com os eventos e a `NarrationFormatter` do Sandbox. O texto da narração fica na interface; o Core só entrega os dados. O passo a passo está no "Guia de integração na Stride", acima.

## Próximos passos

1. Revisão e merge do PR do Sandbox.
2. Apoiar o Mateus na integração com a Stride (guia acima).
3. Roteiro completo do Core (M2 a M4): `docs/plans/2026-10-21-persistencia-e-dados.md`, seção 12.
4. M2: simular as rodadas dos outros clubes; cartões, lesões, moral e condição física.
5. M4: calibrar com médias reais do Brasileirão (gols por jogo, porcentagem de vitórias do mandante).
