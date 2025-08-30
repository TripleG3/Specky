using Microsoft.Extensions.DependencyInjection;

namespace Specky7;

/// <summary>
/// Base attribute used by Specky to register services. Can be applied to classes or configuration interface members.
/// When applied without a service type it registers the decorated type as both service and implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
public class SpeckAttribute : Attribute
{
    /// <summary>
    /// Create a new speck attribute.
    /// </summary>
    /// <param name="serviceLifetime">Lifetime of the service.</param>
    /// <param name="serviceType">Optional explicit service type (defaults to decorated type / member type).</param>
    public SpeckAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton, Type? serviceType = null)
    {
        ServiceLifetime = serviceLifetime;
        ServiceType = serviceType;
    }
    /// <summary>Requested service lifetime.</summary>
    public ServiceLifetime ServiceLifetime { get; init; }
    /// <summary>The service abstraction type if supplied (generic wrapper sets this automatically).</summary>
    public Type? ServiceType { get; init; }
    internal bool IsPostInit { get; set; }
}

/// <summary>
/// Generic convenience variant specifying the service type explicitly via type parameter.
/// </summary>
public class SpeckAttribute<T> : SpeckAttribute where T : class
{
    /// <inheritdoc />
    public SpeckAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) : base(serviceLifetime, typeof(T)) { }
}

/// <summary>Registers the decorated type as a singleton service of itself.</summary>
public class SingletonAttribute : SpeckAttribute { }
/// <summary>Registers the decorated type as the implementation of <typeparamref name="T"/> with singleton lifetime.</summary>
public class SingletonAttribute<T> : SpeckAttribute<T> where T : class { }

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

/// <summary>Registers the decorated type as a singleton and schedules immediate resolution post pipeline build.</summary>
public class SingletonPostInitAttribute : SpeckAttribute
{
    /// <summary>Create a singleton registration and mark it for immediate post-init resolution.</summary>
    public SingletonPostInitAttribute() : base(ServiceLifetime.Singleton) { IsPostInit = true; }
}

/// <summary>Registers the decorated type as the implementation of <typeparamref name="T"/> with singleton lifetime and immediate post-init resolution.</summary>
public class SingletonPostInitAttribute<T> : SpeckAttribute<T> where T : class
{
    /// <summary>Create a singleton registration for <typeparamref name="T"/> and mark for immediate post-init resolution.</summary>
    public SingletonPostInitAttribute() : base(ServiceLifetime.Singleton) { IsPostInit = true; }
}