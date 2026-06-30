using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace TripleG3.Specky;

/// <summary>
/// Extension methods providing Specky service registration and post-initialization hooks.
/// </summary>
public static class Extensions
{
    private const BindingFlags ConfigurationMemberBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static readonly Type SpeckyConfigurationAttributeType = typeof(SpeckyConfigurationAttribute);
    private const string NoConfigurationsFoundMessage = "Specky was expected to inject with configurations but none was found.";
    private const string NullServiceTypeName = "null";

    // Holds attributes requiring post-init resolution
    internal static readonly HashSet<PostInitRegistration> SpeckyInitRegistrations = [];
    private static readonly object _postInitLock = new();

    /// <summary>
    /// Force resolves any services marked with <see cref="SingletonPostInitAttribute"/> (or generic variant) to trigger construction after pipeline build.
    /// </summary>
    public static IApplicationBuilder UseSpeckyPostSpecks(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        foreach (var registration in SpeckyInitRegistrations)
        {
            _ = registration.ServiceKey is null
                ? app.ApplicationServices.GetService(registration.ServiceType)
                : app.ApplicationServices.GetKeyedService(registration.ServiceType, registration.ServiceKey);
        }

        return app;
    }

    /// <summary>
    /// Add Specks by inferring assembly from generic type parameter.
    /// </summary>
    public static IServiceCollection AddSpecks<T>(this IServiceCollection serviceCollection)
        => serviceCollection.AddSpecks(opt => opt.AddAssembly<T>());

    /// <summary>
    /// Add Specks with options and inferred assembly.
    /// </summary>
    public static IServiceCollection AddSpecks<T>(this IServiceCollection serviceCollection, Action<SpeckyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentNullException.ThrowIfNull(options);

        return serviceCollection.AddSpecks(opts =>
        {
            opts.AddAssembly<T>();
            options(opts);
        });
    }

