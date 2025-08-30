using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Specky7;

/// <summary>
/// Internal caches to reduce reflection and allocation overhead when scanning assemblies repeatedly.
/// </summary>
internal static class SpeckyCaches
{
    private static readonly ConcurrentDictionary<MemberInfo, SpeckAttribute[]> _memberSpeckCache = new();
    private static readonly ConcurrentDictionary<Type, SpeckAttribute[]> _typeSpeckCache = new();

    public static SpeckAttribute[] GetTypeSpeckAttributes(Type type)
        => _typeSpeckCache.GetOrAdd(type, t => (SpeckAttribute[])t.GetCustomAttributes(typeof(SpeckAttribute), false));

    public static SpeckAttribute[] GetSpeckAttributes(MemberInfo member)
        => _memberSpeckCache.GetOrAdd(member, m => (SpeckAttribute[])m.GetCustomAttributes(typeof(SpeckAttribute), false));

    /// <summary>
    /// Builds a hash set of existing (service, implementation, lifetime) tuples for fast duplicate rejection.
    /// </summary>
    public static HashSet<ServiceTriple> BuildExistingDescriptorSet(IReadOnlyCollection<ServiceDescriptor> descriptors)
    {
        var set = new HashSet<ServiceTriple>(ServiceTriple.Comparer);
        foreach (var d in descriptors)
        {
            if (d.ImplementationType != null)
            {
                set.Add(new ServiceTriple(d.ServiceType, d.ImplementationType, d.Lifetime));
            }
        }
        return set;
    }
}

internal readonly record struct ServiceTriple(Type ServiceType, Type ImplementationType, ServiceLifetime Lifetime)
{
    public static IEqualityComparer<ServiceTriple> Comparer { get; } = new TripleComparer();
    private sealed class TripleComparer : IEqualityComparer<ServiceTriple>
    {
        public bool Equals(ServiceTriple x, ServiceTriple y)
            => x.ServiceType == y.ServiceType && x.ImplementationType == y.ImplementationType && x.Lifetime == y.Lifetime;
        public int GetHashCode(ServiceTriple obj)
        {
            unchecked
            {
                var h = obj.ServiceType.GetHashCode();
                h = (h * 397) ^ obj.ImplementationType.GetHashCode();
                h = (h * 397) ^ (int)obj.Lifetime;
                return h;
            }
        }
    }
}
