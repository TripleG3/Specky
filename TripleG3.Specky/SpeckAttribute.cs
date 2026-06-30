using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

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
    /// <summary>
    /// Builds a validated service-type list for multi-service attribute registrations.
    /// </summary>
    /// <param name="serviceType1">The first service type.</param>
    /// <param name="serviceType2">The second service type.</param>
    /// <param name="additionalServiceTypes">Any additional service types.</param>
    /// <returns>An array containing all requested service types.</returns>
    protected static Type[] BuildServiceTypes(Type serviceType1, Type serviceType2, params Type[] additionalServiceTypes)
    {
        ArgumentNullException.ThrowIfNull(serviceType1);
        ArgumentNullException.ThrowIfNull(serviceType2);

        var serviceTypes = new Type[additionalServiceTypes.Length + 2];
        serviceTypes[0] = serviceType1;
        serviceTypes[1] = serviceType2;

        for (var index = 0; index < additionalServiceTypes.Length; index++)
        {
            serviceTypes[index + 2] = additionalServiceTypes[index] ?? throw new ArgumentNullException(nameof(additionalServiceTypes));
        }

        return serviceTypes;
    }

    private static Type[] ValidateServiceTypes(Type[] serviceTypes, string paramName)
    {
        ArgumentNullException.ThrowIfNull(serviceTypes);

        if (serviceTypes.Length == 0)
        {
            throw new ArgumentException("At least one service type must be provided.", paramName);
        }

        for (var index = 0; index < serviceTypes.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(serviceTypes[index], paramName);
        }

        return serviceTypes;
    }

    /// <summary>Requested service lifetime.</summary>
    public ServiceLifetime ServiceLifetime { get; init; } = serviceLifetime;
    /// <summary>The service abstraction type if supplied (generic wrapper sets this automatically).</summary>
    public Type? ServiceType { get; init; } = serviceType;
    /// <summary>Optional service abstraction types when registering the decorated implementation for multiple contracts.</summary>
    public IReadOnlyList<Type> ServiceTypes { get; init; } = serviceType is null ? Array.Empty<Type>() : [serviceType];
    internal bool IsPostInit { get; set; }
    internal object? Key { get; init; } = key;

    /// <summary>
    /// Creates an attribute that registers the decorated implementation for multiple service types.
    /// </summary>
    protected SpeckAttribute(ServiceLifetime serviceLifetime, Type[] serviceTypes, object? key = null)
        : this(serviceLifetime, serviceType: null, key)
    {
        ServiceTypes = ValidateServiceTypes(serviceTypes, nameof(serviceTypes));
    }
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

/// <summary>
/// Registers the decorated implementation for multiple service types with a specified lifetime.
/// </summary>
public class MultiServiceSpeckAttribute : SpeckAttribute
{
    /// <summary>
    /// Registers the decorated implementation for multiple service types with singleton lifetime.
    /// </summary>
    public MultiServiceSpeckAttribute(Type serviceType1, Type serviceType2, params Type[] additionalServiceTypes)
        : base(ServiceLifetime.Singleton, BuildServiceTypes(serviceType1, serviceType2, additionalServiceTypes))
    {
    }

    /// <summary>
    /// Registers the decorated implementation for multiple service types with the specified lifetime.
    /// </summary>
    public MultiServiceSpeckAttribute(ServiceLifetime serviceLifetime, Type serviceType1, Type serviceType2, params Type[] additionalServiceTypes)
        : base(serviceLifetime, [serviceType1, serviceType2, .. additionalServiceTypes])
    {
    }
}