    /// <summary>
    /// Add Specks using the supplied options callback.
    /// </summary>
    /// <exception cref="SpeckyException">Thrown when required assemblies or configurations are missing or invalid.</exception>
    public static IServiceCollection AddSpecks(this IServiceCollection serviceCollection, Action<SpeckyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentNullException.ThrowIfNull(options);

        var localOptions = new SpeckyOptions();
        var entryAssembly = Assembly.GetEntryAssembly();
        options(localOptions);
        if (localOptions.Assemblies.Count == 0)
        {
            if (entryAssembly == null) throw new SpeckyException($"No assembly was found or registered for Specky to scan.\n{nameof(AddSpecks)}");
            localOptions.AddAssemblies(entryAssembly);
        }

        if (localOptions.Configurations.Count > 0)
        {
            _ = InjectInterfaceConfigurationsOnly(serviceCollection, localOptions);
        }

        if (localOptions.UseConfigurationsOnly && localOptions.Configurations.Count == 0)
        {
            var speckyConfigurationTypes = localOptions
                .Assemblies
                .SelectMany(a => SafeGetTypes(a)
                    .Where(t => t.IsDefined(SpeckyConfigurationAttributeType, false)))
                .ToArray();

            if (speckyConfigurationTypes.Length == 0)
            {
                throw new SpeckyException(NoConfigurationsFoundMessage);
            }
            localOptions.AddConfigurations(speckyConfigurationTypes);
            return InjectInterfaceConfigurationsOnly(serviceCollection, localOptions);
        }

        // Pre-compute a hash set of existing service descriptors for fast duplicate checking.
        var existing = SpeckyCaches.BuildExistingDescriptorSet(serviceCollection);
        var speckTypes = localOptions.Assemblies.SelectMany(SafeGetTypes);
        foreach (var implementationType in speckTypes)
        {
            serviceCollection.ScanTypeAndInject(implementationType, localOptions, existing);
        }
        return serviceCollection;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException rtl)
        {
            return rtl.Types.Where(t => t != null)!;
        }
    }

    private static IServiceCollection InjectInterfaceConfigurationsOnly(IServiceCollection serviceCollection, SpeckyOptions options)
    {
        var existing = SpeckyCaches.BuildExistingDescriptorSet(serviceCollection);
        var shouldFilterByOptions = options.Options.Count > 0;

        foreach (var iface in options.Configurations)
        {
            var speckyConfigurationAttribute = iface.GetCustomAttribute<SpeckyConfigurationAttribute>();
            if (speckyConfigurationAttribute == null) continue;

            if (shouldFilterByOptions)
            {
                InjectInterfaceConfigurationsWithOptionsOnly(serviceCollection, iface, speckyConfigurationAttribute, options, existing);
                continue;
            }
            ScanAndInjectInterface(serviceCollection, iface, options, existing);
        }
        return serviceCollection;
    }

    private static void InjectInterfaceConfigurationsWithOptionsOnly(IServiceCollection serviceCollection, Type iface, SpeckyConfigurationAttribute speckyConfigurationAttribute, SpeckyOptions options, HashSet<ServiceTriple> existing)
    {
        if (options.Options.Contains(speckyConfigurationAttribute.Option))
        {
            ScanAndInjectInterface(serviceCollection, iface, options, existing);
        }
    }

    private static void ScanAndInjectInterface(IServiceCollection serviceCollection, Type iface, SpeckyOptions options, HashSet<ServiceTriple> existing)
    {
        if (!iface.IsInterface)
        {
            throw new SpeckyException($"{iface.Name} must be an interface to be scanned as a speck configuration.\n{nameof(ScanAndInjectInterface)}({iface.Name})");
        }

        serviceCollection.ScanPropertiesFromConfigurationAndInject(iface, options, existing);
        serviceCollection.ScanFieldsAndInject(iface, options, existing);
        serviceCollection.ScanMethodsAndInject(iface, options, existing);
    }

    //Primary - called first when needing to locate all specks and attempt injecting all.
    internal static void ScanTypeAndInject(this IServiceCollection serviceCollection, Type implementationType, SpeckyOptions options, HashSet<ServiceTriple> existing)
    {
        var specks = SpeckyCaches.GetTypeSpeckAttributes(implementationType);
        foreach (var speck in specks)
        {
            foreach (var serviceType in speck.ServiceTypes.Count == 0 ? [implementationType] : speck.ServiceTypes)
            {
                try
                {
                    serviceCollection.AddSpeck(serviceType, implementationType, speck.ServiceLifetime, speck.Key, options, existing);
                }
                catch (TypeAccessException ex)
                {
                    throw new SpeckyException($"Specky could not inject service type {serviceType.Name} with implementation type {implementationType.Name} for an unknown reason.\n{speck.ServiceType?.Name ?? NullServiceTypeName}.{implementationType.Name}", ex);
                }

                if (speck.IsPostInit)
                {
                    lock (_postInitLock)
                    {
                        SpeckyInitRegistrations.Add(new PostInitRegistration(serviceType, speck.Key));
                    }
                }
            }
        }
    }

    internal static void ScanPropertiesFromConfigurationAndInject(this IServiceCollection serviceCollection, Type type, SpeckyOptions options, HashSet<ServiceTriple> existing)
    {
        var propertyInfos = type.GetProperties(ConfigurationMemberBindingFlags);
        foreach (var propertyInfo in propertyInfos)
        {
            var specks = SpeckyCaches.GetSpeckAttributes((MemberInfo)propertyInfo);
            foreach (var speck in specks)
            {
                foreach (var serviceType in speck.ServiceTypes.Count == 0 ? [propertyInfo.PropertyType] : speck.ServiceTypes)
                {
                    try
                    {
                        serviceCollection.AddSpeck(serviceType, propertyInfo.PropertyType, speck.ServiceLifetime, speck.Key, options, existing);
                    }
                    catch (TypeAccessException ex)
                    {
                        throw new SpeckyException($"{speck.ServiceType?.Name ?? NullServiceTypeName}.{type.Name}.{propertyInfo.Name}.{propertyInfo.PropertyType.Name}", ex);
                    }

                    options.ConfigurationAddedServiceTypes.Add(serviceType);
                }
            }
        }
    }

    internal static void ScanMethodsAndInject(this IServiceCollection serviceCollection, Type type, SpeckyOptions options, HashSet<ServiceTriple> existing)
    {
        foreach (var methodInfo in type.GetMethods(ConfigurationMemberBindingFlags))
        {
            var specks = SpeckyCaches.GetSpeckAttributes((MemberInfo)methodInfo);
            foreach (var speck in specks)
            {
                if (methodInfo.ReturnType == typeof(void))
                {
                    throw new SpeckyException($"Specky configuration methods cannot return {typeof(void).Name}. The {nameof(methodInfo.ReturnType)} must be the {nameof(Type)} you want Specky to inject.\n{speck.ServiceType?.Name ?? typeof(void).Name}.{type.Name}.{methodInfo.Name}.{nameof(methodInfo.ReturnType.Name)}");
                }

                var implementationType = methodInfo.ReturnType;
                var serviceLifetime = speck.ServiceLifetime;

                foreach (var serviceType in speck.ServiceTypes.Count == 0 ? [methodInfo.ReturnType] : speck.ServiceTypes)
                {
                    serviceCollection.AddSpeck(serviceType, implementationType, serviceLifetime, speck.Key, options, existing);
                    options.ConfigurationAddedServiceTypes.Add(serviceType);
                }
            }
        }
    }

    internal static void ScanFieldsAndInject(this IServiceCollection serviceCollection, Type type, SpeckyOptions options, HashSet<ServiceTriple> existing)
    {
        foreach (var fieldInfo in type.GetFields(ConfigurationMemberBindingFlags))
        {
            var specks = SpeckyCaches.GetSpeckAttributes((MemberInfo)fieldInfo);
            foreach (var speck in specks)
            {
                var implementationType = fieldInfo.FieldType;
                var serviceLifetime = speck.ServiceLifetime;

                foreach (var serviceType in speck.ServiceTypes.Count == 0 ? [fieldInfo.FieldType] : speck.ServiceTypes)
                {
                    try
                    {
                        serviceCollection.AddSpeck(serviceType, implementationType, serviceLifetime, speck.Key, options, existing);
                    }
                    catch (TypeAccessException ex)
                    {
                        throw new SpeckyException($"Specky could not inject service type {serviceType.Name} with implementation type {implementationType.Name} for an unknown reason.\n{speck.ServiceType?.Name ?? NullServiceTypeName}.{type.Name}.{fieldInfo.Name}.{nameof(fieldInfo.FieldType.Name)}", ex);
                    }

                    options.ConfigurationAddedServiceTypes.Add(serviceType);
                }
            }
        }
    }

    internal static void AddSpeck(this IServiceCollection serviceCollection, Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, object? serviceKey, SpeckyOptions options, HashSet<ServiceTriple> existing)
    {
        if (!implementationType.IsAssignableTo(serviceType))
        {
            throw new SpeckyException($"Specky cannot inject {implementationType.Name} type because it cannot be assigned to {serviceType.Name}.\n{serviceType.Name}.{implementationType.Name}");
        }
        if (implementationType.IsInterface)
        {
            throw new SpeckyException($"Specky cannot inject {implementationType.Name} because it is an interface.\n{serviceType.Name}.{implementationType.Name}");
        }
        if (options.ConfigurationAddedServiceTypes.Contains(serviceType)) return;
        var triple = new ServiceTriple(serviceType, implementationType, serviceLifetime, serviceKey);
        if (!existing.Add(triple)) return; // fast duplicate rejection

        if (serviceKey is null)
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
            return;
        }

        // Register keyed service
        switch (serviceLifetime)
        {
            case ServiceLifetime.Singleton:
                serviceCollection.Add(ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, implementationType));
                break;
            case ServiceLifetime.Scoped:
                serviceCollection.Add(ServiceDescriptor.KeyedScoped(serviceType, serviceKey, implementationType));
                break;
            case ServiceLifetime.Transient:
                serviceCollection.Add(ServiceDescriptor.KeyedTransient(serviceType, serviceKey, implementationType));
                break;
            default:
                serviceCollection.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
                break;
        }
    }
}

internal readonly record struct PostInitRegistration(Type ServiceType, object? ServiceKey);
