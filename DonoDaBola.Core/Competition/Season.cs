using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Competition;

// Um jogo da temporada; HomeGoals e AwayGoals ficam nulos até o jogo ser disputado.
public record SeasonMatch(int Round, int Index, DateOnly Date, Club Home, Club Away, int? HomeGoals, int? AwayGoals);

// Um gol da temporada (para a artilharia). ClubId é o clube por quem o jogador marcou.
public record SeasonGoal(int Round, int Index, int Minute, string ClubId, string PlayerId);

public record TopScorer(Player Player, Club Club, int Goals);

// Série A em memória: calendário, resultados, gols, cartões, sorteio de desempate e relógio do jogo.
// A interface lê pela ISeasonView; quem controla o jogo avança com AdvanceRound().
public sealed class Season : ISeasonView
{
    public const int ClubCount = 20;
    public const int MatchesPerRound = ClubCount / 2;

    private readonly Dictionary<string, Club> _clubs;
    private readonly SeasonMatch[] _matches; // ordenados por rodada e índice: o jogo i da rodada r fica em (r-1)*10+i
    private readonly List<SeasonGoal> _goals;
    private readonly List<SeasonCard> _cards;

    private Season(IReadOnlyList<Club> clubs, int seed, SeasonConfig config, IEnumerable<SeasonMatch> matches,
        IEnumerable<SeasonGoal> goals, IReadOnlyDictionary<string, int> drawKeys, IEnumerable<SeasonCard> cards, DisciplineRules discipline)
    {
        _clubs = clubs.ToDictionary(c => c.Id);
        if (_clubs.Count != ClubCount)
            throw new ArgumentException($"A Série A tem {ClubCount} clubes; recebidos {_clubs.Count}.", nameof(clubs));
        (Seed, Config) = (seed, config);
        _matches = matches.OrderBy(m => m.Round).ThenBy(m => m.Index).ToArray();
        _goals = goals.ToList();
        _cards = cards.ToList();
        Discipline = discipline;
        DrawKeys = new Dictionary<string, int>(drawKeys);

        // Restore recebe dados do save: tudo é conferido aqui, para nenhum erro aparecer só na hora de ler a tabela.
        if (_matches.Length != SeasonConfig.Rounds * MatchesPerRound
            || _matches.Where((m, i) => m.Round != i / MatchesPerRound + 1 || m.Index != i % MatchesPerRound).Any())
            throw new InvalidDataException("Temporada incompleta: o calendário precisa de 38 rodadas de 10 jogos, sem repetição.");
        if (_matches.Any(m => !_clubs.ContainsKey(m.Home.Id) || !_clubs.ContainsKey(m.Away.Id)
                || (m.HomeGoals is null) != (m.AwayGoals is null) || m.HomeGoals < 0 || m.AwayGoals < 0))
            throw new InvalidDataException("Temporada corrompida: jogo com clube desconhecido ou placar incompleto.");
        if (!DrawKeys.Keys.Order(StringComparer.Ordinal).SequenceEqual(_clubs.Keys.Order(StringComparer.Ordinal)) || DrawKeys.Values.Distinct().Count() != ClubCount)
            throw new InvalidDataException("Temporada corrompida: as chaves de sorteio precisam ser uma por clube, sem repetição.");
        if (_goals.Any(g => !_clubs.TryGetValue(g.ClubId, out var c) || c.Squad.All(p => p.Id != g.PlayerId)))
            throw new InvalidDataException("Temporada corrompida: gol de jogador fora do elenco do clube.");
        if (_cards.Any(c => !Competition.Discipline.IsCard(c.Type) || c.Minute is < 1 or > MatchEngine.Minutes
                || !_clubs.TryGetValue(c.ClubId, out var club) || club.Squad.All(p => p.Id != c.PlayerId)
                || c.Round < 1 || c.Round > SeasonConfig.Rounds || c.Index is < 0 or >= MatchesPerRound
                || _matches[(c.Round - 1) * MatchesPerRound + c.Index] is var m && (m.HomeGoals is null || (m.Home.Id != c.ClubId && m.Away.Id != c.ClubId))))
            throw new InvalidDataException("Temporada corrompida: cartão inválido, de jogador fora do elenco ou de jogo não disputado.");
        // A rodada atual é a primeira sem placar; rodada pela metade (ou fora de ordem) é dado corrompido.
        var played = _matches.Count(m => m.HomeGoals is not null);
        if (played % MatchesPerRound != 0 || _matches.Take(played).Any(m => m.HomeGoals is null))
            throw new InvalidDataException("Temporada corrompida: há rodada jogada pela metade.");
        CurrentRound = played / MatchesPerRound + 1;
    }

