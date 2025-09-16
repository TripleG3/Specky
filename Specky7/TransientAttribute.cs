using Microsoft.Extensions.DependencyInjection;

namespace Specky7;

/// <summary>Registers the decorated type as a transient service of itself.</summary>
public class TransientAttribute : SpeckAttribute
{
    /// <summary>Create a transient registration of the decorated type.</summary>
    public TransientAttribute() : base(ServiceLifetime.Transient) { }
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
