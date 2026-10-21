using System.Reflection;

namespace DonoDaBola.Core.Tests;

public class SmokeTests
{
    // Regra de arquitetura: o Core é C# puro, sem Stride e sem SQLite (quem fala com o banco é o DonoDaBola.Data).
    [Theory]
    [InlineData("Stride")]
    [InlineData("Microsoft.Data.Sqlite")]
    [InlineData("SQLitePCL")]
    public void CoreDoesNotReference(string forbiddenPrefix)
    {
        var core = Assembly.Load("DonoDaBola.Core");

        Assert.DoesNotContain(core.GetReferencedAssemblies(), a => a.Name!.StartsWith(forbiddenPrefix));
    }
}