    public int Seed { get; }
    public SeasonConfig Config { get; }
    public IReadOnlyDictionary<string, int> DrawKeys { get; }
    public IReadOnlyList<SeasonMatch> Matches => _matches;
    public IReadOnlyList<SeasonGoal> Goals => _goals;
    public IReadOnlyList<SeasonCard> Cards => _cards;
    public DisciplineRules Discipline { get; }  // não é gravada: vale a do jogo que carrega o save

    public int CurrentRound { get; private set; }
    public bool IsFinished => CurrentRound > SeasonConfig.Rounds;

    // Data da próxima rodada; antes do início, a da 1ª; encerrada, fica na da 38ª.
    public DateOnly Today => Config.DateOf(Math.Min(CurrentRound, SeasonConfig.Rounds));

    public DateOnly DateOf(int round) => Config.DateOf(round);

    public IReadOnlyList<SeasonMatch> Round(int round)
    {
        DateOf(round); // valida 1..38
        return _matches.AsSpan((round - 1) * MatchesPerRound, MatchesPerRound).ToArray();
    }

    public IReadOnlyList<SeasonMatch> MatchesOf(string clubId) =>
        _clubs.ContainsKey(clubId)
            ? _matches.Where(m => m.Home.Id == clubId || m.Away.Id == clubId).ToList()
            : throw new ArgumentException($"Clube '{clubId}' não está na temporada.", nameof(clubId));

    public IReadOnlyList<StandingRow> Table() => Standings.Compute(_clubs.Values, _matches, DrawKeys, _cards, Discipline);

    // Suspensos do clube na rodada atual (os que não podem jogar a próxima rodada). Com a temporada encerrada,
    // nenhum: a suspensão não passa para a temporada seguinte (decisão até o M3, ADR 0005).
    public IReadOnlyList<Unavailable> UnavailableOf(string clubId)
    {
        if (!_clubs.TryGetValue(clubId, out var club))
            throw new ArgumentException($"Clube '{clubId}' não está na temporada.", nameof(clubId));
        if (IsFinished)
            return [];
        var back = Competition.Discipline.ReturnRounds(Competition.Discipline.Suspensions(_cards, Discipline), CurrentRound);
        return club.Squad.Where(p => back.ContainsKey(p.Id))
            .Select(p => new Unavailable(p, UnavailableReason.Suspended, back[p.Id]))
            .ToList();
    }

    // Por clube: menos vermelhos e depois menos amarelos primeiro (contagem do desempate); empate pelo id.
    public IReadOnlyList<DisciplineRow> DisciplineTable()
    {
        var suspensions = Competition.Discipline.Suspensions(_cards, Discipline);
        return _clubs.Values
            .Select(c => (Club: c, Cards: Competition.Discipline.TieBreakCards(_cards, c.Id, Discipline)))
            .Select(x => new DisciplineRow(x.Club, x.Cards.Yellows, x.Cards.Reds, suspensions.Count(s => s.ClubId == x.Club.Id)))
            .OrderBy(r => r.Reds).ThenBy(r => r.Yellows).ThenBy(r => r.Club.Id, StringComparer.Ordinal)
            .ToList();
    }

