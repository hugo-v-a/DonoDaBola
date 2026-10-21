using DonoDaBola.Core;
using Microsoft.Data.Sqlite;

namespace DonoDaBola.Data;

// Esquema do banco e mapeamento Player <-> linha. Para mudar o esquema, acrescente um script em Migrations
// (nunca edite um já publicado): o índice + 1 vira o PRAGMA user_version.
internal static class Schema
{
    private const string Rating = "INTEGER NOT NULL CHECK ({0} BETWEEN 1 AND 20)";

    // Colunas de atributos, na ordem de leitura e escrita.
    private static readonly string[] AttributeColumns =
    [
        "passing", "dribbling", "finishing", "crossing", "tackling", "marking", "heading", "set_pieces",
        "vision", "positioning", "decisions", "leadership", "composure", "aggression",
        "pace", "stamina", "strength", "agility",
        "gk_reflexes", "gk_positioning", "gk_rushing_out", "gk_kicking",
    ];

    public static readonly string[] Migrations =
    [
        // v1: clubes e jogadores
        $"""
        CREATE TABLE club (
            id          TEXT PRIMARY KEY,
            name        TEXT NOT NULL,
            short_name  TEXT NOT NULL
        ) STRICT;

        CREATE TABLE player (
            id              TEXT PRIMARY KEY,
            club_id         TEXT REFERENCES club(id) ON DELETE SET NULL,
            name            TEXT NOT NULL,
            position        TEXT NOT NULL CHECK (position IN ('goalkeeper', 'defender', 'midfielder', 'forward')),
            birth_date      TEXT NOT NULL CHECK (birth_date GLOB '[0-9][0-9][0-9][0-9]-[0-1][0-9]-[0-3][0-9]'),
            height_cm       INTEGER CHECK (height_cm BETWEEN {Player.MinHeightCm} AND {Player.MaxHeightCm}),
            weight_kg       INTEGER CHECK (weight_kg BETWEEN {Player.MinWeightKg} AND {Player.MaxWeightKg}),
            preferred_foot  TEXT NOT NULL CHECK (preferred_foot IN ('right', 'left', 'both')),
            nationality     TEXT NOT NULL CHECK (length(nationality) = 2 AND nationality GLOB '[A-Z][A-Z]'),
            data_source     TEXT NOT NULL CHECK (data_source IN ('wikidata', 'generated')),
            potential       {string.Format(Rating, "potential")},
            {string.Join(",\n    ", AttributeColumns.Select(c => $"{c} {string.Format(Rating, c)}"))}
        ) STRICT;

        CREATE INDEX ix_player_club_position ON player(club_id, position);
        CREATE INDEX ix_player_position ON player(position);
        """,
    ];

    public static readonly string PlayerColumns =
        "id, club_id, name, position, birth_date, height_cm, weight_kg, preferred_foot, nationality, data_source, potential, "
        + string.Join(", ", AttributeColumns);

    // Ordem natural do elenco: goleiros, defensores, meio-campistas, atacantes.
    public const string PositionOrder =
        "CASE position WHEN 'goalkeeper' THEN 0 WHEN 'defender' THEN 1 WHEN 'midfielder' THEN 2 ELSE 3 END";

    public static readonly string InsertPlayer =
        $"INSERT INTO player ({PlayerColumns}) VALUES ({string.Join(", ", PlayerColumns.Split(", ").Select((_, i) => $"$p{i}"))})";

    public static string ToText<T>(T value) where T : struct, Enum => value.ToString().ToLowerInvariant();

    public static void BindPlayer(SqliteCommand cmd, Player p, string clubId)
    {
        object?[] values =
        [
            p.Id, clubId, p.Name, ToText(p.Position), p.BirthDate.ToString("yyyy-MM-dd"),
            p.HeightCm, p.WeightKg, ToText(p.PreferredFoot), p.Nationality, ToText(p.DataSource), p.Potential,
            p.Technical.Passing, p.Technical.Dribbling, p.Technical.Finishing, p.Technical.Crossing,
            p.Technical.Tackling, p.Technical.Marking, p.Technical.Heading, p.Technical.SetPieces,
            p.Mental.Vision, p.Mental.Positioning, p.Mental.Decisions, p.Mental.Leadership, p.Mental.Composure, p.Mental.Aggression,
            p.Physical.Pace, p.Physical.Stamina, p.Physical.Strength, p.Physical.Agility,
            p.Goalkeeping.Reflexes, p.Goalkeeping.Positioning, p.Goalkeeping.RushingOut, p.Goalkeeping.Kicking,
        ];
        for (var i = 0; i < values.Length; i++)
            cmd.Parameters.AddWithValue($"$p{i}", values[i] ?? DBNull.Value);
    }

    // Lê uma linha selecionada com PlayerColumns (mesma ordem de BindPlayer).
    public static Player ReadPlayer(SqliteDataReader r)
    {
        var i = 11; // primeiro atributo
        int Next() => r.GetInt32(i++);
        return new Player
        {
            Id = r.GetString(0),
            Name = r.GetString(2),
            Position = Enum.Parse<Position>(r.GetString(3), ignoreCase: true),
            BirthDate = DateOnly.ParseExact(r.GetString(4), "yyyy-MM-dd"),
            HeightCm = r.IsDBNull(5) ? null : r.GetInt32(5),
            WeightKg = r.IsDBNull(6) ? null : r.GetInt32(6),
            PreferredFoot = Enum.Parse<PreferredFoot>(r.GetString(7), ignoreCase: true),
            Nationality = r.GetString(8),
            DataSource = Enum.Parse<DataSource>(r.GetString(9), ignoreCase: true),
            Potential = r.GetInt32(10),
            Technical = new() { Passing = Next(), Dribbling = Next(), Finishing = Next(), Crossing = Next(), Tackling = Next(), Marking = Next(), Heading = Next(), SetPieces = Next() },
            Mental = new() { Vision = Next(), Positioning = Next(), Decisions = Next(), Leadership = Next(), Composure = Next(), Aggression = Next() },
            Physical = new() { Pace = Next(), Stamina = Next(), Strength = Next(), Agility = Next() },
            Goalkeeping = new() { Reflexes = Next(), Positioning = Next(), RushingOut = Next(), Kicking = Next() },
        };
    }
}
