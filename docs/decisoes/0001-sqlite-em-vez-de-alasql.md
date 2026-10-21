# ADR 0001: SQLite em vez de AlaSQL

## Status
Aceito

## Data
2026-10-21

## Contexto
O professor pediu que os dados do jogo saiam do JSON e passem para um banco de dados, e sugeriu o AlaSQL. O jogo é C#/.NET 10 sobre a Stride 4.4 e precisa rodar no Windows e, no futuro, em Android e iOS. Os dados (20 clubes, ~600 jogadores, depois partidas e finanças) são relacionais, e o save precisa ser um arquivo local.

## Decisão
Usar **SQLite** via **Microsoft.Data.Sqlite 10.0.13**, no projeto `DonoDaBola.Data`. O Core define as interfaces (`IClubRepository`, `IPlayerRepository`, `ISaveGameStore`, `IGameClock`) e não referencia SQLite. O JSON passa a ser formato de seed, importação e exportação. O banco é a fonte de verdade em tempo de execução e também o save, que é uma cópia do banco feita com a API de backup.

## Alternativas consideradas

### AlaSQL
- Prós: sugerido pelo professor; SQL em memória.
- Contras: é uma biblioteca **só de JavaScript**, para navegador e Node.js, sem versão .NET.
- Rejeitado: não roda dentro de um processo .NET/Stride sem embutir um motor de JavaScript.

### Continuar só com JSON
- Prós: zero dependências.
- Contras: não atende ao pedido; filtros, paginação e saves parciais viram código manual.
- Rejeitado.

### LiteDB ou outro banco de documentos
- Contras: os dados são relacionais; o suporte mobile é menos documentado que o do SQLite.
- Rejeitado.

## Consequências
- O pacote padrão traz o `SQLitePCLRaw.bundle_e_sqlite3`, que tem nativos para Windows, Linux, macOS, Android e iOS. O mesmo código serve a todas as plataformas.
- Os testes do Data usam banco em memória (`Data Source=:memory:`), sem arquivos.
- As mudanças de esquema viram migrações numeradas (`PRAGMA user_version`). Um script publicado nunca é editado.
- Um teste de arquitetura impede o Core de referenciar SQLite.

## Fontes
- https://github.com/AlaSQL/alasql: "an open source SQL database for JavaScript".
- https://www.nuget.org/packages/Microsoft.Data.Sqlite: 10.0.13, mira netstandard2.0.
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions: o bundle `e_sqlite3` é o padrão.
- Metadados NuGet do `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13: grupos `net6.0-android31.0` e `net6.0-ios14.2`.
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/in-memory-databases
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup
