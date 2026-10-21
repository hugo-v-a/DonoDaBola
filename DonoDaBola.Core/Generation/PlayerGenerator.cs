using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Generation;

// Gera um jogador com seed: perfil pelas distribuições da posição, atributos pelo nível do clube, idade e posição.
public static class PlayerGenerator
{
    // Pontos de calibração (M4).
    public const double RightFootShare = 0.75, LeftFootShare = 0.20; // o resto é ambidestro; o Wikidata não tem esse dado
    public const double AttributeNoise = 1.6;
    public const int MinAge = 17, MaxAge = 37;
    // ponytail: idade do elenco aproximada à mão (sem fonte); o Wikidata não dá a idade do elenco atual. Calibrar no M4.
    public const double SquadAgeMean = 26, SquadAgeSd = 4.5, GoalkeeperExtraAge = 1.5;

    public static Player Create(Random random, PlayerStats stats, string id, string name, Position position, int level, DateOnly seasonStart)
    {
        var s = stats.For(position);
        var ageMean = SquadAgeMean + (position == Position.Goalkeeper ? GoalkeeperExtraAge : 0);
        // Sorteia de novo fora da faixa (cortar acumularia jogadores com a mesma data de nascimento nos limites).
        double age;
        do age = Normal(random, ageMean, SquadAgeSd); while (age < MinAge || age >= MaxAge + 1);
        var form = level + AgeCurve(age);
        int Attr(double bias) => Math.Clamp((int)Math.Round(form + bias + Normal(random, 0, AttributeNoise)), Rating.Min, Rating.Max);
        var b = Bias.For(position);
        int Keeper(double bias) => position == Position.Goalkeeper ? Attr(bias) : random.Next(1, 5);

        var player = new Player
        {
            Id = id,
            Name = name,
            Position = position,
            BirthDate = seasonStart.AddDays(-(int)Math.Round(age * 365.25)),
            HeightCm = Clamp(Normal(random, s.HeightMean, s.HeightSd), Player.MinHeightCm, Player.MaxHeightCm),
            WeightKg = Clamp(Normal(random, s.WeightMean, s.WeightSd), Player.MinWeightKg, Player.MaxWeightKg),
            PreferredFoot = random.NextDouble() switch
            {
                < RightFootShare => PreferredFoot.Right,
                < RightFootShare + LeftFootShare => PreferredFoot.Left,
                _ => PreferredFoot.Both,
            },
            Nationality = PickNationality(random, stats.Nationalities),
            DataSource = DataSource.Generated,
            Potential = Rating.Min, // definido abaixo, a partir dos atributos gerados
            Technical = new()
            {
                Passing = Attr(b.Passing), Dribbling = Attr(b.Dribbling), Finishing = Attr(b.Finishing), Crossing = Attr(b.Crossing),
                Tackling = Attr(b.Tackling), Marking = Attr(b.Marking), Heading = Attr(b.Heading), SetPieces = Attr(b.Other),
            },
            Mental = new()
            {
                Vision = Attr(b.Vision), Positioning = Attr(b.Positioning), Decisions = Attr(b.Decisions),
                Leadership = Attr(b.Other), Composure = Attr(b.Composure), Aggression = Attr(b.Other),
            },
            Physical = new() { Pace = Attr(b.Pace), Stamina = Attr(b.Stamina), Strength = Attr(b.Strength), Agility = Attr(b.Other) },
            Goalkeeping = new() { Reflexes = Keeper(2), Positioning = Keeper(2), RushingOut = Keeper(1), Kicking = Keeper(0) },
        };

        // Potencial nunca abaixo do nível atual na posição; jovens têm margem para crescer.
        var current = Lineup.Rate(player, position);
        var growth = Math.Max(0, 24 - age) * 0.6 + Math.Abs(Normal(random, 0.5, 1));
        return player with { Potential = Math.Clamp((int)Math.Ceiling(current + growth), Rating.Min, Rating.Max) };
    }

    // Evolução até 27–29 anos e declínio a partir de 31–32 (GDD §6).
    private static double AgeCurve(double age) => age switch
    {
        < 21 => -2.5 + (age - MinAge) * 0.5,
        < 27 => -0.5 + (age - 21) * 0.25,
        < 30 => 1,
        < 32 => 0.5,
        _ => 0.5 - (age - 32) * 0.8,
    };

    // Ajuste de cada atributo pela função: os da posição sobem, os alheios descem.
    private sealed record Bias(
        double Passing, double Dribbling, double Finishing, double Crossing, double Tackling, double Marking, double Heading,
        double Vision, double Positioning, double Decisions, double Composure, double Pace, double Stamina, double Strength, double Other)
    {
        private static readonly Bias Goalkeeper = new(-4, -6, -7, -6, -5, -5, -4, -2, 1, 0, 1, -3, -2, 0, -1);
        private static readonly Bias Defender = new(-1, -3, -4, -1, 2, 2, 2, -1, 2, 0, 0, 0, 0, 2, 0);
        private static readonly Bias Midfielder = new(2, 1, -1, 1, -1, -1, -2, 2, 0, 2, 0, 0, 2, -1, 0);
        private static readonly Bias Forward = new(-1, 2, 2, 0, -4, -4, 1, 0, -1, 0, 2, 2, 0, 0, 0);

        public static Bias For(Position position) => position switch
        {
            Position.Goalkeeper => Goalkeeper,
            Position.Defender => Defender,
            Position.Midfielder => Midfielder,
            _ => Forward,
        };
    }

    // ponytail: as nacionalidades fora do top 10 (cerca de 2%) viram proporção das listadas; incluir "outras" se fizer falta.
    private static string PickNationality(Random random, IReadOnlyList<NationalityShare> shares)
    {
        var roll = random.NextDouble() * shares.Sum(n => n.Share);
        foreach (var n in shares)
            if ((roll -= n.Share) < 0)
                return n.Code;
        return shares[^1].Code;
    }

    private static int Clamp(double value, int min, int max) => Math.Clamp((int)Math.Round(value), min, max);

    // Normal(média, desvio) por Box-Muller.
    private static double Normal(Random random, double mean, double sd) =>
        mean + sd * Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
}
