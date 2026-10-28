using DonoDaBola.Core;
using Microsoft.Data.Sqlite;

namespace DonoDaBola.Data;

// Banco do jogo em SQLite: fonte de verdade em tempo de execução e formato do save.
// Uma conexão aberta durante toda a partida (o jogo é single-thread).
public sealed partial class GameDatabase : IClubRepository, IPlayerRepository, ISeasonStore, IDisposable
{
    public const int SchemaVersion = 3;
    public const int MaxPageSize = 200;

    internal SqliteConnection Connection { get; }

    private GameDatabase(SqliteConnection connection) => Connection = connection;

    // Abre (ou cria) o banco e aplica as migrações pendentes. Ex.: "Data Source=:memory:" nos testes.
    public static GameDatabase Open(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        try
        {
            Migrate(connection);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
        return new GameDatabase(connection);
    }

    // No jogo, a pasta vem da plataforma (Stride: VirtualFileSystem.ApplicationLocal). Nunca use caminho fixo.
    public static GameDatabase OpenInDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        // Sem pool: ao fechar, o arquivo é liberado (no Windows, o pool mantém o game.db travado).
        return Open(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "game.db"), Pooling = false }.ToString());
    }

    public int UserVersion => Convert.ToInt32(Scalar("PRAGMA user_version"));
    public bool IsEmpty => Convert.ToInt64(Scalar("SELECT count(*) FROM club")) == 0;

    public IReadOnlyList<Club> GetAll() => ReadClubs(null);
    public Club? Get(string id) => ReadClubs(id).SingleOrDefault();

    public IReadOnlyList<Player> Find(PlayerQuery query)
    {
        if (query.Limit is < 1 or > MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(query), query.Limit, $"Limit deve estar entre 1 e {MaxPageSize}.");
        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query), query.Offset, "Offset não pode ser negativo.");

        using var cmd = Filtered($"SELECT {Schema.PlayerColumns} FROM player", query);
        cmd.CommandText += $" ORDER BY club_id, {Schema.PositionOrder}, id LIMIT $limit OFFSET $offset";
        cmd.Parameters.AddWithValue("$limit", query.Limit);
        cmd.Parameters.AddWithValue("$offset", query.Offset);
        return ReadPlayers(cmd).Select(x => x.Player).ToList();
    }

    public int Count(PlayerQuery query)
    {
        using var cmd = Filtered("SELECT count(*) FROM player", query);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // Exporta o banco no formato de seed (data/seed.json).
    // ponytail: jogadores sem clube (club_id NULL) ficam de fora; incluir quando houver agentes livres (M3).
    public SeedFile Export() => new(GetAll());

    // Grava clubes e elencos numa transação: ou entra tudo, ou nada.
    internal void Insert(IEnumerable<Club> clubs)
    {
        using var tx = Connection.BeginTransaction();
        foreach (var club in clubs)
        {
            using (var cmd = Command("INSERT INTO club (id, name, short_name) VALUES ($id, $name, $short)", tx))
            {
                cmd.Parameters.AddWithValue("$id", club.Id);
                cmd.Parameters.AddWithValue("$name", club.Name);
                cmd.Parameters.AddWithValue("$short", club.ShortName);
                cmd.ExecuteNonQuery();
            }
            foreach (var player in club.Squad)
            {
                using var cmd = Command(Schema.InsertPlayer, tx);
                Schema.BindPlayer(cmd, player, club.Id);
                cmd.ExecuteNonQuery();
            }
        }
        tx.Commit();
    }

    // Aplica os scripts de Schema.Migrations ainda não aplicados. Também roda depois de carregar um save antigo.
    internal static void Migrate(SqliteConnection connection)
    {
        using var read = new SqliteCommand("PRAGMA user_version", connection);
        var version = Convert.ToInt32(read.ExecuteScalar());
        if (version > Schema.Migrations.Length)
            throw new InvalidOperationException($"O banco está na versão {version}, mais nova que a suportada ({Schema.Migrations.Length}). Atualize o jogo.");

        for (var v = version; v < Schema.Migrations.Length; v++)
        {
            using var tx = connection.BeginTransaction();
            using var cmd = new SqliteCommand($"{Schema.Migrations[v]}\nPRAGMA user_version = {v + 1};", connection, tx);
            cmd.ExecuteNonQuery();
            tx.Commit();
        }
    }

    public void Dispose() => Connection.Dispose();

    private List<Club> ReadClubs(string? id)
    {
        var where = id is null ? "" : " WHERE id = $id";
        using var clubs = Command($"SELECT id, name, short_name FROM club{where} ORDER BY id");
        using var players = Command($"SELECT {Schema.PlayerColumns} FROM player WHERE club_id IS NOT NULL{(id is null ? "" : " AND club_id = $id")} ORDER BY {Schema.PositionOrder}, id");
        if (id is not null)
        {
            clubs.Parameters.AddWithValue("$id", id);
            players.Parameters.AddWithValue("$id", id);
        }

        var squads = ReadPlayers(players).ToLookup(x => x.ClubId, x => x.Player);
        var result = new List<Club>();
        using var reader = clubs.ExecuteReader();
        while (reader.Read())
        {
            var clubId = reader.GetString(0);
            result.Add(new Club { Id = clubId, Name = reader.GetString(1), ShortName = reader.GetString(2), Squad = squads[clubId].ToList() });
        }
        return result;
    }

    private static List<(string? ClubId, Player Player)> ReadPlayers(SqliteCommand cmd)
    {
        var list = new List<(string?, Player)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add((reader.IsDBNull(1) ? null : reader.GetString(1), Schema.ReadPlayer(reader)));
        return list;
    }

    private SqliteCommand Filtered(string select, PlayerQuery query)
    {
        var cmd = Command(select + " WHERE 1 = 1");
        if (query.ClubId is not null)
        {
            cmd.CommandText += " AND club_id = $club";
            cmd.Parameters.AddWithValue("$club", query.ClubId);
        }
        if (query.Position is { } position)
        {
            cmd.CommandText += " AND position = $position";
            cmd.Parameters.AddWithValue("$position", Schema.ToText(position));
        }
        return cmd;
    }

    private SqliteCommand Command(string sql, SqliteTransaction? tx = null) => new(sql, Connection, tx);

    private object? Scalar(string sql)
    {
        using var cmd = Command(sql);
        return cmd.ExecuteScalar();
    }
}
