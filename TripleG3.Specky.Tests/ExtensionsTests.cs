using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TripleG3.Specky.Tests;

[TestClass()]
public class ExtensionsTests
{
    [TestMethod()]
    public void AddInvalidConfigurationTest()
    {
        //Arrange
        var serviceProvider = new MockServiceCollecton();

        //Act and Assert
        Assert.ThrowsException<SpeckyException>(() =>
        {
            serviceProvider.AddSpecks(opts =>
            {
                opts.AddConfiguration<IInvalidConfiguration>();
            });
        });
    }

    [TestMethod()]
    public void AddInvalidOptionTest()
    {
        //Arrange
        IServiceCollection serviceProvider = new MockServiceCollecton();

        //Act and Assert
        Assert.ThrowsException<SpeckyException>(() =>
        {
            serviceProvider.AddSpecks(opts =>
            {
                opts.AddConfiguration<IInvalidConfiguration>();
                opts.AddConfiguration<IOkConfiguration>();
                opts.AddOption("Invalid");
            });
        });
    }

    [TestMethod()]
    public void AddOkOptionTest()
    {
        //Arrange
        IServiceCollection serviceProvider = new MockServiceCollecton();

        //Act
        serviceProvider.AddSpecks(opts =>
        {
            opts.AddConfiguration<IInvalidConfiguration>();
            opts.AddConfiguration<IOkConfiguration>();
            opts.AddConfiguration<IOk2Configuration>();
            opts.AddOption("Ok");
            opts.AddOption("Ok2");
        });

        //Assert
        Assert.AreEqual(4, serviceProvider.Count);
    }

    [TestMethod()]
    public void AddSpecksScanningTest()
    {
        //Arrange
        var serviceProvider = new MockServiceCollecton();

        //Act
        serviceProvider.AddSpecks(opts =>
        {
            opts.AddAssembly<ExtensionsTests>();
        });

        //Assert
        Assert.AreEqual(15, serviceProvider.Count);
        var a = serviceProvider.Any(x => x.ServiceType == typeof(IFooTime)
        && x.ImplementationType == typeof(B_Foo)
        && x.Lifetime == ServiceLifetime.Singleton);

        var b = serviceProvider.Any(x => x.ServiceType == typeof(IFooId)
        && x.ImplementationType == typeof(B_Foo)
        && x.Lifetime == ServiceLifetime.Scoped);

        var c = serviceProvider.Any(x => x.ServiceType == typeof(A_FooTime)
        && x.ImplementationType == typeof(A_FooTime)
        && x.Lifetime == ServiceLifetime.Singleton);

        var d = serviceProvider.Any(x => x.ServiceType == typeof(B_FooTime)
        && x.ImplementationType == typeof(B_FooTime)
        && x.Lifetime == ServiceLifetime.Transient);

        var keyedSingleton = serviceProvider.Any(x => x.ServiceType == typeof(IFooTime)
        && x.IsKeyedService
        && x.KeyedImplementationType == typeof(Keyed_Time_Singleton)
        && x.Lifetime == ServiceLifetime.Singleton
        && x.ServiceKey?.Equals("TimeKey1") == true);

        var keyedScoped = serviceProvider.Any(x => x.ServiceType == typeof(IFooId)
        && x.IsKeyedService
        && x.KeyedImplementationType == typeof(Keyed_Id_Scoped)
        && x.Lifetime == ServiceLifetime.Scoped
        && x.ServiceKey?.Equals("IdKeyScoped") == true);

        var keyedTransient = serviceProvider.Any(x => x.ServiceType == typeof(IFooId)
        && x.IsKeyedService
        && x.KeyedImplementationType == typeof(Keyed_Id_Transient)
        && x.Lifetime == ServiceLifetime.Transient
        && x.ServiceKey?.Equals("IdKeyTransient") == true);

        var multiScopedId = serviceProvider.Any(x => x.ServiceType == typeof(IFooId)
        && x.ImplementationType == typeof(MultiScopedFoo)
        && x.Lifetime == ServiceLifetime.Scoped);

        var multiScopedTime = serviceProvider.Any(x => x.ServiceType == typeof(IFooTime)
        && x.ImplementationType == typeof(MultiScopedFoo)
        && x.Lifetime == ServiceLifetime.Scoped);

        var multiSingletonId = serviceProvider.Any(x => x.ServiceType == typeof(IFooId)
        && x.ImplementationType == typeof(MultiSingletonFoo)
        && x.Lifetime == ServiceLifetime.Singleton);

        var multiSingletonTime = serviceProvider.Any(x => x.ServiceType == typeof(IFooTime)
        && x.ImplementationType == typeof(MultiSingletonFoo)
        && x.Lifetime == ServiceLifetime.Singleton);

        var multiTransientId = serviceProvider.Any(x => x.ServiceType == typeof(IFooId)
        && x.ImplementationType == typeof(MultiTransientFoo)
        && x.Lifetime == ServiceLifetime.Transient);

        var multiTransientTime = serviceProvider.Any(x => x.ServiceType == typeof(IFooTime)
        && x.ImplementationType == typeof(MultiTransientFoo)
        && x.Lifetime == ServiceLifetime.Transient);

        var openGenericScoped = serviceProvider.Any(x => x.ServiceType == typeof(IGenericRepository<>)
        && x.ImplementationType == typeof(GenericScopedRepository<>)
        && x.Lifetime == ServiceLifetime.Scoped);

        var openGenericDuplicateContract = serviceProvider.Any(x => x.ServiceType == typeof(IGenericRepository<>)
        && x.ImplementationType == typeof(OpenGenericDuplicateContracts<>)
        && x.Lifetime == ServiceLifetime.Scoped);

        Assert.IsTrue(a);
        Assert.IsTrue(b);
        Assert.IsTrue(c);
        Assert.IsTrue(d);
        Assert.IsTrue(keyedSingleton);
        Assert.IsTrue(keyedScoped);
        Assert.IsTrue(keyedTransient);
        Assert.IsTrue(multiScopedId);
        Assert.IsTrue(multiScopedTime);
        Assert.IsTrue(multiSingletonId);
        Assert.IsTrue(multiSingletonTime);
        Assert.IsTrue(multiTransientId);
        Assert.IsTrue(multiTransientTime);
        Assert.IsTrue(openGenericScoped);
        Assert.IsTrue(openGenericDuplicateContract);
    }

    [TestMethod()]
    public void AddSpecksUseConfigurationsOnlyStillThrowsForInvalidScannedTypes()
    {
        var serviceProvider = new MockServiceCollecton();

        var exception = Assert.ThrowsException<SpeckyException>(() => serviceProvider.AddSpecks(opts =>
        {
            opts.AddAssembly<InvalidOpenGenericMultiMap<int>>();
            opts.UseConfigurationsOnly = true;
        }));

        StringAssert.Contains(exception.Message, "interface");
    }
}