using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>Registers the decorated type as a singleton and schedules immediate resolution post pipeline build.</summary>
public class SingletonPostInitAttribute : SpeckAttribute
{
    /// <summary>Create a singleton registration and mark it for immediate post-init resolution.</summary>
    public SingletonPostInitAttribute() : base(ServiceLifetime.Singleton) { IsPostInit = true; }

    /// <summary>Create a singleton registration for the specified service type and mark it for immediate post-init resolution.</summary>
    public SingletonPostInitAttribute(Type serviceType) : base(ServiceLifetime.Singleton, serviceType) { IsPostInit = true; }
}

/// <summary>Registers the decorated type as the implementation of <typeparamref name="T"/> with singleton lifetime and immediate post-init resolution.</summary>
public class SingletonPostInitAttribute<T> : SpeckAttribute<T> where T : class
{
    /// <summary>Create a singleton registration for <typeparamref name="T"/> and mark for immediate post-init resolution.</summary>
    public SingletonPostInitAttribute() : base(ServiceLifetime.Singleton) { IsPostInit = true; }
}

/// <summary>
/// Registers the decorated type for multiple service types with singleton lifetime and post-init resolution.
/// </summary>
public class MultiSingletonPostInitAttribute : SpeckAttribute
{
    /// <summary>Create a multi-service singleton registration and mark it for immediate post-init resolution.</summary>
    public MultiSingletonPostInitAttribute(Type serviceType1, Type serviceType2, params Type[] additionalServiceTypes)
        : base(ServiceLifetime.Singleton, BuildServiceTypes(serviceType1, serviceType2, additionalServiceTypes))
    {
        IsPostInit = true;
    }
}