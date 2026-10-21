# Dono da Bola

Jogo de gerenciamento de futebol (estilo Brasfoot / Football Manager) baseado no Brasileirão Série A 2026.
O jogador é DONO/PRESIDENTE e TÉCNICO do clube ao mesmo tempo, com autonomia total.
Trabalho da disciplina Jogos e Entretenimento Digital (Ciência da Computação – UNEMAT). Grupo: Hugo, Mateus e Renan.

Design completo em `docs/GDD.md`. Consulte quando a tarefa envolver regras do jogo; não precisa ler em toda tarefa.

## Engine
- Stride 4.4.0, C#. Use SEMPRE a API da Stride, NUNCA a da Unity.
- Scripts herdam de `SyncScript`, `AsyncScript` ou `StartupScript`.
- Equivalências para não confundir com Unity:
  - MonoBehaviour → SyncScript / AsyncScript / StartupScript
  - GameObject → Entity
  - GetComponent<T>() → Entity.Get<T>()
  - transform → Entity.Transform
  - Instantiate(prefab) → prefab.Instantiate() + adicionar as entidades à cena
  - Debug.Log → Log.Info / DebugText.Print
  - Input.GetKeyDown → Input.IsKeyPressed(Keys.X)
  - Canvas/UGUI → UIComponent + UIPage
- Na dúvida sobre uma API da Stride, confira na documentação oficial (doc.stride3d.net) ou no código-fonte da engine. Não suponha.
- Prefira que assets e cenas (.sdscene, .sdprefab, .sdui, .sdpkg) sejam editados pelo Game Studio. Se precisar editá-los à mão, avise antes para eu fechar o Game Studio.
- Skills: ignore recomendações de engine da skill game-development (pc-games compara Unity, Godot e Unreal). A engine é a Stride.

## Divisão do trabalho
- Hugo: todo o Core e o Data (simulação + dados e competição, frente que era do Renan, assumida com a autorização dele).
- Mateus: interface na Stride (ferramentas próprias de design). NÃO altere `DonoDaBola.Game`, `DonoDaBola.Windows`, nem `.sdscene`, `.sdui`, `.sdpkg`, `.sdprefab`.
- Cada um atualiza o próprio `docs/progresso/<nome>.md`; não edite o dos outros.
- Contratos que o Mateus consome (interfaces, modelos, eventos da partida) ficam estáveis e documentados em "Como usar" do `docs/progresso/hugo.md`. Mudança que quebra compatibilidade só com aviso nesse arquivo.

## Arquitetura
- `DonoDaBola.Core`: lógica do jogo em C# puro. PROIBIDO referenciar a Stride (`Stride.*`) e o SQLite (`Microsoft.Data.Sqlite`, `SQLitePCL`).
  Contém modelos, simulação, finanças, mercado, campeonato/calendário, IA dos clubes e as interfaces de dados (`IClubRepository`, `IPlayerRepository`, `ISaveGameStore`, `IGameClock`).
- `DonoDaBola.Data`: implementa as interfaces com SQLite (Microsoft.Data.Sqlite). Não referencia a Stride.
- `DonoDaBola.Core.Tests` e `DonoDaBola.Data.Tests`: testes xUnit (o Data usa SQLite em memória).
- `tools/DonoDaBola.SeedBuilder`: ferramenta manual, fora do jogo, que gera o seed. Nenhum projeto do jogo a referencia.
- Projetos da Stride: apenas interface, entrada do usuário, apresentação da partida e áudio. Eles chamam o Core e o Data.
- Dados: o banco SQLite é a fonte de verdade em tempo de execução e o formato do save. O JSON (`data/seed.json`) é só seed, importação e exportação. Clubes e nomes sempre genéricos; dados reais só por arquivo editável. Decisões em `docs/decisoes/`.
- A idade nunca é guardada: guarde a data de nascimento e calcule com `AgeOn(clock.Today)`. Nunca use `DateTime.Now` na lógica.

## Portabilidade mobile (requisito de arquitetura; ver ADR 0002)
1. UI para toque: nada depende de hover ou clique direito; alvos de toque com cerca de 48 dp; listas longas paginadas ou virtualizadas; telas adaptáveis a resoluções, proporções e áreas seguras.
2. Entrada por ponteiro (toque e mouse); teclado só como atalho opcional.
3. Nada de APIs exclusivas do Windows nem caminhos fixos; a pasta do banco e dos saves chega por parâmetro (Stride: `VirtualFileSystem.ApplicationLocal`).
4. A simulação roda por evento, nunca a cada frame; sem alocações desnecessárias em loops quentes; consultas ao banco paginadas.
5. Core e Data compilam para `net10.0`, nunca para TFM `-windows`.
- Revisão: conferir o checklist de `.github/pull_request_template.md`.

## Regras de jogo fixas
- Série A: 20 clubes, pontos corridos, turno e returno, 38 rodadas, 4 rebaixados, pausa no meio do ano para a Copa do Mundo.
- Atributos dos jogadores na escala 1–20 (técnicos, mentais, físicos, goleiro), mais potencial, data de nascimento, altura, peso, pé preferido, nacionalidade, moral, condição física e contrato.
- Partida simulada por eventos com probabilidades ponderadas pelos atributos e pela tática.
- Toda aleatoriedade passa por um gerador injetável com seed, para que testes e simulações sejam reprodutíveis.
- Valores monetários em `decimal`.
- Game over: falência do clube ou revolta da torcida que força a venda do clube.

## Convenções
- Código (classes, métodos, variáveis) em inglês; comentários, documentação e commits em português.
- Uma branch por funcionalidade (ex.: `feature/simulacao`) e pull request para a `main`.
- Testes do Core e do Data são obrigatórios, inclusive no modo minimalista do Ponytail.
- Nunca faça `git push` sem eu pedir.

## Comandos (Windows / PowerShell)
- Build: `dotnet build DonoDaBola.slnx`
- Testes: `dotnet test DonoDaBola.Core.Tests` e `dotnet test DonoDaBola.Data.Tests`
- Se o build acusar arquivo travado, peça para eu fechar o Game Studio.
