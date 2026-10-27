using System.Globalization;
using DonoDaBola.Core;
using DonoDaBola.Core.Competition;
using DonoDaBola.Core.Simulation;
using Microsoft.Data.Sqlite;

namespace DonoDaBola.Data;

// Temporada no banco (esquema v2). Uma temporada por jogo; os saves copiam o banco inteiro.
public sealed partial class GameDatabase
{
    private const string DateFormat = "yyyy-MM-dd";

    // Na primeira vez grava a temporada (configuração, sorteio e os 380 jogos); depois, só os placares e gols
    // das rodadas ainda não gravadas. Tudo numa transação: se algo falhar, nada do que estava pendente entra
    // (a rodada atual é a primeira sem placar). Gravar de novo a mesma temporada não muda nada.
    public void SaveSeason(Season season)
    {
        using var tx = Connection.BeginTransaction();
        using (var read = Command("SELECT seed FROM season", tx))
        {
            var saved = read.ExecuteScalar();
            if (saved is null)
                InsertSeason(season, tx);
            else if (Convert.ToInt64(saved) != season.Seed)
                throw new InvalidOperationException($"O banco já tem outra temporada (seed {saved}); a recebida é da seed {season.Seed}.");
        }

        // Objeto atrasado em relação ao banco (ex.: guardado antes de um saves.Load): gravar perderia rodadas.
        int savedMatches;
        using (var read = Command("SELECT count(*) FROM season_match WHERE home_goals IS NOT NULL", tx))
            savedMatches = Convert.ToInt32(read.ExecuteScalar());
        if (savedMatches > season.Matches.Count(m => m.HomeGoals is not null))
            throw new InvalidOperationException("Temporada desatualizada: o banco já tem mais rodadas jogadas. Recarregue com LoadSeason().");

        using var update = Command("UPDATE season_match SET home_goals = $h, away_goals = $a WHERE round = $r AND idx = $i AND home_id = $home AND away_id = $away AND home_goals IS NULL", tx);
        using var insert = Command("INSERT INTO season_goal (round, idx, minute, club_id, player_id) VALUES ($r, $i, $m, $c, $p)", tx);
        using var insertCard = Command("INSERT INTO season_card (round, idx, minute, club_id, player_id, kind) VALUES ($r, $i, $m, $c, $p, $k)", tx);
        var goals = season.Goals.ToLookup(g => (g.Round, g.Index));
        var cards = season.Cards.ToLookup(c => (c.Round, c.Index));
        // Os jogos jogados formam rodadas inteiras em ordem (Season valida): os já gravados são os primeiros.
        foreach (var m in season.Matches.Skip(savedMatches).Where(m => m.HomeGoals is not null))
        {
            Bind(update, ("$h", m.HomeGoals), ("$a", m.AwayGoals), ("$r", m.Round), ("$i", m.Index), ("$home", m.Home.Id), ("$away", m.Away.Id));
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"O calendário salvo não confere com a temporada (rodada {m.Round}, jogo {m.Index}).");

            foreach (var g in goals[(m.Round, m.Index)])
            {
                Bind(insert, ("$r", g.Round), ("$i", g.Index), ("$m", g.Minute), ("$c", g.ClubId), ("$p", g.PlayerId));
                insert.ExecuteNonQuery();
            }
            foreach (var c in cards[(m.Round, m.Index)])
            {
                Bind(insertCard, ("$r", c.Round), ("$i", c.Index), ("$m", c.Minute), ("$c", c.ClubId), ("$p", c.PlayerId), ("$k", CardKind(c.Type)));
                insertCard.ExecuteNonQuery();
            }
        }
        tx.Commit();
    }

    // Banco corrompido (inclusive save editado à mão) vira sempre InvalidDataException.
    public Season? LoadSeason()
    {
        try
        {
            return ReadSeason();
        }
        catch (Exception ex) when (ex is KeyNotFoundException or FormatException or ArgumentException or OverflowException or InvalidCastException)
        {
            throw new InvalidDataException("Temporada corrompida no banco.", ex);
        }
    }

    private Season? ReadSeason()
    {
        int seed;
        SeasonConfig config;
        using (var cmd = Command("SELECT seed, start_date, break_after_round, resume_date, days_between_rounds FROM season"))
        using (var r = cmd.ExecuteReader())
        {
            if (!r.Read())
                return null;
            seed = r.GetInt32(0);
            config = new SeasonConfig(ParseDate(r.GetString(1)), r.GetInt32(2), ParseDate(r.GetString(3)), r.GetInt32(4));
        }

        var clubs = GetAll();
        var byId = clubs.ToDictionary(c => c.Id);
        var matches = new List<SeasonMatch>();
        using (var cmd = Command("SELECT round, idx, home_id, away_id, home_goals, away_goals FROM season_match ORDER BY round, idx"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                matches.Add(new(r.GetInt32(0), r.GetInt32(1), config.DateOf(r.GetInt32(0)), byId[r.GetString(2)], byId[r.GetString(3)],
                    r.IsDBNull(4) ? null : r.GetInt32(4), r.IsDBNull(5) ? null : r.GetInt32(5)));

        var goals = new List<SeasonGoal>();
        using (var cmd = Command("SELECT round, idx, minute, club_id, player_id FROM season_goal ORDER BY rowid"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                goals.Add(new(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3), r.GetString(4)));

        var cards = new List<SeasonCard>();
        using (var cmd = Command("SELECT round, idx, minute, club_id, player_id, kind FROM season_card ORDER BY rowid"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                cards.Add(new(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3), r.GetString(4), CardType(r.GetString(5))));

        var drawKeys = new Dictionary<string, int>();
        using (var cmd = Command("SELECT club_id, draw_key FROM season_club"))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                drawKeys[r.GetString(0)] = r.GetInt32(1);

        return Season.Restore(clubs, seed, config, matches, goals, drawKeys, cards); // valida e lança InvalidDataException se corrompido
    }

    private void InsertSeason(Season season, SqliteTransaction tx)
    {
        var c = season.Config;
        using (var cmd = Command("INSERT INTO season (id, seed, start_date, break_after_round, resume_date, days_between_rounds) VALUES (1, $s, $start, $break, $resume, $days)", tx))
        {
            Bind(cmd, ("$s", season.Seed), ("$start", FormatDate(c.Start)), ("$break", c.BreakAfterRound), ("$resume", FormatDate(c.ResumeOn)), ("$days", c.DaysBetweenRounds));
            cmd.ExecuteNonQuery();
        }
        foreach (var (clubId, key) in season.DrawKeys)
        {
            using var cmd = Command("INSERT INTO season_club (club_id, draw_key) VALUES ($c, $k)", tx);
            Bind(cmd, ("$c", clubId), ("$k", key));
            cmd.ExecuteNonQuery();
        }
        foreach (var m in season.Matches)
        {
            using var cmd = Command("INSERT INTO season_match (round, idx, home_id, away_id) VALUES ($r, $i, $h, $a)", tx);
            Bind(cmd, ("$r", m.Round), ("$i", m.Index), ("$h", m.Home.Id), ("$a", m.Away.Id));
            cmd.ExecuteNonQuery();
        }
    }

    // Cria o parâmetro na primeira vez e só troca o valor depois: o mesmo comando serve para todos os jogos.
    private static void Bind(SqliteCommand cmd, params (string Name, object? Value)[] values)
    {
        foreach (var (name, value) in values)
        {
            if (cmd.Parameters.Contains(name))
                cmd.Parameters[name].Value = value ?? DBNull.Value;
            else
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    private static string CardKind(MatchEventType type) => type switch
    {
        MatchEventType.YellowCard => "yellow",
        MatchEventType.SecondYellow => "second_yellow",
        MatchEventType.RedCard => "red",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Não é cartão."),
    };

    private static MatchEventType CardType(string kind) => kind switch
    {
        "yellow" => MatchEventType.YellowCard,
        "second_yellow" => MatchEventType.SecondYellow,
        "red" => MatchEventType.RedCard,
        _ => throw new FormatException($"Tipo de cartão desconhecido: '{kind}'."),
    };

    private static string FormatDate(DateOnly d) => d.ToString(DateFormat, CultureInfo.InvariantCulture);
    private static DateOnly ParseDate(string s) => DateOnly.ParseExact(s, DateFormat, CultureInfo.InvariantCulture);
}
