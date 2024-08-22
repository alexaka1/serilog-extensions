using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;

namespace Serilog.Extensions.Formatting;

/// <summary>
///     Formats log events in a simple JSON structure using <see cref="System.Text.Json.Utf8JsonWriter" />.
/// </summary>
public class Utf8JsonFormatter : ITextFormatter
{
    private readonly string _closingDelimiter;
    private readonly CultureInfo _formatProvider;
    private readonly JsonLogPropertyNames _names;
    private readonly JsonNamingPolicy _namingPolicy;
    private readonly bool _renderMessage;
    private readonly int _spanBufferSize;
    private readonly Utf8JsonWriter _writer;
    private const string DateOnlyFormat = "O";
    private const string TimeFormat = "O";

    /// <summary>
    ///     Formats log events in a simple JSON structure using <see cref="System.Text.Json.Utf8JsonWriter" />.
    /// </summary>
    /// <param name="closingDelimiter">
    ///     A string that will be written after each log event is formatted.
    ///     If null, <see cref="Environment.NewLine" /> will be used.
    /// </param>
    /// <param name="renderMessage">
    ///     If <see langword="true" />, the message will be rendered and written to the output as a
    ///     property named RenderedMessage.
    /// </param>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
    /// <param name="spanBufferSize">
    ///     Buffer size for the <see cref="ISpanFormattable" /> property values that are not already
    ///     handled by Utf8JsonWriter.
    /// </param>
    /// <param name="skipValidation">
    ///     Set to <see langword="false" /> to enable validation of the JSON output by the underlying
    ///     <see cref="Utf8JsonWriter" />.
    /// </param>
    /// <param name="namingPolicy">Naming policy to use for the JSON output.</param>
    /// <param name="jsonWriterEncoder">
    ///     Use <see cref="JavaScriptEncoder" /> for escaping
    ///     characters. See more:
    ///     https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding
    /// </param>
    public Utf8JsonFormatter(string? closingDelimiter = null,
        bool renderMessage = false,
        IFormatProvider? formatProvider = null,
        int spanBufferSize = 64,
        bool skipValidation = true,
        JsonNamingPolicy? namingPolicy = null,
        JavaScriptEncoder? jsonWriterEncoder = null)
    {
        _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();
        _names = new JsonLogPropertyNames(_namingPolicy);
        _renderMessage = renderMessage;
        _spanBufferSize = spanBufferSize;
        _closingDelimiter = closingDelimiter ?? Environment.NewLine;
        _formatProvider = formatProvider as CultureInfo ?? CultureInfo.InvariantCulture;
        _writer = new Utf8JsonWriter(Stream.Null,
            new JsonWriterOptions
            {
                SkipValidation = skipValidation,
                Encoder = jsonWriterEncoder,
            });
    }


    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">When <paramref name="logEvent" /> is <c>null</c></exception>
    /// <exception cref="ArgumentNullException">When <paramref name="output" /> is <c>null</c></exception>
    public void Format(LogEvent? logEvent, TextWriter? output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);
        Stream str;
        if (output is StreamWriter streamWriter)
        {
            str = streamWriter.BaseStream;
        }
        else
        {
            str = new MemoryStream();
        }

        var writer = GetWriter(str);
        writer.WriteStartObject();
        writer.WriteString(_names.Timestamp, logEvent.Timestamp.ToString(TimeFormat, _formatProvider));
        writer.WriteString(_names.Level, Enum.GetName(logEvent.Level));
        writer.WriteString(_names.MessageTemplate, logEvent.MessageTemplate.Text);
        if (_renderMessage)
        {
            writer.WriteString(_names.RenderedMessage,
                logEvent.MessageTemplate.Render(logEvent.Properties, _formatProvider));
        }

        if (logEvent.TraceId.HasValue)
        {
            writer.WriteString(_names.TraceId, logEvent.TraceId.Value.ToString());
        }

        if (logEvent.SpanId.HasValue)
        {
            writer.WriteString(_names.SpanId, logEvent.SpanId.Value.ToString());
        }

