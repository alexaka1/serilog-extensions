using Serilog.Core;
using Serilog.Events;

namespace Serilog.Extensions.Formatting.Test;

public class DelegatingSink : ILogEventSink
{
    private readonly Action<LogEvent> _write;

    public DelegatingSink(Action<LogEvent> write)
    {
        ArgumentNullException.ThrowIfNull(write);
        _write = write;
    }

    public void Emit(LogEvent logEvent)
    {
        _write(logEvent);
    }

    public static LogEvent GetLogEvent(Action<ILogger> writeAction,
        Func<LoggerConfiguration, LoggerConfiguration>? configure = null)
    {
        LogEvent? result = null;
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new DelegatingSink(le => result = le));

        if (configure != null)
        {
            configuration = configure(configuration);
        }

        var l = configuration.CreateLogger();

        writeAction(l);
        Assert.NotNull(result);
        return result!;
    }
}