    // Mais gols primeiro; empate pelo id do jogador, para a ordem não variar.
    // ponytail: soma por (clube, jogador) e procura no elenco atual; com o mercado (M3), somar por jogador e
    // guardar quem saiu do elenco, senão um artilheiro vendido aparece duas vezes ou some.
    public IReadOnlyList<TopScorer> TopScorers(int limit = 20)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        return _goals.GroupBy(g => (g.ClubId, g.PlayerId))
            .Select(g => (Club: _clubs[g.Key.ClubId], g.Key.PlayerId, Goals: g.Count()))
            .Select(x => new TopScorer(x.Club.Squad.First(p => p.Id == x.PlayerId), x.Club, x.Goals))
            .OrderByDescending(s => s.Goals).ThenBy(s => s.Player.Id, StringComparer.Ordinal)
            .Take(limit)
            .ToList();
    }

    public static Season Create(IReadOnlyList<Club> clubs, int seed, SeasonConfig? config = null, DisciplineRules? discipline = null)
    {
        config ??= SeasonConfig.Default;
        var random = new Random(seed);
        var ids = clubs.Select(c => c.Id).Order(StringComparer.Ordinal).ToArray();

        // Sorteio de desempate feito uma vez e guardado: a tabela nunca consome um Random.
        var draw = ids.ToArray();
        random.Shuffle(draw);
        var drawKeys = draw.Select((id, key) => (id, key)).ToDictionary(x => x.id, x => x.key);

        var byId = clubs.ToDictionary(c => c.Id);
        var matches = Schedule.Generate(ids, random)
            .Select(f => new SeasonMatch(f.Round, f.Index, config.DateOf(f.Round), byId[f.HomeId], byId[f.AwayId], null, null));
        return new Season(clubs, seed, config, matches, [], drawKeys, [], discipline ?? DisciplineRules.Default);
    }

    // Remonta uma temporada salva (Data). Rodada pela metade lança InvalidDataException.
    // Sem cartões (save da v2), a temporada continua sem suspensões até os próximos cartões.
    public static Season Restore(IReadOnlyList<Club> clubs, int seed, SeasonConfig config, IEnumerable<SeasonMatch> matches,
        IEnumerable<SeasonGoal> goals, IReadOnlyDictionary<string, int> drawKeys, IEnumerable<SeasonCard>? cards = null, DisciplineRules? discipline = null) =>
        new(clubs, seed, config, matches, goals, drawKeys, cards ?? [], discipline ?? DisciplineRules.Default);

    // Seed de cada jogo: aritmética fixa (HashCode.Combine muda a cada execução), para a temporada ser
    // reprodutível e retomável. Mudar a fórmula muda os jogos de todo save; o teste MatchSeed_ReferenceValue avisa.
    public static int MatchSeed(int seasonSeed, int round, int index) => unchecked(seasonSeed * 1_000_003 + round * 1_009 + index);

    // Sequências separadas da seed do jogo (1 = cartões; 2 e 3 reservadas para lesões no M2): não mexem nos gols.
    public static int SubSeed(int matchSeed, int stream) => unchecked(matchSeed * 31 + stream * 7_919);

    // Simula os 10 jogos da rodada atual e devolve os resultados completos (com eventos, para a narração).
    // A temporada guarda o placar, os gols e os cartões. Tudo é calculado antes de gravar: ou entra a rodada inteira, ou nada.
    public IReadOnlyList<MatchResult> AdvanceRound()
    {
        if (IsFinished)
            throw new InvalidOperationException($"A temporada terminou: as {SeasonConfig.Rounds} rodadas já foram jogadas.");

        var first = (CurrentRound - 1) * MatchesPerRound;
        // Suspensos ficam fora da escalação desta rodada.
        var suspended = Competition.Discipline.ReturnRounds(Competition.Discipline.Suspensions(_cards, Discipline), CurrentRound).Keys.ToHashSet();
        var results = Enumerable.Range(first, MatchesPerRound)
            .Select(i =>
            {
                var m = _matches[i];
                var seed = MatchSeed(Seed, m.Round, m.Index);
                return MatchEngine.Simulate(m.Home, m.Away, new Random(seed), new MatchOptions
                {
                    HomeLineup = Lineup.Pick442(m.Home, suspended),
                    AwayLineup = Lineup.Pick442(m.Away, suspended),
                    Discipline = Discipline,
                    Cards = new Random(SubSeed(seed, 1)),
                });
            })
            .ToList();

        for (var i = 0; i < MatchesPerRound; i++)
        {
            var (m, r) = (_matches[first + i], results[i]);
            _matches[first + i] = m with { HomeGoals = r.HomeGoals, AwayGoals = r.AwayGoals };
            _goals.AddRange(r.Events.Where(e => e.Type == MatchEventType.Goal)
                .Select(e => new SeasonGoal(m.Round, m.Index, e.Minute, e.Club.Id, e.Player.Id)));
            _cards.AddRange(r.Events.Where(e => Competition.Discipline.IsCard(e.Type))
                .Select(e => new SeasonCard(m.Round, m.Index, e.Minute, e.Club.Id, e.Player.Id, e.Type)));
        }
        CurrentRound++;
        return results;
    }
}
