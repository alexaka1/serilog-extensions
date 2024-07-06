using System.Globalization;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Serilog.Core;
using Serilog.Enrichers.Sensitive;
using Serilog.Exceptions;

namespace Serilog.Extensions.Formatting.Benchmark;

[SimpleJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class JsonFormatterNamingBenchmark
{
    private Exception _exception = null!;
    private Logger _jsonLog = null!;

    [ParamsAllValues]
    public Namings Naming { get; set; }

    private static readonly DateTime s_propertyValue0 = new(1970, 1, 1);
    private static readonly dynamic s_propertyValue1 = new { B = new DateTime(2000, 1, 1), C = new[] { 1, 2, 3 } };

    private static readonly Dictionary<string, DateTime> s_propertyValue2 = new()
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

    private static JsonNamingPolicy? GetNamingPolicy(Namings naming)
    {
        return naming switch
        {
            Namings.CamelCase => JsonNamingPolicy.CamelCase,
            Namings.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
            Namings.KebabCase => JsonNamingPolicy.KebabCaseLower,
            Namings.None => null,
            _ => throw new ArgumentOutOfRangeException(nameof(naming), naming, null),
        };
    }

    [GlobalSetup]
    public void Setup()
    {
        _exception = new Exception("An Error");
        _jsonLog = LoggerConfiguration()
            .WriteTo.Sink(new NullSink(
                new Utf8JsonFormatter(renderMessage: true, namingPolicy: GetNamingPolicy(Naming),
                    formatProvider: new CultureInfo("en-GB")),
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
        _jsonLog.Error(_exception, "Hello, {A} {B} {C}!", 1, 2, 3);
        _jsonLog.Information("The current time is, {Time}!", int.MaxValue);
        _jsonLog.Debug("Hello there!");
    }

    [Benchmark]
    public void ComplexPropertiesUtf8()
    {
        _jsonLog.Error(_exception, "Hello, {A} {@B} {C}!", s_propertyValue0, s_propertyValue1,
            s_propertyValue2);
        _jsonLog.Information("The current time is, {Time}!", int.MaxValue);
        _jsonLog.Debug("Hello there!");
    }

    public enum Namings
    {
        CamelCase,
        SnakeCase,
        KebabCase,
        None,
    }
}
