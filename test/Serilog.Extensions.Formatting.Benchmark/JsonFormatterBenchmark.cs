using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Serilog.Core;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Templates;

namespace Serilog.Extensions.Formatting.Benchmark
{
    [SimpleJob]
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    public class JsonFormatterBenchmark
    {
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

        [GlobalSetup]
        public void Setup()
        {
            _exception = new Exception("An Error");
            _jsonLog = new LoggerConfiguration().MinimumLevel.Verbose()
                .WriteTo.Sink(new NullSink(
                    Formatter == Formatters.Json ? new JsonFormatter() :
                    Formatter == Formatters.Utf8Json ? new Utf8JsonFormatter(skipValidation: true) :
                    Formatter == Formatters.Expression ? (ITextFormatter)new ExpressionTemplate(
                        "{ {Timestamp:@t,Level:@l,MessageTemplate:@mt,RenderedMessage:@m,TraceId:@tr,SpanId:@sp,Exception:@x,Properties:@p,Renderings:@r} }\n") :
                    throw new ArgumentOutOfRangeException(nameof(Formatter), Formatter, null),
                    new StreamWriter(Stream.Null)))
                .CreateLogger();
        }

        [Benchmark]
        public void EmitLogEvent()
        {
            _jsonLog.Error(_exception, "Hello, {Name}!", "World");
            _jsonLog.Information("Hello, {Name}!", "Alex");
            _jsonLog.Debug("This is a debug message");
        }

        [Benchmark]
        public void IntProperties()
        {
            _jsonLog.Error(_exception, "Hello, {A:0000} {B:0000} {C:0000}!", 1, 2, 3);
            _jsonLog.Information("The current time is, {Time}!", int.MaxValue);
            _jsonLog.Debug("Hello there!");
        }

        [Benchmark]
        public void ComplexProperties()
        {
            _jsonLog.Error(_exception, "Hello, {A:D} {@B} {C}!", s_propertyValue0, s_propertyValue1,
                s_propertyValue2);
            _jsonLog.Information("The current time is, {Time:c}!", TimeSpan.MaxValue);
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
