namespace TripleG3.Specky;

/// <summary>
/// Marks a type as a provider of explicit Specky service descriptors.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SpeckyDescriptorProviderAttribute : Attribute
{
}
