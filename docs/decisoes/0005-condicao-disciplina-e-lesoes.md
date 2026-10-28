# ADR 0005: Estado de condição, disciplina e lesões

## Status
Aceito para a disciplina (PR E). A parte de lesões, condição e moral será completada no PR F.

## Data
2026-10-27

## Contexto
O M2 acrescenta cartões, suspensões, lesões, condição física e moral. O REC da Série A 2026 só cita os cartões nos critérios 5 e 6 do desempate (Art. 15) e remete o resto ao regulamento geral da CBF. Esse regulamento foi substituído pelo **Manual de Competições CBF 2026, que não encontramos** em 27/10/2026.

Por isso as regras abaixo são **adotadas, não confirmadas pela CBF**. Elas vêm de três fontes:
- regulamentos de federações de 2026: FGF, FMF e FPF-PE;
- o REC da Copa do Brasil 2026;
- casos reais da Série A 2026: o 3º amarelo suspende 1 jogo, e o 3º amarelo mais um vermelho direto no mesmo jogo afastam por 2 rodadas.

## Decisão
- **Regras de disciplina adotadas**, todas em `DisciplineRules` (configuráveis, sem constantes soltas):
  - o 3º amarelo da série suspende 1 jogo; os amarelos não zeram na temporada, e a série recomeça depois da suspensão;
  - o vermelho direto suspende 1 jogo;
  - a expulsão por 2º amarelo suspende 1 jogo, e esses dois amarelos não contam para a série;
  - suspensões do mesmo jogo são cumpridas em sequência.
- **Contagem no desempate (escolha nossa; o Art. 15 não define):**
  - a expulsão por 2º amarelo conta **1 vermelho e 1 amarelo** (o primeiro), por `SecondYellowCountsAsYellow = true`;
  - a regra fica num único método, `Discipline.TieBreakCards`, com teste próprio para os dois valores do flag;
  - só contam cartões de jogos já disputados.
- **Disciplina derivada do histórico:**
  - o banco guarda só os cartões (`season_card`, migração v3);
  - amarelos da série, suspensões e indisponíveis são recalculados a partir deles;
  - não há estado de disciplina gravado, então não há duas fontes da verdade.
- **Sequências aleatórias separadas:**
  - os cartões usam `new Random(Season.SubSeed(matchSeed, 1))`, e as lesões terão `SubSeed(…, 2)` e `SubSeed(…, 3)`;
  - a sequência dos gols não muda;
  - com a disciplina desligada (`DisciplineRules.Off`) e estado neutro, os placares e gols de referência da `main` 6676904 saem **idênticos**, o que é testado;
  - mesmo com cartões, um jogo sem vermelho mantém os gols.
- **Na partida:**
  - a cada minuto, cada time sorteia um amarelo e um vermelho direto;
  - quem recebe é sorteado com peso agressividade × função (zagueiro 1,5, meia 1,2, atacante 0,8, goleiro 0,2);
  - quem já tem amarelo tem peso × `BookedCaution` (0,15) no sorteio do amarelo;
  - a expulsão multiplica defesa, meio e ataque do time por 0,85 até o fim, e o expulso não finaliza mais;
  - o goleiro só recebe amarelo, e depois de advertido sai do sorteio, até existirem substituições (M3).
- **Calibração:** meta por jogo, **somando os dois times**: de 4,5 a 6,0 amarelos (Brasileirão de 2023: 5,40) e de 0,15 a 0,40 vermelho (0,25 a 0,28 em 2023 a 2026).
  - Medido em 20 seeds: cerca de 5,25 amarelos e 0,27 vermelho por jogo (0,14 por 2º amarelo e 0,13 direto).
  - Sem o `BookedCaution`, o 2º amarelo saía em 0,63 por jogo.
- **Lesões (PR F):**
  - a taxa e a duração são parâmetros (`FitnessRules`);
  - há lesões em jogo e em treino, porque a fonte (levantamento do ge) conta as duas;
  - a duração máxima de 6 rodadas **deixa as lesões longas de fora**;
  - a média de indisponíveis será conferida pela métrica do Sandbox.

## Consequências
- Uma temporada com tudo ligado tem outra tabela, pelos expulsos e pelos suspensos. Em 20 seeds com o `seed.json`:
  - o campeão foi o mesmo em 18 de 20;
  - a média de pontos do 1º passou de 85,5 para 86,1, e a do 17º de 33,2 para 33,3;
  - os gols por jogo passaram de 2,599 para 2,603.
- As regras **não são gravadas no banco**: um save usa as regras da versão do jogo que o carrega.
- **Saves da v2** carregam sem cartões, portanto sem suspensões.
- **Suspensão no fim da temporada:** a que sobra depois da rodada 38 **não passa para a temporada seguinte**, que não existe até o M3. São cerca de 19 por temporada. `UnavailableOf` devolve vazio com a temporada encerrada.
- **Se o Manual de Competições for encontrado:** as regras mudam por configuração, sem refatorar.
