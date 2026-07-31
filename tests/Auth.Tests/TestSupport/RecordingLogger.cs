using Microsoft.Extensions.Logging;

namespace Auth.Tests.TestSupport;

public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception), exception));

    public bool Logged(LogLevel level, string messageFragment)
        => Entries.Any(e => e.Level == level && e.Message.Contains(messageFragment, StringComparison.Ordinal));
}
