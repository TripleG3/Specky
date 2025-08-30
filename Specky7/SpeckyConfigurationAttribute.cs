namespace Specky7;

/// <summary>
/// Marks an interface as a Specky configuration contract whose members (properties / methods) declare services to register.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class SpeckyConfigurationAttribute : Attribute 
{
    /// <summary>
    /// Create a configuration attribute with no option filtering (matches empty option string).
    /// </summary>
    public SpeckyConfigurationAttribute() : this(string.Empty) { }
    /// <summary>
    /// Create a configuration attribute tied to a specific option value used for filtering.
    /// </summary>
    public SpeckyConfigurationAttribute(string option) => Option = option;
    /// <summary>
    /// Option key used to include this configuration when matching options are added via <c>AddOption</c>.
    /// </summary>
    public string Option { get; init; }
}