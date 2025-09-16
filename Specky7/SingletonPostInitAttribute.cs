using Microsoft.Extensions.DependencyInjection;

namespace Specky7;

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