## O que muda

<!-- Resumo para os colegas. Se mudar um contrato usado por outra frente (interfaces, modelos, eventos), diga o que mudou e atualize o "Como usar" do seu docs/progresso. -->

## Checklist

- [ ] `dotnet build DonoDaBola.slnx` e os testes (`DonoDaBola.Core.Tests`, `DonoDaBola.Data.Tests`) passam
- [ ] Core sem Stride e sem SQLite; Data sem Stride
- [ ] Portabilidade (ADR 0002):
  - [ ] UI por toque: nada de hover ou clique direito; alvos de toque com cerca de 48 dp; listas longas paginadas
  - [ ] Entrada por ponteiro; teclado só como atalho
  - [ ] Sem APIs do Windows, `Environment.GetFolderPath` ou caminhos fixos
  - [ ] Simulação fora do `Update()` de cada frame; consultas paginadas
  - [ ] Core e Data em `net10.0`, sem TFM `-windows`
- [ ] Atualizei o meu `docs/progresso/<nome>.md`
