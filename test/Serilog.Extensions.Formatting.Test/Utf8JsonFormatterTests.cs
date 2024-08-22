using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;
using Serilog.Templates;
using Xunit.Abstractions;

namespace Serilog.Extensions.Formatting.Test;

public class Utf8JsonFormatterTests(ITestOutputHelper output)
{
    private readonly DateTimeOffset _dateTimeOffset = new(new DateTime(1970, 1, 1), TimeSpan.Zero);

    [Theory]
    [MemberData(nameof(LogEvents))]
    public void RendersSameJsonAsJsonFormatter(LogEvent e)
    {
        var json = new JsonFormatter(renderMessage: true);
        var utf8 = new Utf8JsonFormatter(renderMessage: true,
            // fix Unicode escaping for the tests
            jsonWriterEncoder: JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

        var jsonB = new StringWriter();
        var utf8B = new StringWriter();
        json.Format(e, jsonB);
        utf8.Format(e, utf8B);
        jsonB.Flush();
        utf8B.Flush();
        string expected = jsonB.ToString();
        string actual = utf8B.ToString();
        output.WriteLine("Json:");
        output.WriteLine(expected);
        output.WriteLine("Utf8:");
        output.WriteLine(actual);
        Assert.Equal(expected, actual);
    }

    public static TheoryData<LogEvent> LogEvents()
    {
        var p = new MessageTemplateParser();
        return new TheoryData<LogEvent>
        {
            new LogEvent(Some.OffsetInstant(), LogEventLevel.Information, null,
                p.Parse("Value: {AProperty}"),
                [
                    new LogEventProperty("AProperty", new ScalarValue(12)),
                ]),
            new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Verbose,
                new Exception("test") { Data = { ["testData"] = "test2" } },
                p.Parse(
                    "My name is {Name}, I'm {Age} years old, and I live in {City}, and the time is {Time:HH:mm:ss}"),
                [
                    new LogEventProperty("Name", new ScalarValue("John Doe")),
                    new LogEventProperty("Age", new ScalarValue(42)),
                    new LogEventProperty("City", new ScalarValue("London")),
                    new LogEventProperty("Time",
                        // DateTimes are trimmed, we test this case elsewhere
                        new ScalarValue(DateTimeOffset.Parse("2023-01-01T12:34:56.7891111+01:00"))
                    ),
                ]),
            new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Verbose,
                new Exception("test") { Data = { ["testData"] = "test2" } },
                p.Parse(
                    "My name is {Name}, I'm {Age} years old, and I live in {City}, and the time is {Time:HH:mm:ss}"),
                [
                    new LogEventProperty("Name", new ScalarValue("John Doe")),
                    new LogEventProperty("Age", new ScalarValue(42)),
                    new LogEventProperty("City", new ScalarValue("London")),
                    new LogEventProperty("Time",
                        new ScalarValue(DateTime.Parse("2023-01-01T12:34:56.7891111+01:00"))
                    ),
                ]),
            new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Debug,
                new Exception("test") { Data = { ["testData"] = "test2" } },
                p.Parse(
                    "I have {Count} fruits, which are {Fruits}"),
                [
                    new LogEventProperty("Count", new ScalarValue(3)),
                    new LogEventProperty("Fruits",
                        new SequenceValue([
                                new ScalarValue("apple"), new ScalarValue("banana"), new ScalarValue("cherry"),
                            ]
                        )
                    ),
                ]
            ),
            new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information,
                new Exception("test") { Data = { ["testData"] = "test2" } },
                p.Parse(
                    "I have {Fruit,-20} fruits"),
                [
                    new LogEventProperty("Fruit", new ScalarValue("apple")),
                ]
            ),
            new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information,
                new Exception("test") { Data = { ["testData"] = "test2" } },
                p.Parse(
                    "I have {@Fruit,-40} fruits, {Hello:u3}"),
                [
                    new LogEventProperty("Fruit", new StructureValue([
                                new LogEventProperty("apple", new ScalarValue("apple")),
                            ]
                        )
                    ),
                    new LogEventProperty("Hello", new ScalarValue("Hello World")),
                ]
            ),
        };
    }

    [Fact]
    public void CamelCase()
    {
        var formatter =
            new Utf8JsonFormatter("", true, null, 64, true, JsonNamingPolicy.CamelCase);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world {Number}", [new PropertyToken("Number", "{Number}")]),
            [
                new LogEventProperty("HelloWorld", new ScalarValue("world")),
                new LogEventProperty("Number", new ScalarValue(123)),
            ],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        Assert.Equal("""
            {"timestamp":"1970-01-01T00:00:00.0000000\u002B00:00","level":"Debug","messageTemplate":"hello world {Number}","renderedMessage":"123","traceId":"3653d3ec94d045b9850794a08a4b286f","spanId":"fcfb4c32a12a3532","properties":{"helloWorld":"world","number":123}}
            """, message);
    }

    [Fact]
    public void DoesNotThrowError()
    {
        var formatter =
            new Utf8JsonFormatter(null, true);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world {Number}", [new PropertyToken("Number", "{Number}")]),
            [
                new LogEventProperty("HelloWorld", new ScalarValue("world")),
                new LogEventProperty("Number", new ScalarValue(123)),
            ],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        writer.Flush();
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        output.WriteLine(message);
        Helpers.AssertValidJson(message);
    }

    [Fact]
    public void ExpressionTemplate()
    {
        var formatter =
            new ExpressionTemplate(
                "{ {Timestamp:@t,Level:@l,MessageTemplate:@mt,RenderedMessage:@m,TraceId:@tr,SpanId:@sp,Exception:@x,Properties:@p} }");
        var sb = new MemoryStream();
        using var writer = new StreamWriter(sb);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world {Number}", [new PropertyToken("Number", "{Number}")]),
            [
                new LogEventProperty("HelloWorld", new ScalarValue("world")),
                new LogEventProperty("Number", new ScalarValue(123)),
            ],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        writer.Flush();
        string message = Encoding.UTF8.GetString(sb.ToArray());
        output.WriteLine(message);
        Helpers.AssertValidJson(message);
    }

    [Fact]
    public void FormatTest()
    {
        var formatter = new Utf8JsonFormatter("");
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world", []), [new LogEventProperty("hello", new ScalarValue("world"))],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        writer.Flush();
        Assert.Equal("""
            {"Timestamp":"1970-01-01T00:00:00.0000000\u002B00:00","Level":"Debug","MessageTemplate":"hello world","TraceId":"3653d3ec94d045b9850794a08a4b286f","SpanId":"fcfb4c32a12a3532","Properties":{"hello":"world"}}
            """, Encoding.UTF8.GetString(stream.ToArray().AsSpan()));
    }

    [Fact]
    public void KebabCaseLower()
    {
        var formatter =
            new Utf8JsonFormatter("", true, null, 64, true, JsonNamingPolicy.KebabCaseLower);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world {Number}", [new PropertyToken("Number", "{Number}")]),
            [
                new LogEventProperty("HelloWorld", new ScalarValue("world")),
                new LogEventProperty("Number", new ScalarValue(123)),
            ],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        writer.Flush();
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        Assert.Equal("""
            {"timestamp":"1970-01-01T00:00:00.0000000\u002B00:00","level":"Debug","message-template":"hello world {Number}","rendered-message":"123","trace-id":"3653d3ec94d045b9850794a08a4b286f","span-id":"fcfb4c32a12a3532","properties":{"hello-world":"world","number":123}}
            """, message);
    }

    [Fact]
    public void SnakeCaseLower()
    {
        var formatter =
            new Utf8JsonFormatter("", true, null, 64, true, JsonNamingPolicy.SnakeCaseLower);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world {Number}", [new PropertyToken("Number", "{Number}")]),
            [
                new LogEventProperty("HelloWorld", new ScalarValue("world")),
                new LogEventProperty("Number", new ScalarValue(123)),
            ],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        writer.Flush();
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        Assert.Equal("""
            {"timestamp":"1970-01-01T00:00:00.0000000\u002B00:00","level":"Debug","message_template":"hello world {Number}","rendered_message":"123","trace_id":"3653d3ec94d045b9850794a08a4b286f","span_id":"fcfb4c32a12a3532","properties":{"hello_world":"world","number":123}}
            """, message);
    }

    [Fact]
    public void SnakeCaseUpper()
    {
        var formatter =
            new Utf8JsonFormatter("", true, null, 64, true, JsonNamingPolicy.SnakeCaseUpper);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, null,
            new MessageTemplate("hello world {Number}", [new PropertyToken("Number", "{Number}")]),
            [
                new LogEventProperty("HelloWorld", new ScalarValue("world")),
                new LogEventProperty("Number", new ScalarValue(123)),
            ],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        writer.Flush();
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        Assert.Equal("""
            {"TIMESTAMP":"1970-01-01T00:00:00.0000000\u002B00:00","LEVEL":"Debug","MESSAGE_TEMPLATE":"hello world {Number}","RENDERED_MESSAGE":"123","TRACE_ID":"3653d3ec94d045b9850794a08a4b286f","SPAN_ID":"fcfb4c32a12a3532","PROPERTIES":{"HELLO_WORLD":"world","NUMBER":123}}
            """, message);
    }

    [Fact]
    public void WithException()
    {
        var formatter =
            new Utf8JsonFormatter(null, true);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, new AggregateException(
                new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }), new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }), new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }), new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }), new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }), new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }), new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                }), new Exception("test"), new InvalidOperationException("test2", new ArgumentException("test3")
                {
                    Data = { ["test"] = "test2" },
                })),
            new MessageTemplate("hello world", []), [new LogEventProperty("hello", new ScalarValue("world"))],
            ActivityTraceId.CreateFromUtf8String("3653d3ec94d045b9850794a08a4b286f"u8),
            ActivitySpanId.CreateFromUtf8String("fcfb4c32a12a3532"u8)), writer);
        writer.Flush();
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        output.WriteLine(message);
        Helpers.AssertValidJson(message);
    }
}
