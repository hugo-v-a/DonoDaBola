using System.Globalization;
using DonoDaBola.Core;
using Microsoft.Data.Sqlite;

namespace DonoDaBola.Data;

// Esquema do banco e mapeamento Player <-> linha. Para mudar o esquema, acrescente um script em Migrations
// (nunca edite um já publicado): o índice + 1 vira o PRAGMA user_version.
// Cada script roda numa transação; PRAGMA foreign_keys não muda dentro dela (recriar tabela exige cuidado).
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

        // v2: temporada (só tabelas novas; clubes e jogadores ficam como estão). A rodada atual é a primeira sem
        // placar, e a data sai da configuração: o relógio não tem coluna própria. Cartões virão na v3.
        // Números literais (38 rodadas, 10 jogos, 90 minutos): um script publicado nunca muda com as constantes.
        """
        CREATE TABLE season (
            id                   INTEGER PRIMARY KEY CHECK (id = 1),
            seed                 INTEGER NOT NULL,
            start_date           TEXT NOT NULL,
            break_after_round    INTEGER NOT NULL CHECK (break_after_round BETWEEN 1 AND 37),
            resume_date          TEXT NOT NULL,
            days_between_rounds  INTEGER NOT NULL CHECK (days_between_rounds > 0)
        ) STRICT;

        CREATE TABLE season_club (
            club_id   TEXT PRIMARY KEY REFERENCES club(id),
            draw_key  INTEGER NOT NULL UNIQUE
        ) STRICT;

        CREATE TABLE season_match (
            round       INTEGER NOT NULL CHECK (round BETWEEN 1 AND 38),
            idx         INTEGER NOT NULL CHECK (idx BETWEEN 0 AND 9),
            home_id     TEXT NOT NULL REFERENCES club(id),
            away_id     TEXT NOT NULL REFERENCES club(id),
            home_goals  INTEGER CHECK (home_goals >= 0),
            away_goals  INTEGER CHECK (away_goals >= 0),
            PRIMARY KEY (round, idx),
            CHECK (home_id <> away_id),
            CHECK ((home_goals IS NULL) = (away_goals IS NULL))
        ) STRICT;

        CREATE TABLE season_goal (
            round      INTEGER NOT NULL,
            idx        INTEGER NOT NULL,
            minute     INTEGER NOT NULL CHECK (minute BETWEEN 1 AND 90),
            club_id    TEXT NOT NULL REFERENCES club(id),
            player_id  TEXT NOT NULL REFERENCES player(id),
            FOREIGN KEY (round, idx) REFERENCES season_match(round, idx)
        ) STRICT;
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
            p.Id, clubId, p.Name, ToText(p.Position), p.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
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
            BirthDate = DateOnly.ParseExact(r.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
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
