using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>
/// Base attribute for method-based service factory registrations.
/// </summary>
public abstract class SpeckyFactoryAttribute : Attribute
{
    /// <summary>
    /// Creates a new factory registration attribute.
    /// </summary>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <param name="serviceType">The service type produced by the factory.</param>
    protected SpeckyFactoryAttribute(ServiceLifetime serviceLifetime, Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ServiceLifetime = serviceLifetime;
        ServiceType = serviceType;
    }

    /// <summary>The service lifetime.</summary>
    public ServiceLifetime ServiceLifetime { get; }

    /// <summary>The service type produced by the factory.</summary>
    public Type ServiceType { get; }
}
