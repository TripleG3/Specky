using Microsoft.Extensions.DependencyInjection;

namespace Specky7;

/// <summary>Registers the decorated type as a scoped service of itself.</summary>
public class ScopedAttribute : SpeckAttribute
{
    /// <summary>Create a scoped registration of the decorated type.</summary>
    public ScopedAttribute() : base(ServiceLifetime.Scoped) { }
}

/// <summary>Registers the decorated type as the implementation of <typeparamref name="T"/> with scoped lifetime.</summary>
public class ScopedAttribute<T> : SpeckAttribute<T> where T : class
{
    /// <summary>Create a scoped registration of <typeparamref name="T"/> implemented by the decorated type.</summary>
    public ScopedAttribute() : base(ServiceLifetime.Scoped) { }
}

/// <summary>
/// Registers the decorated type as the implementation of <typeparamref name="T"/> with scoped lifetime and associates it with a key.
/// </summary>
/// <typeparam name="T">The service type to register.</typeparam>
/// <param name="key">The key to associate with the service registration.</param>
public class ScopedKeyedAttribute<T>(object key) : SpeckKeyedAttribute<T>(key, ServiceLifetime.Scoped) where T : class
{
}
