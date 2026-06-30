using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>
/// Registers the attributed static method as a transient service factory.
/// </summary>
/// <typeparam name="TService">The service type produced by the factory.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TransientFactoryAttribute<TService> : SpeckyFactoryAttribute where TService : class
{
    /// <summary>
    /// Creates a transient factory registration for <typeparamref name="TService"/>.
    /// </summary>
    public TransientFactoryAttribute() : base(ServiceLifetime.Transient, typeof(TService))
    {
    }
}