        if (logEvent.Exception != null)
        {
            writer.WriteString(_names.Exception, logEvent.Exception.ToString());
        }

        if (logEvent.Properties.Count != 0)
        {
            writer.WriteStartObject(_names.Properties);
            foreach (var property in logEvent.Properties)
            {
                writer.WritePropertyName(_namingPolicy.ConvertName(property.Key));
                Format(property.Value, writer);
            }

            writer.WriteEndObject();
        }


        var tokensWithFormat = logEvent.MessageTemplate.Tokens
            .OfType<PropertyToken>()
            .Where(pt => pt.Format != null)
            .GroupBy(pt => pt.PropertyName)
            .ToArray();

        if (tokensWithFormat.Length != 0)
        {
            writer.WriteStartObject(_names.Renderings);
            WriteRenderingsValues(tokensWithFormat, logEvent.Properties, writer);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();
        if (output is not StreamWriter && str is MemoryStream mem)
        {
            // if we used memory stream, we wrote to the memory stream, so we need to write to the output manually
            using (mem)
            {
                output.Write(Encoding.UTF8.GetString(mem.ToArray()).AsSpan());
            }
        }

        output.Write(_closingDelimiter);
    }

    /// <summary>
    ///     Sets the stream of the <see cref="Utf8JsonWriter" /> instance.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <returns>The <see cref="Utf8JsonWriter" /> instance.</returns>
    private Utf8JsonWriter GetWriter(Stream stream)
    {
        _writer.Reset(stream);
        return _writer;
    }

    private void Format<TState>(TState? value, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        switch (value)
        {
            case ScalarValue sv:
                VisitScalarValue(sv, writer);
                return;
            case SequenceValue seqv:
                VisitSequenceValue(seqv, writer);
                return;
            case StructureValue strv:
                VisitStructureValue(strv, writer);
                return;
            case DictionaryValue dictv:
                VisitDictionaryValue(dictv, writer);
                return;
            default:
                throw new NotSupportedException($"The value {value} is not of a type supported by this visitor.");
        }
    }

    private void VisitDictionaryValue(DictionaryValue? value, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();
        foreach (var element in value.Elements)
        {
            if (element.Key.Value?.ToString() is { } key)
            {
                writer.WritePropertyName(_namingPolicy.ConvertName(key));
            }
            else
            {
                writer.WritePropertyName(_names.Null);
            }

            Format(element.Value, writer);
        }

        writer.WriteEndObject();
    }

    private void VisitStructureValue(StructureValue? value, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();
        foreach (var property in value.Properties)
        {
            writer.WritePropertyName(_namingPolicy.ConvertName(property.Name));
            Format(property.Value, writer);
        }

        if (value.TypeTag is not null)
        {
            writer.WriteString(_names.TypeTag, value.TypeTag);
        }

        writer.WriteEndObject();
    }

    private void VisitSequenceValue(SequenceValue? value, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartArray();
        foreach (var element in value.Elements)
        {
            Format(element, writer);
        }

        writer.WriteEndArray();
    }

