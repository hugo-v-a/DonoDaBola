namespace DonoDaBola.Core;

// Contratos que a interface (Stride) consome. Implementados por DonoDaBola.Data com SQLite.

// Data atual do jogo. Nunca use DateTime.Now na lógica: os testes e as temporadas dependem desta data.
public interface IGameClock
{
    DateOnly Today { get; }
}

public interface IClubRepository
{
    IReadOnlyList<Club> GetAll();
    Club? Get(string id);
}

// Filtro e página para listas de jogadores. Limit entre 1 e 200.
public record PlayerQuery(string? ClubId = null, Position? Position = null, int Offset = 0, int Limit = 50);

public interface IPlayerRepository
{
    IReadOnlyList<Player> Find(PlayerQuery query);
    int Count(PlayerQuery query);
}

// Leitura da temporada para as telas (Hub, Classificação, Calendário). Implementada por Competition.Season.
public interface ISeasonView : IGameClock
{
    int CurrentRound { get; }   // próxima rodada a jogar (1 a 38); 39 = encerrada
    bool IsFinished { get; }
    DateOnly DateOf(int round);
    IReadOnlyList<Competition.SeasonMatch> Round(int round);         // os 10 jogos, com placar se já jogados
    IReadOnlyList<Competition.SeasonMatch> MatchesOf(string clubId); // os 38 jogos do clube
    IReadOnlyList<Competition.StandingRow> Table();                  // ordenada, com desempate e zonas
    IReadOnlyList<Competition.TopScorer> TopScorers(int limit = 20);
    IReadOnlyList<Competition.Unavailable> UnavailableOf(string clubId); // suspensos para a rodada atual, com a rodada de volta
    IReadOnlyList<Competition.DisciplineRow> DisciplineTable();          // cartões e suspensões por clube
}

// Temporada guardada no banco (uma por jogo). SaveSeason grava só o que mudou, numa transação:
// uma rodada entra inteira ou não entra. Chame depois de cada AdvanceRound().
public interface ISeasonStore
{
    Competition.Season? LoadSeason(); // null se o jogo ainda não tem temporada
    void SaveSeason(Competition.Season season);
}

// O save é o próprio banco: cada slot é uma cópia do jogo em andamento.
public interface ISaveGameStore
{
    IReadOnlyList<string> List();
    void Save(string slot);
    void Load(string slot);
    void Delete(string slot);
}
