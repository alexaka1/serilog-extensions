using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Serilog.Context;
using Serilog.Core;
using Serilog.Enrichers.Sensitive;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Templates;

namespace Serilog.Extensions.Formatting.Benchmark
{
    [SimpleJob]
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    public class JsonFormatterEnrichBenchmark
    {
        private IEnumerable<IDisposable> _contexts;
        private Exception _exception;
        private Logger _jsonLog;

        [ParamsAllValues]
        public Formatters Formatter { get; set; }

        private static readonly DateTime s_propertyValue0 = new DateTime(1970, 1, 1);
        private static readonly dynamic s_propertyValue1 = new { B = new DateTime(2000, 1, 1), C = new[] { 1, 2, 3 } };

        private static readonly Dictionary<string, DateTime> s_propertyValue2 = new Dictionary<string, DateTime>
        {
            { "D", new DateTime(2000, 1, 1) },
            { "E", new DateTime(2000, 1, 1) },
            { "F", new DateTime(2000, 1, 1) },
        };

        private static LoggerConfiguration LoggerConfiguration()
        {
            return new LoggerConfiguration().MinimumLevel.Verbose()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMemoryUsage()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                .Enrich.WithSensitiveDataMasking(new SensitiveDataEnricherOptions())
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithProperty("HelloWorld", int.MaxValue);
        }

        [GlobalSetup]
        public void Setup()
        {
            _exception = new Exception("An Error");
            _jsonLog = LoggerConfiguration()
                .WriteTo.Sink(new NullSink(Formatter == Formatters.Json ? new JsonFormatter() :
                    Formatter == Formatters.Utf8Json ? new Utf8JsonFormatter(skipValidation: true) :
                    Formatter == Formatters.Expression ? (ITextFormatter)new ExpressionTemplate(
                        "{ {Timestamp:@t,Level:@l,MessageTemplate:@mt,RenderedMessage:@m,TraceId:@tr,SpanId:@sp,Exception:@x,Properties:@p,Renderings:@r} }\n") :
                    throw new ArgumentOutOfRangeException(nameof(Formatter), Formatter, null),
                    new StreamWriter(Stream.Null)))
                .CreateLogger();
            _contexts =
                new List<IDisposable>
                {
                    LogContext.PushProperty("HelloWorld", _exception, true),
#if FEATURE_DATE_AND_TIME_ONLY
                    LogContext.PushProperty("CurrentDate", DateOnly.FromDateTime(DateTime.Now)),
                    LogContext.PushProperty("CurrentTime", TimeOnly.FromDateTime(DateTime.Now)),
#endif
                    LogContext.PushProperty("CurrentDateTime", DateTime.Now),
                    LogContext.PushProperty("EnumValue", LogEventLevel.Fatal),
                }.AsReadOnly();
        }

        [GlobalCleanup]
        public void Dispose()
        {
            foreach (var ctx in _contexts)
            {
                ctx.Dispose();
            }
        }

        [Benchmark]
        public void EmitLogEvent()
        {
            _jsonLog.Error(_exception, "Hello, {Name}!", "World");
            _jsonLog.Information("Hello, {Name}!", "Alex");
            _jsonLog.Debug("This is a debug message");
        }

        [Benchmark]
        public void ComplexProperties()
        {
            _jsonLog.Error(_exception, "Hello, {A:D} {@B} {C}!", s_propertyValue0, s_propertyValue1,
                s_propertyValue2);
            _jsonLog.Information("The current time is, {Time:c}!", TimeSpan.MaxValue);
            _jsonLog.Debug("Hello there!");
        }

        [Benchmark]
        public void IntProperties()
        {
            _jsonLog.Error(_exception, "Hello, {A:0000} {B:0000} {C:0000}!", 1, 2, 3);
            _jsonLog.Information("The current time is, {Time}!", int.MaxValue);
            _jsonLog.Debug("Hello there!");
        }

        public enum Formatters
        {
            Json,
            Utf8Json,
            Expression,
        }
    }
}
