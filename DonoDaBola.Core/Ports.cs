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

// O save é o próprio banco: cada slot é uma cópia do jogo em andamento.
public interface ISaveGameStore
{
    IReadOnlyList<string> List();
    void Save(string slot);
    void Load(string slot);
    void Delete(string slot);
}
