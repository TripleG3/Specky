using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TripleG3.Specky.Tests;

[TestClass]
public class GeneratorSmokeTests
{
    [TestMethod]
    public void GeneratedRegistrationMethodCanBeCalled()
    {
        var services = new ServiceCollection();

        services.AddTripleG3SpeckyGenerated();

        Assert.IsNotNull(services);
    }
}
