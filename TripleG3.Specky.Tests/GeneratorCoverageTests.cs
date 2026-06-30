using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TripleG3.Specky.Tests;

[TestClass]
public class GeneratorCoverageTests
{
    [TestMethod]
    public void GeneratedRegistrationApiIsAvailable()
    {
        var services = new ServiceCollection();

        services.AddTripleG3Specky();

        Assert.IsNotNull(services);
    }
}
