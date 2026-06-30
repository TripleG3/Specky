using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>
/// Registers the attributed static method as a service factory with an explicit lifetime and service type.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ServiceFactoryAttribute : SpeckyFactoryAttribute
{
    /// <summary>
    /// Creates a service factory registration.
    /// </summary>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <param name="serviceType">The service type produced by the factory.</param>
    public ServiceFactoryAttribute(ServiceLifetime serviceLifetime, Type serviceType) : base(serviceLifetime, serviceType)
    {
    }
}
