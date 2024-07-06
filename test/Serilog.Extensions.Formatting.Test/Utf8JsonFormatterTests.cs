using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Templates;
using Xunit.Abstractions;

namespace Serilog.Extensions.Formatting.Test;

public class Utf8JsonFormatterTests
{
    public Utf8JsonFormatterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private readonly ITestOutputHelper _output;
    private readonly DateTimeOffset _dateTimeOffset = new(new DateTime(1970, 1, 1));

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
            {"timestamp":"1970-01-01T00:00:00.0000000\u002B01:00","level":"Debug","messageTemplate":"hello world {Number}","renderedMessage":"123","traceId":"3653d3ec94d045b9850794a08a4b286f","spanId":"fcfb4c32a12a3532","properties":{"helloWorld":"world","number":123}}
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
        _output.WriteLine(message);
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
        _output.WriteLine(message);
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
            {"Timestamp":"1970-01-01T00:00:00.0000000\u002B01:00","Level":"Debug","MessageTemplate":"hello world","TraceId":"3653d3ec94d045b9850794a08a4b286f","SpanId":"fcfb4c32a12a3532","Properties":{"hello":"world"}}
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
            {"timestamp":"1970-01-01T00:00:00.0000000\u002B01:00","level":"Debug","message-template":"hello world {Number}","rendered-message":"123","trace-id":"3653d3ec94d045b9850794a08a4b286f","span-id":"fcfb4c32a12a3532","properties":{"hello-world":"world","number":123}}
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
            {"timestamp":"1970-01-01T00:00:00.0000000\u002B01:00","level":"Debug","message_template":"hello world {Number}","rendered_message":"123","trace_id":"3653d3ec94d045b9850794a08a4b286f","span_id":"fcfb4c32a12a3532","properties":{"hello_world":"world","number":123}}
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
            {"TIMESTAMP":"1970-01-01T00:00:00.0000000\u002B01:00","LEVEL":"Debug","MESSAGE_TEMPLATE":"hello world {Number}","RENDERED_MESSAGE":"123","TRACE_ID":"3653d3ec94d045b9850794a08a4b286f","SPAN_ID":"fcfb4c32a12a3532","PROPERTIES":{"HELLO_WORLD":"world","NUMBER":123}}
            """, message);
    }

    [Fact]
    public void WithException()
    {
        var formatter =
            new Utf8JsonFormatter(null, true);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        formatter.Format(new LogEvent(_dateTimeOffset, LogEventLevel.Debug, new AggregateException([
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
        writer.Flush();
        string message = Encoding.UTF8.GetString(stream.ToArray().AsSpan());
        _output.WriteLine(message);
        Helpers.AssertValidJson(message);
    }
}
