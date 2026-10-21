# ADR 0002: Portabilidade mobile como requisito de arquitetura

## Status
Aceito

## Data
2026-10-21

## Contexto
O professor pediu que o jogo seja pensado desde já para celular (Android e, se viável, iOS). A Stride 4.4 tirou Android e iOS do estado experimental, mas avisa que há pouca gente mantendo essas plataformas e que regressões podem passar. O iOS só compila em macOS, e o Game Studio só roda no Windows. Gerar o build mobile agora não é o objetivo; o objetivo é não fechar a porta.

## Decisão
Portabilidade é requisito de arquitetura a partir de agora:
1. **Toque primeiro na UI:** nada depende de hover ou clique direito; alvos de toque com cerca de 48 dp; listas longas paginadas ou virtualizadas; telas adaptáveis a resoluções, proporções e áreas seguras.
2. **Entrada abstrata:** a UI reage a ponteiro (`PointerEvents` e eventos `TouchDown`/`TouchUp` da UI da Stride, que cobrem toque e mouse). Teclado é só atalho opcional.
3. **Sem APIs do Windows nem caminhos fixos:** a pasta do banco e dos saves chega por parâmetro (Stride: `VirtualFileSystem.ApplicationLocal`).
4. **Desempenho e bateria:** a simulação roda por evento, nunca a cada frame; sem alocações desnecessárias em loops quentes; consultas paginadas.
5. **Core e Data compilam para `net10.0`**, sem TFM `-windows`.

O build de Android/iOS fica para depois.

## Alternativas consideradas
- **Gerar o APK agora:** exige workload Android, SDK, keystore e um projeto de plataforma do Mateus. É custo alto antes de existir tela jogável. Adiado.
- **Ignorar mobile até o fim:** a UI seria desenhada para mouse e teclado, e o retrabalho seria grande. Rejeitado.

## Consequências
- O Mateus segue as regras 1 e 2 na interface. Os revisores conferem pelo checklist do template de PR.
- Os testes de arquitetura garantem as regras 3 e 5 no Core e no Data.
- **Para gerar um APK:** `dotnet workload install android` com Android SDK/JDK; no Game Studio, **Update package → Update platforms → Android** (cria `DonoDaBola.Android`); keystore de assinatura; passar `ApplicationLocal` ao `GameDatabase.OpenInDirectory`.
- **Spike opcional sugerido antes do M3:** meio dia com a tela de Elenco num aparelho Android, para validar toque, rolagem e o SQLite nativo.

## Fontes
- https://doc.stride3d.net/latest/en/manual/platforms/android/index.html
- https://doc.stride3d.net/latest/en/manual/platforms/ios/index.html
- https://doc.stride3d.net/latest/en/manual/platforms/add-or-remove-a-platform.html
- https://doc.stride3d.net/latest/en/manual/input/pointers.html
- https://doc.stride3d.net/latest/en/api/Stride.UI.UIElement.html
- https://doc.stride3d.net/latest/en/api/Stride.Core.IO.VirtualFileSystem.html
