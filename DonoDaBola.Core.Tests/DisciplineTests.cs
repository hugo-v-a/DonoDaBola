using DonoDaBola.Core.Competition;
using DonoDaBola.Core.Simulation;

namespace DonoDaBola.Core.Tests;

// Cartões na partida, suspensões, critérios 5 e 6 do desempate e escalação com indisponíveis.
public class DisciplineTests
{
    private static readonly IReadOnlyList<Club> League = RegressionTests.League;
    private static Club C(string id) => RegressionTests.ClubOf(id);

    private static MatchResult WithCards(Club home, Club away, int seed, DisciplineRules? rules = null) =>
        MatchEngine.Simulate(home, away, new Random(seed), new MatchOptions { Discipline = rules ?? DisciplineRules.Default, Cards = new Random(seed + 1000) });

    private static SeasonCard Card(int round, string player, MatchEventType type, string club = "a", int minute = 10) =>
        new(round, 0, minute, club, player, type);

    // ---------- Regressão ----------

    [Theory]
    [MemberData(nameof(RegressionTests.Matches), MemberType = typeof(RegressionTests))]
    public void Off_SameAsReference(int seed, string home, string away, string expected) =>
        Assert.Equal(expected, RegressionTests.Shape(MatchEngine.Simulate(C(home), C(away), new Random(seed), new MatchOptions { Discipline = DisciplineRules.Off })));

    [Fact]
    public void CardsOn_KeepTheGoals_UntilTheFirstSendingOff()
    {
        // Amarelo não muda nada nos gols; a partir da 1ª expulsão, a força do time muda e o placar pode mudar.
        static IEnumerable<string> Goals(MatchResult m, int upTo) =>
            m.Events.Where(e => e.Type == MatchEventType.Goal && e.Minute <= upTo).Select(e => $"{e.Minute}:{e.Player.Id}");
        var noRed = 0;
        foreach (var row in RegressionTests.Matches)
        {
            var (seed, home, away, expected) = ((int)row[0], (string)row[1], (string)row[2], (string)row[3]);
            var r = WithCards(C(home), C(away), seed);
            var firstOff = r.Events.FirstOrDefault(e => e.Type is MatchEventType.RedCard or MatchEventType.SecondYellow)?.Minute ?? 91;
            var reference = MatchEngine.Simulate(C(home), C(away), new Random(seed));
            Assert.Equal(Goals(reference, firstOff), Goals(r, firstOff));
            if (firstOff == 91)
            {
                noRed++;
                Assert.Equal(expected, RegressionTests.Shape(r));
            }
        }
        Assert.True(noRed >= 10, $"só {noRed} dos 20 jogos sem expulsão");
    }

    [Fact]
    public void Goalkeeper_IsNeverSentOff()
    {
        var keepers = League.SelectMany(c => c.Squad).Where(p => p.Position == Position.Goalkeeper).Select(p => p.Id).ToHashSet();
        for (var seed = 1; seed <= 30; seed++)
        {
            var season = Season.Create(League, seed);
            while (!season.IsFinished) season.AdvanceRound();
            Assert.DoesNotContain(season.Cards, c => c.Type != MatchEventType.YellowCard && keepers.Contains(c.PlayerId));
        }
    }

    // ---------- Partida ----------

