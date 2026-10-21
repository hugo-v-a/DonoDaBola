using System.Text.RegularExpressions;
using DonoDaBola.Core;
using Microsoft.Data.Sqlite;

namespace DonoDaBola.Data;

// Saves em <directory>/<slot>.db, copiados com a API de backup do SQLite (funciona com o banco aberto).
// A pasta vem da plataforma (Stride: VirtualFileSystem.ApplicationLocal + "/saves").
public sealed partial class SaveGameStore(GameDatabase db, string directory) : ISaveGameStore
{
    public IReadOnlyList<string> List() =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.db").Select(f => Path.GetFileNameWithoutExtension(f)).Order().ToList()
            : [];

    public void Save(string slot)
    {
        var path = PathFor(slot);
        Directory.CreateDirectory(directory);
        using var target = Connect(path, SqliteOpenMode.ReadWriteCreate);
        db.Connection.BackupDatabase(target);
    }

    public void Load(string slot)
    {
        var path = PathFor(slot);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Save '{slot}' não encontrado.", path);
        using (var source = Connect(path, SqliteOpenMode.ReadOnly))
            source.BackupDatabase(db.Connection);
        GameDatabase.Migrate(db.Connection); // save de versão antiga sobe para o esquema atual
    }

    public void Delete(string slot) => File.Delete(PathFor(slot));

    // O nome do slot vem do jogador: só letras, números, '-' e '_' (evita sair da pasta de saves).
    private string PathFor(string slot) =>
        SlotName().IsMatch(slot)
            ? Path.Combine(directory, slot + ".db")
            : throw new ArgumentException($"Nome de save inválido: '{slot}'. Use letras, números, '-' ou '_' (até 40).", nameof(slot));

    // Sem pool: o arquivo do slot é liberado logo após a cópia (senão o Delete falha no Windows).
    private static SqliteConnection Connect(string path, SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,40}$")]
    private static partial Regex SlotName();
}
