# Sandbox de integração (fecha o Core no M1): plano

> **Para o Claude:** SUB-SKILL OBRIGATÓRIA: use executing-plans para implementar este plano tarefa por tarefa.

**Objetivo:** um console fora da Stride que percorre o caminho completo Data → Core (abrir o banco, importar o seed, buscar clubes, simular, narrar) e serve de modelo para o Mateus.

**Arquitetura:** `tools/DonoDaBola.Sandbox` referencia Core e Data. Nenhum projeto do jogo o referencia, então ele não entra no build mobile. A narração fica numa classe isolada do Sandbox. O teste de integração vive no `DonoDaBola.Data.Tests`, que já referencia Data e Core e já roda no CI.

**Branch:** `feature/sandbox-integracao` → `main`.

---

## Estado conferido (22/10)

- Os PRs #5, #6 e #7 estão na `main`.
- O CI já roda `Core.Tests`, `Data.Tests` e `SeedBuilder.Tests`.
- O `.gitignore` não ignora `.db`: só tem a regra do `Thumbs.db`.
- Grafo: `GameDatabase` implementa `IClubRepository`/`IPlayerRepository`; `SeedImporter.ImportDefault` → `Import` → `GameDatabase.Insert`; `MatchEngine.Simulate` usa `Lineup.Pick442` e devolve `MatchResult` com `MatchEvent`.

## Argumentos

| Argumento | Padrão | Efeito |
|---|---|---|
| `--db <pasta>` | `%TEMP%/DonoDaBola-sandbox` (fora do repositório) | Pasta do `game.db`. Se o banco estiver vazio, chama `SeedImporter.ImportDefault` |
| `--listar` | — | Lista os 20 clubes: id, nome, sigla e média de atributos (média de `Lineup.Rate` do elenco) |
| `--casa <id>` `--fora <id>` | `clu01`, `clu02` | Clubes da partida. Id inexistente ou repetido → mensagem de erro e código de saída 1 |
| `--seed <n>` | `42` | Seed da partida |

Sem `--listar`, o Sandbox simula e mostra:
1. as escalações 4-4-2, com posição, idade (`AgeOn(2026-01-01)`) e nota na posição;
2. a narração minuto a minuto;
3. o placar final;
4. um resumo por time: finalizações, gols, defesas do goleiro adversário e chutes para fora.

## Arquivos

| Arquivo | Conteúdo |
|---|---|
| `tools/DonoDaBola.Sandbox/DonoDaBola.Sandbox.csproj` | console `net10.0`; referencia Core e Data |
| `tools/DonoDaBola.Sandbox/Program.cs` | argumentos, abertura do banco, listagem, escalações, placar e resumo (tudo específico de console) |
| `tools/DonoDaBola.Sandbox/NarrationFormatter.cs` | `static string Describe(MatchEvent e, MatchResult result)`, que devolve uma frase em português por evento; é a parte para o Mateus copiar. O que é de console (prefixo `12'`, alinhamento) fica marcado em comentário |
| `DonoDaBola.Data.Tests/IntegrationTests.cs` | teste de ponta a ponta |
| `tools/DonoDaBola.Sandbox.Tests/NarrationFormatterTests.cs` | **(ajuste aprovado em 22/10)** um teste: para cada valor do enum `MatchEventType`, `Describe` devolve um texto não vazio. O `switch` lança `ArgumentOutOfRangeException` no caso padrão, então um tipo novo (cartões e lesões, no M2) sem narração quebra o teste |
| `.gitignore` | `*.db`, `*.db-journal`, `*.db-wal`, `*.db-shm` |
| `DonoDaBola.slnx` | Sandbox na pasta `/tools/` |
| `docs/progresso/hugo.md` | estado dos PRs, M4 sem "junto com o Renan", "Como rodar o Sandbox" e "Guia de integração na Stride (Mateus)" |

## Teste de integração (TDD)

`IntegrationTests.FullPath_SeedToMatch` (em `DonoDaBola.Data.Tests`):
1. Pasta temporária única. `GameDatabase.OpenInDirectory(pasta)` → `IsEmpty` → `SeedImporter.ImportDefault`.
2. `IClubRepository repo = db;` → `repo.Get("clu01")` e `repo.Get("clu02")` não são nulos.
3. `MatchEngine.Simulate(casa, fora, new Random(42))`, duas vezes:
   - placar = eventos `Goal` de cada lado;
   - minutos em ordem crescente;
   - as duas execuções são iguais (placar e eventos).
4. Reabrir o banco na mesma pasta não reimporta: `IsEmpty` é `false` e ainda há 20 clubes.
5. A pasta é apagada no `Dispose`.

Ele fica no `Data.Tests` porque esse projeto já referencia Data e Core e já roda no CI. Por isso o CI não precisa de mudança.

## CI

O workflow ganha `dotnet test tools/DonoDaBola.Sandbox.Tests`. O teste de integração entra no `Data.Tests`, que já roda. Nada compila os projetos da Stride.

## Fora do escopo

- O `IGameClock` real (M2): o Sandbox usa uma data fixa, 2026-01-01.
- Saves no Sandbox: o `SaveGameStore` já tem testes próprios.