    private void VisitScalarValue(ScalarValue? value, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(value);
        switch (value.Value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string str:
                writer.WriteStringValue(str);
                break;
            case ValueType vt:
                switch (vt)
                {
                    case int i:
                        writer.WriteNumberValue(i);
                        break;
                    case uint ui:
                        writer.WriteNumberValue(ui);
                        break;
                    case long l:
                        writer.WriteNumberValue(l);
                        break;
                    case ulong ul:
                        writer.WriteNumberValue(ul);
                        break;
                    case decimal dc:
                        writer.WriteNumberValue(dc);
                        break;
                    case byte bt:
                        writer.WriteNumberValue(bt);
                        break;
                    case sbyte sb:
                        writer.WriteNumberValue(sb);
                        break;
                    case short s:
                        writer.WriteNumberValue(s);
                        break;
                    case ushort us:
                        writer.WriteNumberValue(us);
                        break;
                    case double d:
                        writer.WriteNumberValue(d);
                        break;
                    case float f:
                        writer.WriteNumberValue(f);
                        break;
                    case bool b:
                        writer.WriteBooleanValue(b);
                        break;
                    case char c:
                        writer.WriteStringValue([c]);
                        break;
                    case DateTime dt:
                        writer.WriteStringValue(dt);
                        break;
                    case DateTimeOffset dto:
                        writer.WriteStringValue(dto);
                        break;
                    case TimeSpan timeSpan:
                    {
                        Span<char> buffer = stackalloc char[_spanBufferSize];
                        if (timeSpan.TryFormat(buffer, out int written, formatProvider: _formatProvider,
                                format: "c"))
                        {
                            writer.WriteStringValue(buffer[..written]);
                        }

                        break;
                    }
                    case DateOnly dateOnly:
                    {
                        Span<char> buffer = stackalloc char[_spanBufferSize];
                        if (dateOnly.TryFormat(buffer, out int written, provider: _formatProvider,
                                format: DateOnlyFormat))
                        {
                            writer.WriteStringValue(buffer[..written]);
                        }

                        break;
                    }
                    case TimeOnly timeOnly:
                    {
                        Span<char> buffer = stackalloc char[_spanBufferSize];
                        if (timeOnly.TryFormat(buffer, out int written, provider: _formatProvider,
                                format: TimeFormat))
                        {
                            writer.WriteStringValue(buffer[..written]);
                        }

                        break;
                    }
                    case Guid guid:
                    {
                        writer.WriteStringValue(guid);
                        break;
                    }
                    default:
                    {
                        if (vt.GetType().IsEnum)
                        {
                            writer.WriteStringValue(vt.ToString());
                        }
                        else if (vt is ISpanFormattable span)
                        {
                            Span<char> buffer = stackalloc char[_spanBufferSize];
                            if (span.TryFormat(buffer, out int written, provider: _formatProvider,
                                    format: default))
                            {
                                // fallback to string
                                writer.WriteStringValue(buffer[..written]);
                            }
                        }

                        break;
                    }
                }

                break;
            default:
                writer.WriteStringValue(value.Value?.ToString());
                break;
        }
    }

    private void WriteRenderingsValues(ReadOnlySpan<IGrouping<string, PropertyToken>> tokensWithFormat,
        IReadOnlyDictionary<string, LogEventPropertyValue> properties, Utf8JsonWriter writer)
    {
        foreach (var propertyFormats in tokensWithFormat)
        {
            writer.WriteStartArray(propertyFormats.Key);
            foreach (var format in propertyFormats)
            {
                writer.WriteStartObject();
                writer.WriteString(_names.Format, format.Format);
                writer.WritePropertyName(_names.Rendering);
                RenderPropertyToken(format, properties, writer, _formatProvider, true, false);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }

    private void RenderPropertyToken(PropertyToken pt, IReadOnlyDictionary<string, LogEventPropertyValue> properties,
        Utf8JsonWriter output, IFormatProvider formatProvider, bool isLiteral, bool isJson)
    {
        if (!properties.TryGetValue(pt.PropertyName, out var propertyValue))
        {
            output.WriteStringValue(pt.ToString());
            return;
        }

        RenderValue(propertyValue, isLiteral, isJson, output, pt.Format, formatProvider);
    }

    private void RenderValue(LogEventPropertyValue propertyValue, bool literal, bool json, Utf8JsonWriter output,
        string? format, IFormatProvider formatProvider)
    {
        if (literal && propertyValue is ScalarValue { Value: string str })
        {
            output.WriteStringValue(str);
        }
        else if (json && format == null)
        {
            Format(propertyValue, output);
        }
        else
        {
            // todo: optimize
            using var writer = new StringWriter();
            propertyValue.Render(writer, format, formatProvider);
            writer.Flush();
            output.WriteStringValue(writer.ToString());
        }
    }
}
