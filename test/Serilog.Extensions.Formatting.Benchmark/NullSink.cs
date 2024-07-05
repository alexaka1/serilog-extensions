using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Extensions.Formatting.Benchmark;

public class NullSink(ITextFormatter formatter, TextWriter textWriter) : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        formatter.Format(logEvent, textWriter);
    }
}
