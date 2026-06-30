using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>
/// Registers the attributed static method as a scoped service factory.
/// </summary>
/// <typeparam name="TService">The service type produced by the factory.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ScopedFactoryAttribute<TService> : SpeckyFactoryAttribute where TService : class
{
    /// <summary>
    /// Creates a scoped factory registration for <typeparamref name="TService"/>.
    /// </summary>
    public ScopedFactoryAttribute() : base(ServiceLifetime.Scoped, typeof(TService))
    {
    }
}
