using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Extensions.Formatting.Test
{
    internal static class Some
    {
        private static int Counter;

        public static int Int()
        {
            return Interlocked.Increment(ref Counter);
        }

        public static decimal Decimal()
        {
            return Int() + 0.123m;
        }

        public static string String(string tag = null)
        {
            return (tag ?? "") + "__" + Int();
        }

        public static TimeSpan TimeSpan()
        {
            return System.TimeSpan.FromMinutes(Int());
        }

        public static DateTime Instant()
        {
            return new DateTime(2012, 10, 28) + TimeSpan();
        }

        public static DateTimeOffset OffsetInstant()
        {
            return new DateTimeOffset(Instant());
        }

        public static LogEvent LogEvent(
            DateTimeOffset? timestamp = null,
            LogEventLevel level = LogEventLevel.Information,
            Exception exception = null,
            string messageTemplate = null,
            object[] propertyValues = null,
            ActivityTraceId traceId = default,
            ActivitySpanId spanId = default)
        {
            var logger = new LoggerConfiguration().CreateLogger();
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            Assert.True(logger.BindMessageTemplate(messageTemplate ?? "DEFAULT TEMPLATE", propertyValues,
                out var parsedTemplate, out var boundProperties));
            return new LogEvent(
                timestamp ?? OffsetInstant(),
                level,
                exception,
                parsedTemplate,
                boundProperties,
                traceId,
                spanId);
        }

        public static LogEvent InformationEvent(DateTimeOffset? timestamp = null)
        {
            return LogEvent(timestamp);
        }

        public static LogEvent DebugEvent(DateTimeOffset? timestamp = null)
        {
            return LogEvent(timestamp, LogEventLevel.Debug);
        }

        public static LogEvent WarningEvent(DateTimeOffset? timestamp = null)
        {
            return LogEvent(timestamp, LogEventLevel.Warning);
        }

        public static LogEventProperty LogEventProperty()
        {
            return new LogEventProperty(String(), new ScalarValue(Int()));
        }

        public static string NonexistentTempFilePath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        }

        public static string TempFilePath()
        {
            return Path.GetTempFileName();
        }

        public static string TempFolderPath()
        {
            string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static MessageTemplate MessageTemplate()
        {
            return new MessageTemplateParser().Parse(String());
        }

        public static Exception Exception()
        {
            return new ArgumentException(String());
        }
    }
}
