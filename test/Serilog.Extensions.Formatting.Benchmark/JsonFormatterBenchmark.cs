using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Serilog.Core;
using Serilog.Formatting.Json;
using Serilog.Templates;

namespace Serilog.Extensions.Formatting.Benchmark;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class JsonFormatterBenchmark
{
    private Exception _exception = null!;
    private Logger _jsonLog = null!;

    [ParamsAllValues]
    public Formatters Formatter { get; set; }

    private static readonly DateTime s_propertyValue0 = new(1970, 1, 1);
    private static readonly dynamic s_propertyValue1 = new { B = new DateTime(2000, 1, 1), C = new[] { 1, 2, 3 } };

    private static readonly Dictionary<string, DateTime> s_propertyValue2 = new()
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
            .WriteTo.Sink(new NullSink(Formatter switch
            {
                Formatters.Json => new JsonFormatter(),
                Formatters.Utf8Json => new Utf8JsonFormatter(skipValidation: true),
                Formatters.Expression => new ExpressionTemplate("""
                    { {Timestamp:@t,Level:@l,MessageTemplate:@mt,RenderedMessage:@m,TraceId:@tr,SpanId:@sp,Exception:@x,Properties:@p} }

                    """),
                _ => throw new ArgumentOutOfRangeException(nameof(Formatter), Formatter, null),
            }, new StreamWriter(Stream.Null)))
            .CreateLogger();
    }

    [BenchmarkCategory("EmitLogEvent")]
    [Benchmark]
    public void EmitLogEvent()
    {
        _jsonLog.Information(_exception, "Hello, {Name}!", "World");
    }

    [BenchmarkCategory("IntProperties")]
    [Benchmark]
    public void IntProperties()
    {
        _jsonLog.Information(_exception, "Hello, {A} {B} {C}!", 1, 2, 3);
    }

    [BenchmarkCategory("ComplexProperties")]
    [Benchmark]
    public void ComplexProperties()
    {
        _jsonLog.Information(_exception, "Hello, {A} {@B} {C}!", s_propertyValue0, s_propertyValue1, s_propertyValue2);
    }

    public enum Formatters
    {
        Json,
        Utf8Json,
        Expression,
    }
}
