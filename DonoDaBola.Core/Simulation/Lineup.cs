namespace DonoDaBola.Core.Simulation;

// Os 11 titulares num 4-4-2.
public record Lineup(
    Player Goalkeeper,
    IReadOnlyList<Player> Defenders,
    IReadOnlyList<Player> Midfielders,
    IReadOnlyList<Player> Forwards)
{
    public IEnumerable<Player> Players => [Goalkeeper, .. Defenders, .. Midfielders, .. Forwards];

    // Escolhe os melhores por posição; se faltar gente numa posição, completa com o melhor restante nessa função.
    public static Lineup Pick442(Club club)
    {
        if (club.Squad.Count < 11)
            throw new ArgumentException($"{club.Name} tem {club.Squad.Count} jogadores; são precisos 11.", nameof(club));

        var available = club.Squad.ToList();
        List<Player> Pick(Position position, int count)
        {
            var picked = available
                .OrderByDescending(p => p.Position == position)
                .ThenByDescending(p => Rate(p, position))
                .Take(count)
                .ToList();
            available.RemoveAll(picked.Contains);
            return picked;
        }

        return new(Pick(Position.Goalkeeper, 1)[0], Pick(Position.Defender, 4), Pick(Position.Midfielder, 4), Pick(Position.Forward, 2));
    }

    // Nota (1–20) do jogador atuando na posição: média dos atributos da função.
    public static double Rate(Player p, Position position) => position switch
    {
        Position.Goalkeeper => Avg(p.Goalkeeping.Reflexes, p.Goalkeeping.Positioning, p.Goalkeeping.RushingOut, p.Goalkeeping.Kicking),
        Position.Defender => Avg(p.Technical.Tackling, p.Technical.Marking, p.Technical.Heading, p.Mental.Positioning, p.Physical.Strength, p.Physical.Pace),
        Position.Midfielder => Avg(p.Technical.Passing, p.Mental.Vision, p.Mental.Decisions, p.Technical.Dribbling, p.Physical.Stamina),
        Position.Forward => Avg(p.Technical.Finishing, p.Mental.Composure, p.Technical.Dribbling, p.Physical.Pace, p.Technical.Heading),
        _ => throw new ArgumentOutOfRangeException(nameof(position)),
    };

    private static double Avg(params int[] values) => values.Average();
}
