using DonoDaBola.Core;
using DonoDaBola.Core.Simulation;
using DonoDaBola.Core.Tests;

namespace DonoDaBola.Sandbox.Tests;

public class NarrationFormatterTests
{
    // Todo tipo de evento precisa de narração. Quando entrar um tipo novo (ex.: cartões no M2) sem frase,
    // o Describe lança e este teste quebra.
    [Fact]
    public void EveryEventType_HasNarration()
    {
        var home = TestPlayers.CreateClub("casa");
        var away = TestPlayers.CreateClub("fora");
        var homeLineup = Lineup.Pick442(home);
        var awayLineup = Lineup.Pick442(away);

        foreach (var type in Enum.GetValues<MatchEventType>())
        {
            var e = new MatchEvent(10, type, home, homeLineup.Forwards[0], awayLineup.Goalkeeper);
            var result = new MatchResult(home, away, homeLineup, awayLineup, 0, 0, [e]);

            Assert.False(string.IsNullOrWhiteSpace(NarrationFormatter.Describe(e, result)), type.ToString());
        }
    }
}
