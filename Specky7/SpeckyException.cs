namespace Specky7;

/// <summary>
/// Represents errors thrown by Specky during service registration.
/// </summary>
public class SpeckyException : InvalidOperationException
{
    /// <summary>
    /// Creates a new <see cref="SpeckyException"/>.
    /// </summary>
    public SpeckyException(string message) : base(message) { }

    /// <summary>
    /// Creates a new <see cref="SpeckyException"/> with an inner exception.
    /// </summary>
    public SpeckyException(string message, Exception inner) : base(message, inner) { }
}
