using System.Reflection;

namespace Specky7;

/// <summary>
/// Configuration container for a single Specky registration invocation.
/// </summary>
public class SpeckyOptions
{
    internal HashSet<Type> Configurations { get; } = new();
    internal HashSet<string> Options { get; } = new();
    internal HashSet<Assembly> Assemblies { get; } = new();
    /// <summary>
    /// When true, only configuration interfaces will be used (no attribute scanning of classes).
    /// </summary>
    public bool UseConfigurationsOnly { get; set; }
    internal HashSet<Type> ConfigurationAddedServiceTypes { get; } = new();

    /// <summary>
    /// Adds a configuration interface type marked with <see cref="SpeckyConfigurationAttribute"/>.
    /// </summary>
    public SpeckyOptions AddConfiguration<T>()
    {
        if (typeof(T).IsInterface)
        {
            if (typeof(T).GetCustomAttributes(typeof(SpeckyConfigurationAttribute), false).Length == 0)
            {
                throw new SpeckyException($"{typeof(T).Name} must have the {nameof(SpeckyConfigurationAttribute)} to be used as a speck configuration interface.\n{nameof(AddConfiguration)}<{typeof(T).Name}>");
            }
            if (Configurations.Contains(typeof(T)))
            {
                throw new SpeckyException($"{typeof(T).Name} was already added to the configuration interfaces.\n{nameof(AddConfiguration)}<{typeof(T).Name}>");
            }
            Configurations.Add(typeof(T));
            return this;
        }
        throw new SpeckyException($"{typeof(T).Name} must be an interface to be added as a speck configuration.\n{nameof(AddConfiguration)}<{typeof(T).Name}>");
    }
    /// <summary>
    /// Adds an option string used to filter configuration interfaces by <see cref="SpeckyConfigurationAttribute.Option"/>.
    /// </summary>
    public SpeckyOptions AddOption(string option)
    {
        if (Options.Contains(option))
        {
            throw new SpeckyException($"{option} was already added to the configuration options.\n{nameof(AddOption)}");
        }
        Options.Add(option);
        return this;
    }
    /// <summary>
    /// Adds the assembly of the generic type parameter to scan.
    /// </summary>
    public SpeckyOptions AddAssembly<T>() => AddAssemblies(new[] {  typeof(T).Assembly });
    /// <summary>
    /// Adds assemblies to scan.
    /// </summary>
    public SpeckyOptions AddAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies.AsSpan())
        {
            if (Assemblies.Contains(assembly))
            {
                throw new SpeckyException($"{assembly.GetName()} was already added to the configuration assemblies.\n{nameof(AddAssemblies)}");
            }
            Assemblies.Add(assembly);
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

    internal void AddConfigurations(Span<Type> speckyConfigurationTypes)
    {
        foreach (var type in speckyConfigurationTypes) Configurations.Add(type);
    }
}