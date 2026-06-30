using Microsoft.Extensions.DependencyInjection;
using TripleG3.Specky;

namespace TripleG3.Specky.Tests;

internal interface IFooBoth : IFooId, IFooTime
{
}

internal interface IGenericRepository<T>
{
}

internal interface IGenericHandler<TRequest, TResponse>
{
}

internal interface ICleanGenericRepository<T>
{
}

internal interface IFactoryConsole
{
    bool IsEnabled { get; }
}

internal interface IFactoryGreetingService
{
    string Greet(string name);
}

internal interface IDescriptorRegisteredService
{
    string Name { get; }
}

internal class GenericRepository<T> : IGenericRepository<T>
{
}

internal class InvalidGenericRepository<TKey, TValue> : IGenericRepository<TKey>
{
}

[Scoped(typeof(IGenericRepository<>))]
internal class GenericScopedRepository<T> : IGenericRepository<T>
{
}

[Scoped(typeof(ICleanGenericRepository<>))]
internal class CleanGenericRepository<T> : ICleanGenericRepository<T>
{
}

internal class InvalidOpenGenericMultiMap<T> : IGenericRepository<T>
{
}

internal class A_Foo : IFooId, IFooTime
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
}

[Speck<IFooTime>]
[Scoped<IFooId>]
internal class B_Foo : IFooId, IFooTime
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
}
internal class A_FooId : IFooId
{
    public int Id { get; set; }
}
internal class B_FooId : IFooId
{
    public int Id { get; set; }
}

[Speck]
internal class A_FooTime : IFooId
{
    public int Id { get; set; }
}

[Speck(ServiceLifetime.Transient)]
internal class B_FooTime : IFooId
{
    public int Id { get; set; }
}

// Keyed test types
[SingletonKeyed<IFooTime>("TimeKey1")]
internal class Keyed_Time_Singleton : IFooTime
{
    public DateTime Time { get; set; }
}

[ScopedKeyed<IFooId>("IdKeyScoped")]
internal class Keyed_Id_Scoped : IFooId
{
    public int Id { get; set; }
}

[TransientKeyed<IFooId>("IdKeyTransient")]
internal class Keyed_Id_Transient : IFooId
{
    public int Id { get; set; }
}

[MultiScoped(typeof(IFooId), typeof(IFooTime))]
internal class MultiScopedFoo : IFooBoth
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
}

[MultiSingleton(typeof(IFooId), typeof(IFooTime))]
internal class MultiSingletonFoo : IFooBoth
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
}

[MultiTransient(typeof(IFooId), typeof(IFooTime))]
internal class MultiTransientFoo : IFooBoth
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
}

[Scoped(typeof(IGenericRepository<>))]
internal class OpenGenericDuplicateContracts<T> : IGenericRepository<T>
{
}

internal sealed class FactoryConsole : IFactoryConsole
{
    public bool IsEnabled { get; init; }
}

internal sealed class FactoryGreetingService(IFactoryConsole console) : IFactoryGreetingService
{
    public string Greet(string name) => console.IsEnabled ? $"Hello, {name}!" : string.Empty;
}

internal static class TestFactoryRegistrations
{
    [SingletonFactory<IFactoryConsole>]
    public static IFactoryConsole CreateConsole()
        => new FactoryConsole { IsEnabled = true };

    [ScopedFactory<IFactoryGreetingService>]
    public static IFactoryGreetingService CreateGreeting(IServiceProvider services)
        => new FactoryGreetingService(services.GetRequiredService<IFactoryConsole>());
}

internal sealed class DescriptorRegisteredService : IDescriptorRegisteredService
{
    public string Name => nameof(DescriptorRegisteredService);
}

[SpeckyDescriptorProvider]
public sealed class TestDescriptorProvider : ISpeckyDescriptorProvider
{
    public IEnumerable<ServiceDescriptor> GetDescriptors()
    {
        yield return ServiceDescriptor.Singleton<IDescriptorRegisteredService, DescriptorRegisteredService>();
    }
}