# ADR 0004: Calendário gerado e critérios de desempate (regulamento CBF 2026)

## Status
Aceito

## Data
2026-10-22

## Contexto
A competição do M2 precisa de calendário, tabela e zonas da Série A. Os clubes são fictícios, então não dá para usar a tabela real da CBF. A fonte das regras é o Regulamento Específico da Competição (REC) da Série A 2026, versão de 15/12/2025, conferido em 22/10/2026:
- **Art. 3º e 14:** 20 clubes, pontos corridos, turno e returno (19 jogos de ida e 19 de volta).
- **Art. 15:** a ordem do desempate.
- **Art. 6º a 8º:** as vagas e o rebaixamento.

Três pontos não estão no REC:
- **Ordem do returno:** depende da tabela elaborada pela CBF.
- **Pausa da Copa:** o Art. 36 só diz que as datas seguem a FIFA e a CONMEBOL. As fontes de imprensa põem a pausa depois da 18ª rodada, com a volta em 20 ou 22/07.
- **Pontuação de 3, 1 e 0:** é regra geral das competições da CBF.

## Decisão
- **Calendário gerado** pelo método do círculo, com o mando da fatoração de de Werra. Os clubes são embaralhados com a seed da temporada: mesma seed, mesmo calendário.
- **Returno espelhado:** a rodada 19+r repete a rodada r com os mandos trocados. **É escolha nossa, não do regulamento.** Com isso, cada par se enfrenta uma vez em cada casa e cada clube faz 19 jogos em casa e 19 fora.
- **Sequências de mando:** no máximo 2 jogos seguidos com o mesmo mando dentro de cada turno, e 3 na virada do turno (o espelho repete o mando da 19ª na 20ª para parte dos clubes).
- **Datas por parâmetro** (`SeasonConfig`): 1ª rodada em 28/01/2026, pausa depois da 18ª, volta em 22/07/2026 e 7 dias entre rodadas. Assim, a 38ª cai em 02/12, que é o fim oficial. Os valores são parâmetros porque as fontes divergem.
- **Pontuação:** vitória 3, empate 1, derrota 0.
- **Desempate, na ordem do Art. 15:**
  1. vitórias;
  2. saldo;
  3. gols pró;
  4. confronto direto: pontos e depois saldo nos jogos entre os dois, só os já disputados. Com mais de 2 empatados, não vale (§2º). Conta-se quem continua empatado depois dos critérios 1 a 3: se 3 clubes empatam em pontos e um deles sai pelas vitórias, o confronto direto decide entre os 2 que restam;
  5. cartões vermelhos;
  6. cartões amarelos;
  7. sorteio.
- **Lista extensível:** os critérios ficam numa lista (`Standings.TieBreakers`). Os empatados são reavaliados critério a critério, e cada critério só vê quem continua empatado.
- **Cartões:** os critérios 5 e 6 entram quando a partida tiver cartões.
- **Sorteio estável:**
  - feito uma vez na criação da temporada: os ids ordenados são embaralhados com `Random(seed)`, e cada clube recebe uma chave de 0 a 19;
  - as chaves são guardadas na temporada e no save;
  - a tabela nunca consome um `Random`, então a ordem dos empatados não muda entre leituras nem depois de carregar.
- **Zonas fixas:**
  - 1º ao 4º: Libertadores, fase de grupos;
  - 5º: Libertadores, fase preliminar;
  - 6º ao 11º: Sul-Americana;
  - 17º ao 20º: rebaixamento.
  - Não há repasse de vagas por Copa do Brasil ou título continental, que o jogo não simula.
- **Seed de cada jogo:** `seed × 1.000.003 + rodada × 1.009 + índice`, em aritmética de 32 bits com estouro permitido. Um teste fixa o valor de referência.

## Consequências
- A temporada é reprodutível e retomável: o jogo N da rodada R tem sempre a mesma seed, e os elencos vêm do banco.
- A temporada guarda só o placar e os gols, que alimentam a artilharia. Os eventos completos ficam só na memória, no retorno de `AdvanceRound()`. Quando houver o clube do jogador (M3), os eventos dos jogos dele passam a ser gravados.
- **Cartões (próximo bloco):** exigirão a contagem de cartões por clube e por jogo no esquema (migração v3), para os critérios 5 e 6.
- A rodada é gravada inteira ou não é gravada, porque a rodada atual é a primeira sem placar. Uma rodada gravada pela metade é tratada como dado corrompido (`InvalidDataException`).
- **Se a CBF divulgar outra data:** basta trocar `SeasonConfig.Default`, sem mudar código.
