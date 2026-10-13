using System.Reflection;

namespace DonoDaBola.Core.Tests;

public class SmokeTests
{
    // Valida a configuração: o Core compila, é carregado pelos testes e não referencia a Stride.
    [Fact]
    public void CoreLoadsWithoutStride()
    {
        var core = Assembly.Load("DonoDaBola.Core");

        Assert.DoesNotContain(core.GetReferencedAssemblies(), a => a.Name!.StartsWith("Stride"));
    }
}
