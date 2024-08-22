using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Extensions.Formatting.Test
{
    public class NullSink : ILogEventSink
    {
        private readonly ITextFormatter _formatter;
        private readonly TextWriter _textWriter;

        public NullSink(ITextFormatter formatter, TextWriter textWriter)
        {
            _formatter = formatter;
            _textWriter = textWriter;
        }

        public void Emit(LogEvent logEvent)
        {
            _formatter.Format(logEvent, _textWriter);
        }
    }
}
