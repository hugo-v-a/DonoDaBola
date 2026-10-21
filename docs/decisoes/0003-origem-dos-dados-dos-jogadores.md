# ADR 0003: Origem dos dados dos jogadores (Wikidata como amostra + gerador, nomes genéricos)

## Status
Aceito

## Data
2026-10-21

## Contexto
Queremos jogadores com idade, altura, peso, pé preferido e nacionalidade realistas, sem publicar dados de pessoas reais (GDD §10: nomes e escudos genéricos). O Wikidata é CC0 e tem boa cobertura de nascimento, posição e nacionalidade. Num teste exploratório de 21/10/2026 com Flamengo e Palmeiras, porém:
- **Sem data de fim:** os vínculos com clube quase nunca têm data de fim (P582). Filtrar por "sem fim" devolve 461 e 288 jogadores de toda a história.
- **Com início ≥ 2022:** sobram só 13 e 6 jogadores. Não dá para saber quem é o elenco 2026.
- **Cobertura no recorte histórico:** nascimento 98–99%, posição 95–97%, nacionalidade 97–98%, altura 33–36%, peso 17–18%, pé preferido 0%.

## Decisão
- O Wikidata é usado **só como amostra estatística**. A ferramenta `tools/DonoDaBola.SeedBuilder`, rodada à mão e fora do jogo, consulta jogadores que passaram pelos clubes da Série A 2026 e nasceram entre 1990 e 2008. Ela grava em `data/player-stats.json` só agregados por posição: idade, altura, peso, proporção de nacionalidades e tamanho da amostra.
- **Todos os jogadores do seed são gerados com seed** a partir dessas distribuições, com nomes brasileiros fictícios e `DataSource = Generated`. O pé preferido segue uma proporção fixa do gerador (cerca de 75% direito, 20% esquerdo e 5% ambos), porque o Wikidata não tem esse dado.
- O seed publicado (`data/seed.json`) e o `player-stats.json` **não contêm nomes reais nem IDs do Wikidata**. Um teste automatizado garante isso.
- O valor `DataSource = Wikidata` fica reservado para quem carregar dados reais por arquivo editável (GDD §10).
- O jogo nunca acessa a internet.

## Alternativas consideradas
- **Elenco real de cada clube como molde:** inviável, porque o elenco atual não é identificável pelo Wikidata. Também aumentaria o risco de identificar pessoas reais.
- **Distribuições escritas à mão, sem Wikidata:** mais simples, mas menos fundamentadas. Fica como plano B se a ferramenta quebrar (o serviço de consultas do Wikidata migra de backend em 2026–2027).

## Consequências
- O realismo vem das distribuições, não de indivíduos. A precisão de elencos reais não importa, porque os nomes são trocados.
- A ferramenta respeita a política de User-Agent da Wikimedia (nome, versão e contato) e os limites do serviço: consultas sequenciais e `Retry-After` em caso de 429.
- Regerar o seed é determinístico: mesma seed, mesmas estatísticas, mesmo `seed.json`.

## Fontes
- https://www.wikidata.org/wiki/Wikidata:Licensing: dados estruturados em CC0.
- https://www.mediawiki.org/wiki/Wikidata_Query_Service/User_Manual: endpoint, limite de 60 s, aviso de migração.
- https://foundation.wikimedia.org/wiki/Policy:Wikimedia_Foundation_User-Agent_Policy
- Propriedades: P569 (nascimento), P2048 (altura), P2067 (massa), P413 (posição), P54 (clube), P580/P582 (início e fim), P27 (cidadania), P8006 (pé preferido).
