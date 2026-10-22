// Sandbox de integração, fora da Stride: prova o caminho Data -> Core e serve de modelo para a interface.
//   dotnet run --project tools/DonoDaBola.Sandbox -- --listar
//   dotnet run --project tools/DonoDaBola.Sandbox -- --casa clu01 --fora clu02 --seed 42 [--db <pasta>]
using System.Text;
using DonoDaBola.Core;
using DonoDaBola.Core.Simulation;
using DonoDaBola.Data;
using DonoDaBola.Sandbox;

Console.OutputEncoding = Encoding.UTF8;
// ponytail: data fixa da temporada até o IGameClock do jogo chegar (M2).
var today = new DateOnly(2026, 1, 1);

// Opção sem valor (fim da linha ou outra opção logo depois) é erro, e não uma pasta chamada "--listar".
if (MissingValue(args) is { } option)
    return Fail($"{option} precisa de um valor.");

// 1. Banco numa pasta gravável. Na Stride, a pasta vem do VirtualFileSystem (ver docs/progresso/hugo.md).
var folder = Arg("--db") ?? Path.Combine(Path.GetTempPath(), "DonoDaBola-sandbox");
using var db = GameDatabase.OpenInDirectory(folder);
Console.WriteLine($"Banco: {Path.Combine(folder, "game.db")}");

// 2. Primeira abertura: carrega os 20 clubes genéricos embutidos no Data.
if (db.IsEmpty)
    Console.WriteLine($"Banco vazio: {SeedImporter.ImportDefault(db)} clubes importados do seed.");

IClubRepository clubs = db;

// 3. Listagem dos clubes.
if (args.Contains("--listar"))
{
    Console.WriteLine($"\n{"Id",-6} {"Sigla",-5} {"Média",5}  Nome");
    foreach (var c in clubs.GetAll())
        Console.WriteLine($"{c.Id,-6} {c.ShortName,-5} {c.Squad.Average(p => Lineup.Rate(p, p.Position)),5:F1}  {c.Name}");
    return 0;
}

// 4. Partida.
var homeId = Arg("--casa") ?? "clu01";
var awayId = Arg("--fora") ?? "clu02";
var seedText = Arg("--seed") ?? "42";
if (!int.TryParse(seedText, out var seed))
    return Fail($"Seed inválida: '{seedText}'. Use um número inteiro.");
if (homeId == awayId)
    return Fail("O mandante e o visitante precisam ser clubes diferentes.");
if (clubs.Get(homeId) is not { } home)
    return Fail($"Clube '{homeId}' não existe. Use --listar para ver os ids.");
if (clubs.Get(awayId) is not { } away)
    return Fail($"Clube '{awayId}' não existe. Use --listar para ver os ids.");

var result = MatchEngine.Simulate(home, away, new Random(seed));

Console.WriteLine($"\n{home.Name} x {away.Name}  (seed {seed})");
PrintLineup(home, result.HomeLineup);
PrintLineup(away, result.AwayLineup);

Console.WriteLine("\nNarração");
var halfTimeShown = false;
foreach (var e in result.Events)
{
    if (!halfTimeShown && e.Minute > 45)
    {
        Console.WriteLine("     --- Intervalo ---");
        halfTimeShown = true;
    }
    Console.WriteLine($"{e.Minute,3}'  {NarrationFormatter.Describe(e, result)}");
}

if (!halfTimeShown)
    Console.WriteLine("     --- Intervalo ---"); // segundo tempo sem lances

Console.WriteLine($"\nFim de jogo: {home.ShortName} {result.HomeGoals} x {result.AwayGoals} {away.ShortName}");
// "No goleiro" = finalizações deste time defendidas pelo goleiro adversário.
Console.WriteLine($"\n{"",-5} {"Finalizações",12} {"Gols",5} {"No goleiro",11} {"Para fora",10}");
foreach (var club in new[] { home, away })
{
    var mine = result.Events.Where(e => e.Club.Id == club.Id).ToList();
    Console.WriteLine($"{club.ShortName,-5} {mine.Count,12} {Count(mine, MatchEventType.Goal),5} {Count(mine, MatchEventType.Save),11} {Count(mine, MatchEventType.Miss),10}");
}
return 0;

void PrintLineup(Club club, Lineup lineup)
{
    Console.WriteLine($"\n{club.ShortName} (4-4-2)");
    // Nota na função da escalação (um improvisado é avaliado na posição em que joga).
    var slots = new[] { Position.Goalkeeper }.Concat(Enumerable.Repeat(Position.Defender, 4))
        .Concat(Enumerable.Repeat(Position.Midfielder, 4)).Concat(Enumerable.Repeat(Position.Forward, 2));
    foreach (var (slot, p) in slots.Zip(lineup.Players))
        Console.WriteLine($"  {Abbrev(slot)}  {p.Name,-22} {p.AgeOn(today),2} anos  nota {Lineup.Rate(p, slot),4:F1}");
}

static string Abbrev(Position position) => position switch
{
    Position.Goalkeeper => "GOL",
    Position.Defender => "DEF",
    Position.Midfielder => "MEI",
    _ => "ATA",
};

static int Count(List<MatchEvent> events, MatchEventType type) => events.Count(e => e.Type == type);

string? Arg(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 ? args[i + 1] : null; // valor já conferido no início do programa
}

static string? MissingValue(string[] args)
{
    foreach (var option in new[] { "--db", "--casa", "--fora", "--seed" })
    {
        var i = Array.IndexOf(args, option);
        if (i >= 0 && (i + 1 >= args.Length || args[i + 1].StartsWith("--")))
            return option;
    }
    return null;
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
