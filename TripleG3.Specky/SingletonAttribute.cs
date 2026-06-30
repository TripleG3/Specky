namespace TripleG3.Specky;

/// <summary>Registers the decorated type as a singleton service of itself.</summary>
public class SingletonAttribute : SpeckAttribute { }

/// <summary>Registers the decorated type as the implementation of <typeparamref name="T"/> with singleton lifetime.</summary>
public class SingletonAttribute<T> : SpeckAttribute<T> where T : class { }

/// <summary>
/// Registers the decorated type as the implementation of <typeparamref name="T"/> with singleton lifetime and associates it with a key.
/// </summary>
/// <typeparam name="T">The service type to register.</typeparam>
/// <param name="key">The key to associate with the service registration.</param>
public class SingletonKeyedAttribute<T>(object key) : SpeckKeyedAttribute<T>(key) where T : class
{
}