    [Fact]
    public void Cards_AreOrderedAndValid()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var r = WithCards(C("clu01"), C("clu02"), seed);
            Assert.Equal(r.Events.OrderBy(e => e.Minute), r.Events);
            foreach (var e in r.Events.Where(e => !e.IsShot))
            {
                Assert.Null(e.Goalkeeper);
                var lineup = e.Club.Id == r.Home.Id ? r.HomeLineup : r.AwayLineup;
                Assert.Contains(e.Player, lineup.Players);   // quem recebeu é do clube do evento
                if (e.Type != MatchEventType.YellowCard) Assert.NotEqual(Position.Goalkeeper, e.Player.Position);
            }
            foreach (var g in r.Events.Where(e => e.Type == MatchEventType.SecondYellow))
                Assert.Single(r.Events, e => e.Type == MatchEventType.YellowCard && e.Player == g.Player && e.Minute <= g.Minute);
        }
    }

    [Fact]
    public void SentOffPlayer_NeverActsAgain_AndTeamShootsLess()
    {
        int shotsWithRed = 0, gamesWithRed = 0, shotsClean = 0, gamesClean = 0;
        for (var seed = 0; seed < 1500; seed++)
        {
            var r = WithCards(C("clu05"), C("clu06"), seed);
            var off = r.Events.FirstOrDefault(e => e.Club.Id == "clu05" && e.Type is MatchEventType.RedCard or MatchEventType.SecondYellow);
            if (off is not null)
            {
                Assert.DoesNotContain(r.Events, e => e.Player == off.Player && e.Minute > off.Minute);
                if (off.Minute <= 30) { gamesWithRed++; shotsWithRed += r.Events.Count(e => e.IsShot && e.Club.Id == "clu05" && e.Minute > 30); }
            }
            else if (!r.Events.Any(e => e.Type is MatchEventType.RedCard or MatchEventType.SecondYellow))
            {
                gamesClean++; shotsClean += r.Events.Count(e => e.IsShot && e.Club.Id == "clu05" && e.Minute > 30);
            }
        }
        Assert.True(gamesWithRed > 10, $"poucos jogos com vermelho cedo: {gamesWithRed}");
        Assert.True(shotsWithRed / (double)gamesWithRed < shotsClean / (double)gamesClean);
    }

    [Fact]
    public void CardRates_PerMatch_BothTeams_InTargetRange()
    {
        // Faixas por jogo, SOMANDO os dois times (ADR 0005): 4,5–6,0 amarelos (≈ 2,7 por time) e 0,15–0,40 vermelho.
        int games = 0, yellows = 0, reds = 0;
        for (var seed = 1; seed <= 20; seed++)
        {
            var season = Season.Create(League, seed);
            while (!season.IsFinished) season.AdvanceRound();
            games += season.Matches.Count;
            yellows += season.Cards.Count(c => c.Type is MatchEventType.YellowCard);
            reds += season.Cards.Count(c => c.Type is MatchEventType.RedCard or MatchEventType.SecondYellow);
        }
        Assert.InRange(yellows / (double)games, 4.5, 6.0);
        Assert.InRange(reds / (double)games, 0.15, 0.40);
    }

    // ---------- Suspensões ----------

    [Fact]
    public void ThirdYellow_SuspendsOneRound_AndRestartsTheSeries()
    {
        var cards = new[] { Card(1, "p", MatchEventType.YellowCard), Card(3, "p", MatchEventType.YellowCard), Card(4, "p", MatchEventType.YellowCard) };
        var s = Assert.Single(Discipline.Suspensions(cards, DisciplineRules.Default));
        Assert.Equal((5, 1, SuspensionReason.Yellows), (s.FromRound, s.Rounds, s.Reason));
        Assert.Equal(6, Discipline.ReturnRound(cards, DisciplineRules.Default, "p", 5));
        Assert.Null(Discipline.ReturnRound(cards, DisciplineRules.Default, "p", 6));

        // Série recomeça: mais dois amarelos não suspendem; o terceiro, sim.
        var more = cards.Concat([Card(6, "p", MatchEventType.YellowCard), Card(7, "p", MatchEventType.YellowCard)]).ToList();
        Assert.Single(Discipline.Suspensions(more, DisciplineRules.Default));
        more.Add(Card(9, "p", MatchEventType.YellowCard));
        Assert.Equal(10, Discipline.Suspensions(more, DisciplineRules.Default)[^1].FromRound);
    }

    [Fact]
    public void DirectRed_AndSecondYellow_SuspendOneRound()
    {
        var red = Assert.Single(Discipline.Suspensions([Card(2, "p", MatchEventType.RedCard)], DisciplineRules.Default));
        Assert.Equal((3, 1, SuspensionReason.RedCard), (red.FromRound, red.Rounds, red.Reason));

        var second = Assert.Single(Discipline.Suspensions(
            [Card(2, "p", MatchEventType.YellowCard, minute: 20), Card(2, "p", MatchEventType.SecondYellow, minute: 70)], DisciplineRules.Default));
        Assert.Equal((3, 1, SuspensionReason.SecondYellow), (second.FromRound, second.Rounds, second.Reason));
    }

    [Fact]
    public void SecondYellow_YellowsDoNotCountForTheSeries()
    {
        // Dois amarelos antes, depois uma expulsão por 2º amarelo: os amarelos da expulsão não completam a série.
        var cards = new[]
        {
            Card(1, "p", MatchEventType.YellowCard), Card(2, "p", MatchEventType.YellowCard),
            Card(3, "p", MatchEventType.YellowCard, minute: 10), Card(3, "p", MatchEventType.SecondYellow, minute: 60),
        };
        Assert.Equal([SuspensionReason.SecondYellow], Discipline.Suspensions(cards, DisciplineRules.Default).Select(s => s.Reason));
    }

    [Fact]
    public void ThirdYellowAndRed_SameMatch_TwoRounds_InSequence()
    {
        // Como no caso real de 2026: 3º amarelo e vermelho direto no mesmo jogo afastam por 2 rodadas.
        var cards = new[]
        {
            Card(1, "p", MatchEventType.YellowCard), Card(2, "p", MatchEventType.YellowCard),
            Card(5, "p", MatchEventType.YellowCard, minute: 30), Card(5, "p", MatchEventType.RedCard, minute: 45),
        };
        var s = Discipline.Suspensions(cards, DisciplineRules.Default);
        Assert.Equal([(6, 1), (7, 1)], s.Select(x => (x.FromRound, x.Rounds)));
        Assert.Equal(8, Discipline.ReturnRound(cards, DisciplineRules.Default, "p", 6));
        Assert.Equal(8, Discipline.ReturnRound(cards, DisciplineRules.Default, "p", 7));
    }

    [Fact]
    public void Rules_AreConfigurable()
    {
        var strict = DisciplineRules.Default with { YellowsForSuspension = 2, RedCardSuspension = 3 };
        var cards = new[] { Card(1, "p", MatchEventType.YellowCard), Card(2, "p", MatchEventType.YellowCard), Card(4, "q", MatchEventType.RedCard) };
        Assert.Equal([(3, 1), (5, 3)], Discipline.Suspensions(cards, strict).Select(x => (x.FromRound, x.Rounds)));
    }

    [Fact]
    public void Suspended_MissesNextRound_AndReturns()
    {
        var season = Season.Create(League, 21);
        Suspension? first = null;
        while (first is null && !season.IsFinished)
        {
            season.AdvanceRound();
            first = Discipline.Suspensions(season.Cards, season.Discipline).FirstOrDefault(s => s.FromRound == season.CurrentRound);
        }
        Assert.NotNull(first);

        var round = first!.FromRound;
        var back = Assert.Single(season.UnavailableOf(first.ClubId), u => u.Player.Id == first.PlayerId).ReturnRound;
        Assert.True(back >= round + first.Rounds);
        var results = season.AdvanceRound();
        var lineup = results.Select(r => r.Home.Id == first.ClubId ? r.HomeLineup : r.Away.Id == first.ClubId ? r.AwayLineup : null).Single(l => l is not null)!;
        Assert.DoesNotContain(lineup.Players, p => p.Id == first.PlayerId);

        while (season.CurrentRound < back) season.AdvanceRound();
        Assert.DoesNotContain(season.UnavailableOf(first.ClubId), u => u.Player.Id == first.PlayerId); // cumpriu: pode jogar
    }

    // ---------- Desempate (critérios 5 e 6) ----------

    [Fact]
    public void TieBreakCards_SecondYellow_IsOneRedAndOneYellow()
    {
        var cards = new[]
        {
            Card(1, "p", MatchEventType.YellowCard, minute: 10), Card(1, "p", MatchEventType.SecondYellow, minute: 50),
            Card(1, "q", MatchEventType.RedCard), Card(2, "r", MatchEventType.YellowCard), Card(2, "x", MatchEventType.YellowCard, club: "b"),
        };
        Assert.Equal((2, 2), Discipline.TieBreakCards(cards, "a", DisciplineRules.Default));
        // Com o flag desligado, a expulsão por 2º amarelo conta só o vermelho.
        Assert.Equal((2, 1), Discipline.TieBreakCards(cards, "a", DisciplineRules.Default with { SecondYellowCountsAsYellow = false }));
    }

    [Fact]
    public void TieBreakers_IncludeCardsBeforeDraw() =>
        Assert.Equal(
            ["Vitórias", "Saldo de gols", "Gols pró", "Confronto direto (pontos)", "Confronto direto (saldo)", "Cartões vermelhos", "Cartões amarelos", "Sorteio"],
            Standings.TieBreakers.Select(t => t.Name));
}
