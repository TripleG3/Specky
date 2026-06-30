using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>Registers the decorated type as a transient service of itself.</summary>
public class TransientAttribute : SpeckAttribute
{
    /// <summary>Create a transient registration of the decorated type.</summary>
    public TransientAttribute() : base(ServiceLifetime.Transient) { }

    /// <summary>Create a transient registration for the specified service type.</summary>
    public TransientAttribute(Type serviceType) : base(ServiceLifetime.Transient, serviceType) { }
}

/// <summary>Registers the decorated type as the implementation of <typeparamref name="T"/> with transient lifetime.</summary>
public class TransientAttribute<T> : SpeckAttribute<T> where T : class
{
    /// <summary>Create a transient registration of <typeparamref name="T"/> implemented by the decorated type.</summary>
    public TransientAttribute() : base(ServiceLifetime.Transient) { }
}

/// <summary>
/// Registers the decorated type as the implementation of <typeparamref name="T"/> with transient lifetime and associates it with a key.
/// </summary>
/// <typeparam name="T">The service type to register.</typeparam>
/// <param name="key">The key to associate with the service registration.</param>
public class TransientKeyedAttribute<T>(object key) : SpeckKeyedAttribute<T>(key, ServiceLifetime.Transient) where T : class
{
}

/// <summary>
/// Registers the decorated type for multiple service types with transient lifetime.
/// </summary>
public class MultiTransientAttribute(Type serviceType1, Type serviceType2, params Type[] additionalServiceTypes)
    : SpeckAttribute(ServiceLifetime.Transient, BuildServiceTypes(serviceType1, serviceType2, additionalServiceTypes))
{
}
