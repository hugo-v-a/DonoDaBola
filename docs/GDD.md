# DONO DA BOLA

**Game Design Document · Esboço v0.1**

> Presidente e técnico. Autonomia total.
> *(título provisório)*

- **Disciplina:** Jogos e Entretenimento Digital
- **Curso:** Bacharelado em Ciência da Computação – UNEMAT, Campus Barra do Bugres
- **Engine:** Stride (C# / .NET)
- **Integrantes:** Hugo · Mateus · Renan
- **Data:** Outubro de 2026

> Convertido de `GDD_Dono_da_Bola_esboco.pdf` (mantido em `docs/`).

---

## 1. Visão geral

Dono da Bola é um jogo de gerenciamento esportivo no estilo de Football Manager e Brasfoot, ambientado no Campeonato Brasileiro Série A de 2026. O diferencial é o papel duplo: o jogador é ao mesmo tempo o dono/presidente do clube e o técnico do time. Não existe diretoria para pedir aprovação – cada contratação, cada real do orçamento e cada escalação são decisões dele, e as consequências também.

| Item | Definição |
|---|---|
| High concept | "Você manda em tudo: do caixa do clube à prancheta do vestiário." |
| Gênero | Simulação / gerenciamento esportivo (sports management) |
| Plataforma | PC (Windows), mouse e teclado |
| Modo | Single-player, carreira por temporadas |
| Público-alvo | Fãs de futebol brasileiro, 14+, que gostam de estratégia e números; jogadores de Brasfoot/Elifoot/FM |
| Referências | Football Manager (profundidade), Brasfoot (simplicidade e ritmo), Elifoot (nostalgia) |
| Engine | Stride 4.x (C#, .NET) |

### 1.1 Pilares de design

- **Autonomia total:** nenhuma decisão é bloqueada por uma diretoria fictícia; o limite é o dinheiro e a reação de torcida, elenco e mercado.
- **Duas cadeiras, um conflito:** o presidente quer caixa saudável, o técnico quer reforços. O jogador vive essa tensão o tempo todo.
- **Brasileirão de verdade:** calendário apertado, janelas de transferência, pausa para a Copa do Mundo, pressão da torcida e fair play financeiro.
- **Partida rápida e legível:** uma rodada deve caber em poucos minutos, sem perder a sensação de estar no banco de reservas.

## 2. Fantasia do jogador e papéis

O jogador escolhe um dos 20 clubes da Série A 2026 e assume o controle completo. As tarefas se dividem em dois "chapéus", que aparecem como áreas separadas na interface, mas compartilham os mesmos recursos.

| Presidente / Dono | Técnico |
|---|---|
| Orçamento anual e folha salarial | Escalação e formação tática |
| Contratações, vendas, empréstimos e renovações | Instruções de jogo (pressão, linha defensiva, estilo) |
| Patrocínios, bilheteria, preço de ingresso | Treinos semanais e foco de treino |
| Infraestrutura: estádio, CT, categoria de base | Gestão de moral, cansaço e lesões |
| Contratação de staff (auxiliares, preparador, olheiros) | Substituições e ajustes durante a partida |
| Relação com torcida e imprensa | Promoção de jovens da base ao profissional |

**Tensão central:** como presidente, gastar demais gera dívida e punição pelo fair play financeiro; como técnico, economizar demais gera elenco fraco, derrotas e perda de torcida (que reduz receita). O jogo recompensa o equilíbrio.

## 3. Loop principal (core loop)

O ciclo base é a semana de jogo. Cada semana tem uma ou duas partidas, dependendo do calendário.

| Etapa | O que o jogador faz | Chapéu |
|---|---|---|
| 1. Escritório | Lê notícias, propostas, e-mails de staff e relatórios de olheiros | Presidente |
| 2. Mercado | Negocia jogadores (se a janela estiver aberta), renova contratos | Presidente |
| 3. Treino | Define foco da semana (físico, tático, finalização...) e intensidade | Técnico |
| 4. Pré-jogo | Escolhe formação, titulares, reservas e instruções | Técnico |
| 5. Partida | Acompanha a simulação, faz substituições e muda a tática | Técnico |
| 6. Pós-jogo | Vê estatísticas, notas dos jogadores, receita da bilheteria e reação da torcida | Ambos |

**Loop longo (temporada):** pré-temporada → 38 rodadas da Série A (com pausa para a Copa do Mundo) → balanço financeiro anual → renovação de patrocínios e orçamento → nova temporada.

## 4. Mecânicas

### 4.1 Gestão financeira

- **Receitas:** direitos de TV (fixo + por posição), bilheteria (público × preço do ingresso), sócio-torcedor, patrocínios, premiações e vendas de jogadores.
- **Despesas:** salários, luvas, comissões de agentes, manutenção do estádio e do CT, base e staff.
- **Fair play financeiro:** o Brasileirão 2026 introduz regras de controle financeiro; no jogo, gastar acima de um limite proporcional à receita gera avisos, multas e, em casos graves, proibição de registrar novos atletas.
- **Saldo negativo prolongado:** leva a atraso de salários, queda de moral do elenco e, no limite, game over (falência).

### 4.2 Mercado de transferências

- Janelas de transferência abertas apenas em períodos definidos do calendário.
- Compra, venda, empréstimo (com ou sem opção de compra) e troca.
- Negociação em rodadas: valor, salário, duração do contrato, luvas e cláusula de rescisão.
- Olheiros revelam atributos aos poucos – quanto melhor o olheiro, mais precisa a informação.
- IA dos outros 19 clubes também compra e vende, com base em orçamento e carências do elenco.

### 4.3 Comando técnico

- Formações pré-definidas (4-4-2, 4-3-3, 3-5-2, 4-2-3-1...) com posicionamento ajustável.
- Instruções coletivas: mentalidade (defensiva a ofensiva), pressão, ritmo, largura e altura da linha.
- Treino semanal afeta atributos, condição física e risco de lesão.
- Moral individual e coletiva, influenciada por resultados, tempo de jogo e salários em dia.

### 4.4 Simulação da partida

A partida é calculada por um **motor de simulação baseado em eventos**: a cada intervalo de tempo de jogo, o motor compara a força dos setores (defesa, meio, ataque) e os atributos dos jogadores envolvidos, aplica as instruções táticas e sorteia o resultado do lance com probabilidades ponderadas (posse, chance, finalização, defesa do goleiro, falta, cartão, lesão). Isso garante que o melhor time vença mais vezes, mas não sempre.

- **Visualização (MVP):** narração em texto + placar + mini-campo 2D mostrando onde está a bola.
- **Visualização (futuro):** campo 3D simples em visão tática, aproveitando o renderizador da Stride.
- Velocidade ajustável e opção de "pular para o resultado".

### 4.5 Torcida, imprensa e elenco

- **Satisfação da torcida (0–100):** afeta público, sócios e receita. Sobe com vitórias, títulos e contratações de impacto; cai com derrotas, vendas de ídolos e ingressos caros.
- **Imprensa:** manchetes geradas a partir dos acontecimentos, que influenciam moral e torcida.
- **Vestiário:** jogadores insatisfeitos pedem para sair, rendem menos ou se recusam a renovar.

## 5. Competição: Brasileirão Série A 2026

O MVP reproduz apenas a Série A. Formato: 20 clubes, pontos corridos em turno e returno, 38 rodadas. Os 4 últimos são rebaixados e a parte de cima da tabela garante vagas na Libertadores (5 vagas em 2026) e na Sul-Americana. O calendário inclui a pausa para a Copa do Mundo, o que cria um período de pré-temporada no meio do ano.

**Clubes participantes**

| | | | |
|---|---|---|---|
| Athletico-PR | Atlético-MG | Bahia | Botafogo |
| Bragantino | Chapecoense | Corinthians | Coritiba |
| Cruzeiro | Flamengo | Fluminense | Grêmio |
| Internacional | Mirassol | Palmeiras | Remo |
| Santos | São Paulo | Vasco | Vitória |

*Promovidos da Série B 2025: Athletico-PR, Chapecoense, Coritiba e Remo. Expansões possíveis: Copa do Brasil, estaduais, Libertadores e Série B (para quem for rebaixado).*

## 6. Modelo de dados dos jogadores

| Grupo | Atributos (escala 1–20) |
|---|---|
| Técnicos | Passe, drible, finalização, cruzamento, desarme, marcação, cabeceio, cobrança de bola parada |
| Mentais | Visão de jogo, posicionamento, decisão, liderança, frieza, agressividade |
| Físicos | Velocidade, resistência, força, agilidade |
| Goleiro | Reflexo, posicionamento, saída do gol, jogo com os pés |
| Ocultos/estado | Potencial, propensão a lesão, condição física, moral, forma recente |
| Contrato | Salário, duração, cláusula de rescisão, valor de mercado |

Jogadores evoluem até cerca de 27–29 anos e declinam após os 31–32, com variações por posição e treino. A categoria de base gera jovens novos a cada temporada, com potencial ligado ao nível do investimento na base.

## 7. Objetivos, vitória e derrota

- **Objetivo por temporada:** definido pelo próprio jogador no início (ex.: evitar rebaixamento, vaga na Libertadores, título). Cumprir aumenta a satisfação da torcida e atrai patrocinadores.
- **Sem "fim" fixo:** a carreira continua por quantas temporadas o jogador quiser.
- **Game over:** falência do clube (dívida acima de um limite por várias temporadas) ou revolta da torcida que força a venda do clube.
- **Conquistas:** títulos, revelar craques da base, sanear as finanças, levar um clube pequeno à Libertadores.

## 8. Interface e telas principais

| Tela | Conteúdo |
|---|---|
| Menu inicial | Novo jogo, carregar, opções, créditos |
| Escolha do clube | Lista dos 20 clubes com orçamento, força do elenco e expectativa |
| Painel (hub) | Próximo jogo, notícias, caixa, satisfação da torcida, atalhos |
| Elenco | Tabela ordenável de jogadores com atributos, contrato e condição |
| Tática | Campo com formação arrastável e painel de instruções |
| Mercado | Busca de jogadores com filtros, propostas recebidas e enviadas |
| Finanças | Receitas × despesas por mês, folha salarial, limite do fair play |
| Clube | Estádio, CT, base, staff e patrocinadores |
| Partida | Placar, narração, mini-campo, estatísticas ao vivo, substituições |
| Classificação | Tabela da Série A, artilharia, calendário de rodadas |

### 8.1 Arte e som

- Estilo visual limpo, inspirado em painéis de dados esportivos; paleta verde-gramado com destaque nas cores do clube escolhido.
- Ícones e escudos estilizados (ver seção 10 sobre direitos).
- Som ambiente de torcida durante a partida, com reação a gols e lances perigosos; trilha leve nos menus.

## 9. Tecnologia: por que Stride

A Stride é uma engine open source (licença MIT), escrita em C# sobre .NET, com editor próprio (Game Studio), arquitetura baseada em entidades e componentes, e scripts em C#. Para um jogo de gerenciamento, isso traz uma vantagem clara: toda a lógica de simulação pode ser escrita como C# puro, testável fora da engine.

| Camada | Responsabilidade | Implementação prevista |
|---|---|---|
| Dados | Clubes, jogadores, calendário, saves | Banco SQLite (`DonoDaBola.Data`) como fonte de verdade e formato do save; JSON só como seed, importação e exportação |
| Núcleo (Core) | Simulação da partida, finanças, mercado, IA dos clubes, evolução | Biblioteca .NET separada, sem dependência da Stride; testes unitários |
| Apresentação | Telas, entrada do usuário, mini-campo, áudio | Stride: UI system, scripts (SyncScript/AsyncScript), cenas |

Essa separação também facilita a apresentação sobre a engine para a turma: o código de exemplo pode mostrar um script da Stride chamando o núcleo de simulação e atualizando a interface com o resultado de um lance.

**Atualização (out/2026):** os dados saíram do JSON e foram para o SQLite, a pedido do professor (ADR 0001). O jogo passa a ser pensado para celular (Android e, se viável, iOS) desde já: UI para toque, entrada por ponteiro, nada de APIs do Windows ou caminhos fixos, e Core e Data em `net10.0` (ADR 0002). Idade, altura e demais dados de perfil seguem distribuições derivadas do Wikidata, com nomes fictícios (ADR 0003).

> **Ponto de atenção:** a Stride é mais voltada a jogos 3D e o sistema de UI é menos maduro que o de outras engines; telas com muitas tabelas exigirão componentes de interface próprios. Vale validar isso cedo com um protótipo.

## 10. Riscos e questões legais

- **Direitos de nomes, escudos e jogadores:** clubes, marcas e atletas reais são protegidos (direito de imagem e marca). Para um trabalho acadêmico sem fins comerciais o risco é baixo, mas a abordagem mais segura é a do Brasfoot: o jogo usa nomes e escudos genéricos e carrega os dados reais por arquivos editáveis, criados pela comunidade ou pelo usuário.
- **Balanceamento da simulação:** resultados irreais quebram a imersão; será preciso calibrar com médias reais (gols por jogo, porcentagem de vitórias do mandante etc.).
- **Volume de dados:** 20 clubes × ~30 atletas = ~600 jogadores para cadastrar com atributos. Sugestão: gerar atributos a partir de poucos parâmetros (posição, nível, idade).
- **Escopo:** jogos de gerenciamento crescem rápido; manter o MVP enxuto.
- **Mobile na Stride:** Android e iOS saíram do estado experimental na 4.4, mas a própria documentação avisa que pouca gente os mantém e que regressões podem passar. O iOS só compila num Mac. Mitigação: regras de portabilidade desde já e um spike opcional num aparelho Android antes do M3.
- **Ferramenta do Wikidata:** o serviço de consultas migra de backend em 2026–2027 e pode mudar URLs. A ferramenta roda fora do jogo; se quebrar, as distribuições podem ser escritas à mão (ADR 0003).

## 11. Escopo: MVP × versões futuras

| MVP (entrega da disciplina) | Futuro |
|---|---|
| Série A 2026 completa (38 rodadas) | Copa do Brasil, estaduais, Libertadores, Série B |
| Simulação em texto + mini-campo 2D | Visualização 3D da partida |
| Elenco, tática, treino básico | Treino detalhado por jogador e staff especializado |
| Finanças simplificadas + fair play | Construção/reforma de estádio, sócio-torcedor detalhado |
| Mercado de compra e venda simples | Empréstimos, trocas, agentes, cláusulas complexas |
| Uma temporada jogável + save | Carreira multi-temporada, base, aposentadoria, editor de dados |
| Arquitetura portável (Core e Data em `net10.0`, UI para toque) | Build Android (APK) e iOS |

## 12. Questões em aberto para o grupo

- Nome definitivo do jogo.
- Nomes reais ou genéricos + arquivo de dados editável?
- A partida terá visual 2D, 3D ou só texto no MVP?
- Quanto da tensão presidente × técnico será sistêmica (regras) e quanto será narrativa (eventos, notícias)?
- Divisão de tarefas: núcleo de simulação, interface na Stride, dados e balanceamento.
- Haverá condição de "demissão"? (Como dono, o jogador não é demitido – mas pode perder o clube.)
