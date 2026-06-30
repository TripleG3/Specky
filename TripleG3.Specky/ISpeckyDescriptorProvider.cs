using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Specky;

/// <summary>
/// Provides explicit service descriptors for advanced Specky registration scenarios.
/// </summary>
public interface ISpeckyDescriptorProvider
{
    /// <summary>
    /// Gets the service descriptors to add to the service collection.
    /// </summary>
    /// <returns>The descriptors to add.</returns>
    IEnumerable<ServiceDescriptor> GetDescriptors();
}
