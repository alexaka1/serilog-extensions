using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Moq;
using Serilog.Events;
using Serilog.Parsing;
using Xunit.Abstractions;

namespace Serilog.Extensions.Formatting.Test;

public class Utf8JsonFormatterTest
{
    public Utf8JsonFormatterTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private readonly ITestOutputHelper _output;
    private readonly DateTimeOffset _dateTimeOffset = new(new DateTime(1970, 1, 1));

    [Fact]
    public void DoesNotThrowError()
    {
        var formatter =
            new Mock<Utf8JsonFormatter>(() => new Utf8JsonFormatter(null, true, null, 64))
            {
                CallBase = true,
            };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Setup(f => f.CreateWriter(It.IsAny<Stream>(), It.IsAny<JsonWriterOptions>())).Returns(
            new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                SkipValidation = false,
            }));
        formatter.Object.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world {Number}", [new PropertyToken("Number", "{Number}")]),
            [
                new LogEventProperty("hello", new ScalarValue("world")),
                new LogEventProperty("Number", new ScalarValue(123)),
            ],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        _output.WriteLine(message);
        Helpers.AssertValidJson(message);
    }

    [Fact]
    public void FormatTest()
    {
        var formatter =
            new Mock<Utf8JsonFormatter>(() => new Utf8JsonFormatter(null, false, null, 64))
            {
                CallBase = true,
            };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Setup(f => f.CreateWriter(It.IsAny<Stream>(), It.IsAny<JsonWriterOptions>())).Returns(
            new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                SkipValidation = true,
            }));
        formatter.Object.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world", []), [new LogEventProperty("hello", new ScalarValue("world"))],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        Assert.Equal("""
            {"timestamp":"1970-01-01T00:00:00.0000000\u002B01:00","level":"Debug","messageTemplate":"hello world","traceId":"3653d3ec94d045b9850794a08a4b286f","spanId":"fcfb4c32a12a3532","properties":{"hello":"world"}}
            """, Encoding.UTF8.GetString(stream.ToArray().AsSpan()));
    }

    [Fact]
    public void WithException()
    {
        var formatter =
            new Mock<Utf8JsonFormatter>(() => new Utf8JsonFormatter(null, false, null, 64))
            {
                CallBase = true,
            };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Setup(f => f.CreateWriter(It.IsAny<Stream>(), It.IsAny<JsonWriterOptions>())).Returns(
            new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                SkipValidation = false,
            }));
        formatter.Object.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, new AggregateException([
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }),
            ]),
            new MessageTemplate("hello world", []), [new LogEventProperty("hello", new ScalarValue("world"))],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        _output.WriteLine(message);
        Helpers.AssertValidJson(message);
    }
}
