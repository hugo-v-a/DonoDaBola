using DonoDaBola.Core.Competition;
using DonoDaBola.Core.Generation;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Tests;

// Referência gravada na main 6676904, antes dos cartões: placar e gols (minuto:jogador) de 20 partidas e a
// tabela final da seed 42. Com a disciplina desligada e estado neutro, o motor tem de repetir tudo exatamente.
// Se um teste daqui quebrar, a simulação mudou: só atualize os valores com a mudança explicada no PR.
public class RegressionTests
{
    internal static readonly IReadOnlyList<Club> League =
        LeagueGenerator.Create(new Random(7), GenerationTests.Stats, SeasonConfig.Default.Start).Clubs;

    public const string Season42 =
        "clu02:81:82:27;clu04:78:70:25;clu03:73:56:30;clu01:72:53:32;clu09:68:62:39;clu12:67:41:34;clu05:65:62:39;" +
        "clu06:63:54:36;clu08:51:50:46;clu07:47:48:56;clu20:46:39:52;clu10:44:40:59;clu14:43:39:52;clu13:42:43:56;" +
        "clu11:39:42:48;clu16:38:33:57;clu18:38:39:56;clu19:31:29:75;clu17:30:29:63;clu15:28:29:58";

    internal static string Shape(MatchResult r) =>
        $"{r.HomeGoals}-{r.AwayGoals}|" + string.Join(";", r.Events.Where(e => e.Type == MatchEventType.Goal).Select(e => $"{e.Minute}:{e.Player.Id}"));

    internal static string Table(Season s) =>
        string.Join(";", s.Table().Select(x => $"{x.Club.Id}:{x.Points}:{x.GoalsFor}:{x.GoalsAgainst}"));

    public static TheoryData<int, string, string, string> Matches => new()
    {
        { 1, "clu01", "clu20", "4-0|26:clu01-p25;43:clu01-p25;51:clu01-p25;59:clu01-p20" },
        { 2, "clu02", "clu19", "2-2|5:clu02-p18;16:clu19-p24;18:clu02-p25;73:clu19-p22" },
        { 3, "clu03", "clu18", "1-0|45:clu03-p06" },
        { 4, "clu04", "clu17", "4-0|12:clu04-p28;15:clu04-p08;44:clu04-p07;89:clu04-p28" },
        { 5, "clu05", "clu16", "5-0|5:clu05-p07;14:clu05-p15;37:clu05-p23;41:clu05-p07;48:clu05-p18" },
        { 6, "clu06", "clu15", "6-1|12:clu06-p26;17:clu06-p26;34:clu15-p23;46:clu06-p13;50:clu06-p28;69:clu06-p21;70:clu06-p26" },
        { 7, "clu07", "clu14", "3-2|10:clu07-p10;12:clu07-p27;24:clu14-p23;80:clu07-p16;90:clu14-p23" },
        { 8, "clu08", "clu13", "0-0|" },
        { 9, "clu09", "clu12", "1-0|11:clu09-p26" },
        { 10, "clu10", "clu11", "0-2|16:clu11-p23;72:clu11-p24" },
        { 11, "clu20", "clu01", "1-2|76:clu01-p26;80:clu01-p26;84:clu20-p24" },
        { 12, "clu19", "clu02", "1-4|32:clu02-p13;58:clu02-p26;72:clu02-p15;77:clu19-p28;83:clu02-p26" },
        { 13, "clu18", "clu03", "0-2|11:clu03-p19;65:clu03-p25" },
        { 14, "clu17", "clu04", "0-3|30:clu04-p28;48:clu04-p08;65:clu04-p08" },
        { 15, "clu16", "clu05", "0-0|" },
        { 16, "clu15", "clu06", "1-1|19:clu15-p19;30:clu06-p26" },
        { 17, "clu14", "clu07", "1-2|46:clu07-p09;68:clu14-p13;81:clu07-p20" },
        { 18, "clu13", "clu08", "1-1|48:clu08-p22;50:clu13-p18" },
        { 19, "clu12", "clu09", "0-1|46:clu09-p26" },
        { 20, "clu11", "clu10", "1-1|10:clu11-p24;53:clu10-p27" },
    };

    internal static Club ClubOf(string id) => League.Single(c => c.Id == id);

    [Theory]
    [MemberData(nameof(Matches))]
    public void Match_SameAsReference(int seed, string home, string away, string expected) =>
        Assert.Equal(expected, Shape(MatchEngine.Simulate(ClubOf(home), ClubOf(away), new Random(seed))));

    [Fact]
    public void Season42_SameAsReference()
    {
        var season = Season.Create(League, 42);
        while (!season.IsFinished) season.AdvanceRound();
        Assert.Equal(Season42, Table(season));
    }
}
