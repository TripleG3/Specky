using System.Reflection;

namespace Specky7;

/// <summary>
/// Configuration container for a single Specky registration invocation.
/// </summary>
public class SpeckyOptions
{
    internal HashSet<Type> Configurations { get; } = [];
    internal HashSet<string> Options { get; } = new(StringComparer.Ordinal);
    internal HashSet<Assembly> Assemblies { get; } = [];
    /// <summary>
    /// When true, only configuration interfaces will be used (no attribute scanning of classes).
    /// </summary>
    public bool UseConfigurationsOnly { get; set; }
    internal HashSet<Type> ConfigurationAddedServiceTypes { get; } = [];

    private static readonly Type SpeckyConfigurationAttributeType = typeof(SpeckyConfigurationAttribute);

    /// <summary>
    /// Adds a configuration interface type marked with <see cref="SpeckyConfigurationAttribute"/>.
    /// </summary>
    public SpeckyOptions AddConfiguration(Type configurationType)
    {
        ArgumentNullException.ThrowIfNull(configurationType);

        if (!configurationType.IsInterface)
        {
            throw new SpeckyException($"{configurationType.Name} must be an interface to be added as a speck configuration.\n{nameof(AddConfiguration)}({configurationType.Name})");
        }

        if (!configurationType.IsDefined(SpeckyConfigurationAttributeType, false))
        {
            throw new SpeckyException($"{configurationType.Name} must have the {nameof(SpeckyConfigurationAttribute)} to be used as a speck configuration interface.\n{nameof(AddConfiguration)}({configurationType.Name})");
        }

        if (!Configurations.Add(configurationType))
        {
            throw new SpeckyException($"{configurationType.Name} was already added to the configuration interfaces.\n{nameof(AddConfiguration)}({configurationType.Name})");
        }

        return this;
    }

    /// <summary>
    /// Adds a configuration interface type marked with <see cref="SpeckyConfigurationAttribute"/>.
    /// </summary>
    public SpeckyOptions AddConfiguration<T>() => AddConfiguration(typeof(T));

    /// <summary>
    /// Adds an option string used to filter configuration interfaces by <see cref="SpeckyConfigurationAttribute.Option"/>.
    /// </summary>
    public SpeckyOptions AddOption(string option)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(option);

        if (!Options.Add(option))
        {
            throw new SpeckyException($"{option} was already added to the configuration options.\n{nameof(AddOption)}");
        }

        return this;
    }

    /// <summary>
    /// Adds the assembly of the generic type parameter to scan.
    /// </summary>
    public SpeckyOptions AddAssembly<T>() => AddAssemblies(typeof(T).Assembly);

    /// <summary>
    /// Adds assemblies to scan.
    /// </summary>
    public SpeckyOptions AddAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies.AsSpan())
        {
            ArgumentNullException.ThrowIfNull(assembly);

            if (!Assemblies.Add(assembly))
            {
                throw new SpeckyException($"{assembly.GetName()} was already added to the configuration assemblies.\n{nameof(AddAssemblies)}");
            }
        }

        return this;
    }

    internal void Clear()
    {
        Configurations.Clear();
        Options.Clear();
        Assemblies.Clear();
        ConfigurationAddedServiceTypes.Clear();
        UseConfigurationsOnly = false;
    }

    internal void AddConfigurations(ReadOnlySpan<Type> speckyConfigurationTypes)
    {
        foreach (var type in speckyConfigurationTypes)
        {
            if (type is not null)
            {
                _ = Configurations.Add(type);
            }
        }
    }
}