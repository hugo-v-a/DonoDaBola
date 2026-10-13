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

## Arquitetura
- `DonoDaBola.Core`: lógica do jogo em C# puro. PROIBIDO referenciar a Stride (namespaces `Stride.*`).
  Contém modelos, simulação de partida, finanças, mercado, campeonato/calendário e IA dos clubes.
- `DonoDaBola.Core.Tests`: testes unitários (xUnit) do Core.
- Projetos da Stride: apenas interface, entrada do usuário, apresentação da partida e áudio. Eles chamam o Core.
- Dados iniciais (clubes, jogadores, calendário) em JSON. Clubes genéricos por padrão; dados reais carregáveis por arquivo editável.

## Regras de jogo fixas
- Série A: 20 clubes, pontos corridos, turno e returno, 38 rodadas, 4 rebaixados, pausa no meio do ano para a Copa do Mundo.
- Atributos dos jogadores na escala 1–20 (técnicos, mentais, físicos, goleiro), mais potencial, idade, moral, condição física e contrato.
- Partida simulada por eventos com probabilidades ponderadas pelos atributos e pela tática.
- Toda aleatoriedade passa por um gerador injetável com seed, para que testes e simulações sejam reprodutíveis.
- Valores monetários em `decimal`.
- Game over: falência do clube ou revolta da torcida que força a venda do clube.

## Convenções
- Código (classes, métodos, variáveis) em inglês; comentários, documentação e commits em português.
- Uma branch por funcionalidade (ex.: `feature/simulacao`) e pull request para a `main`.
- Testes do Core são obrigatórios, inclusive no modo minimalista do Ponytail.
- Nunca faça `git push` sem eu pedir.

## Comandos (Windows / PowerShell)
- Build: `dotnet build DonoDaBola.slnx`
- Testes: `dotnet test DonoDaBola.Core.Tests`
- Se o build acusar arquivo travado, peça para eu fechar o Game Studio.
