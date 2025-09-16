using Microsoft.Extensions.DependencyInjection;

namespace Specky7;

/// <summary>
/// Base attribute used by Specky to register services. Can be applied to classes or configuration interface members.
/// When applied without a service type it registers the decorated type as both service and implementation.
/// </summary>
/// <remarks>
/// Create a new speck attribute.
/// </remarks>
/// <param name="serviceLifetime">Lifetime of the service.</param>
/// <param name="serviceType">Optional explicit service type (defaults to decorated type / member type).</param>
/// <param name="key">Optional key. When provided dependency will be injected as a KeyService.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
public class SpeckAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton, Type? serviceType = null, object? key = null) : Attribute
{
    /// <summary>Requested service lifetime.</summary>
    public ServiceLifetime ServiceLifetime { get; init; } = serviceLifetime;
    /// <summary>The service abstraction type if supplied (generic wrapper sets this automatically).</summary>
    public Type? ServiceType { get; init; } = serviceType;
    internal bool IsPostInit { get; set; }
    internal object? Key { get; init; } = key;
}

/// <summary>
/// Generic convenience variant specifying the service type explicitly via type parameter.
/// </summary>
/// <inheritdoc />
public class SpeckAttribute<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) : SpeckAttribute(serviceLifetime, typeof(T)) where T : class
{
}

/// <summary>
/// Attribute used to register a keyed service with a specified type and lifetime.
/// </summary>
/// <typeparam name="T">The service type to register.</typeparam>
/// <param name="key">The key to associate with the service.</param>
/// <param name="serviceLifetime">The lifetime of the service (default is Singleton).</param>
public class SpeckKeyedAttribute<T>(object key, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) : SpeckAttribute(serviceLifetime, typeof(T), key) where T : class
{
}