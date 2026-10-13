# Dono da Bola

Jogo de gerenciamento de futebol (estilo Brasfoot / Football Manager) baseado no Brasileirão Série A 2026.
O jogador é ao mesmo tempo dono/presidente e técnico do clube, com autonomia total.

Trabalho da disciplina Jogos e Entretenimento Digital – Bacharelado em Ciência da Computação, UNEMAT (Campus Barra do Bugres).

## Integrantes

- Hugo
- Mateus
- Renan

## Requisitos

- Windows 10/11
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Stride 4.4.0](https://www.stride3d.net/) (Game Studio, para editar cenas e assets)

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `DonoDaBola.Core` | Lógica do jogo em C# puro (sem Stride) |
| `DonoDaBola.Core.Tests` | Testes unitários do Core (xUnit) |
| `DonoDaBola.Game` | Projeto Stride: scripts, interface e cenas |
| `DonoDaBola.Windows` | Executável para Windows |
| `data` | Dados iniciais em JSON (clubes, jogadores, calendário) |
| `docs` | Game Design Document (`GDD.md`) e planos |

## Como compilar

```powershell
dotnet build DonoDaBola.slnx
```

Feche o Game Studio antes, se o build acusar arquivo travado.
O executável fica em `Bin/Windows/Debug/`.

## Como testar

```powershell
dotnet test DonoDaBola.Core.Tests
```

## Documentação

O design completo está em [docs/GDD.md](docs/GDD.md).
