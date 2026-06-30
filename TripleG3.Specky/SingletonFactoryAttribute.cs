using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>
/// Registers the attributed static method as a singleton service factory.
/// </summary>
/// <typeparam name="TService">The service type produced by the factory.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SingletonFactoryAttribute<TService> : SpeckyFactoryAttribute where TService : class
{
    /// <summary>
    /// Creates a singleton factory registration for <typeparamref name="TService"/>.
    /// </summary>
    public SingletonFactoryAttribute() : base(ServiceLifetime.Singleton, typeof(TService))
    {
    }
}
