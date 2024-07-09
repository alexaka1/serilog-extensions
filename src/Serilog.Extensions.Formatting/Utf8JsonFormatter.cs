using System.Globalization;
using System.Text;
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
    private const string NoQuotingOfStrings = "l";
    private const string DateOnlyFormat = "yyyy-MM-dd";
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
    public Utf8JsonFormatter(string? closingDelimiter = null,
        bool renderMessage = false,
        IFormatProvider? formatProvider = null,
        int spanBufferSize = 64,
        bool skipValidation = true,
        JsonNamingPolicy? namingPolicy = null)
    {
        _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();
        _names = new JsonLogPropertyNames(_namingPolicy);
        _renderMessage = renderMessage;
        _spanBufferSize = spanBufferSize;
        _closingDelimiter = closingDelimiter ?? Environment.NewLine;
        _formatProvider = formatProvider as CultureInfo ?? CultureInfo.InvariantCulture;
        _writer = new Utf8JsonWriter(Stream.Null, new JsonWriterOptions { SkipValidation = skipValidation });
    }


    /// <inheritdoc />
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
                Visit(property.Value, writer);
            }

            writer.WriteEndObject();
        }

        //
        // var tokensWithFormat = logEvent.MessageTemplate.Tokens
        //     .OfType<PropertyToken>()
        //     .Where(pt => pt.Format != null)
        //     .GroupBy(pt => pt.PropertyName)
        //     .ToArray().AsSpan();
        //
        // if (tokensWithFormat.Length != 0)
        // {
        //     writer.WriteStartObject(_names.Renderings);
        //     WriteRenderingsValues(tokensWithFormat, logEvent.Properties, writer);
        //     writer.WriteEndObject();
        // }

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

    private void Visit<TState>(TState? value, Utf8JsonWriter writer)
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

            Visit(element.Value, writer);
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
            Visit(property.Value, writer);
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
            Visit(element, writer);
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
                                format: default))
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

    private void WriteRenderingsValues(Span<IGrouping<string, PropertyToken>> tokensWithFormat,
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

        if (!pt.Alignment.HasValue)
        {
            RenderValue(propertyValue, isLiteral, isJson, output, pt.Format, formatProvider);
        }
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
            Visit(propertyValue, output);
        }
        else
        {
            Render(propertyValue, output, format, formatProvider);
        }
    }

    // these should no longer be json
    private void Render(LogEventPropertyValue? value, Utf8JsonWriter output, string? format = null,
        IFormatProvider? formatProvider = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        switch (value)
        {
            case ScalarValue sv:
                RenderScalarValue(sv, output, format, formatProvider);
                return;
            case SequenceValue seqv:
                RenderSequenceValue(seqv, output, format, formatProvider);
                return;
            case StructureValue strv:
                RenderStructureValue(strv, output, format, formatProvider);
                return;
            case DictionaryValue dictv:
                RenderDictionaryValue(dictv, output, format, formatProvider);
                return;
        }
    }

    private void RenderDictionaryValue(DictionaryValue value, Utf8JsonWriter output, string? format,
        IFormatProvider? formatProvider)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(value);
        output.WriteStartObject();
        foreach (var element in value.Elements)
        {
            if (element.Key.Value?.ToString() is { } key)
            {
                output.WritePropertyName(_namingPolicy.ConvertName(key));
            }
            else
            {
                output.WritePropertyName(_names.Null);
            }

            Render(element.Value, output, format, formatProvider);
        }
    }

    private void RenderStructureValue(StructureValue? value, Utf8JsonWriter output, string? format,
        IFormatProvider? formatProvider)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(value);
        if (value.TypeTag is not null)
        {
            output.WriteRawValue(_namingPolicy.ConvertName(value.TypeTag));
            output.WriteRawValue([' ']);
        }

        output.WriteStartObject();
        foreach (var property in value.Properties)
        {
            output.WriteRawValue(_namingPolicy.ConvertName(property.Name));
            Render(property.Value, output, format, formatProvider);
        }
    }

    private void RenderSequenceValue(SequenceValue? value, Utf8JsonWriter output, string? format,
        IFormatProvider? formatProvider)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(value);
        output.WriteStartArray();
        foreach (var element in value.Elements)
        {
            Render(element, output, format, formatProvider);
        }

        output.WriteEndArray();
    }

    private void RenderScalarValue(ScalarValue v, Utf8JsonWriter output, string? format,
        IFormatProvider? formatProvider)
    {
        object? value = v.Value;
        ArgumentNullException.ThrowIfNull(output);
        switch (value)
        {
            case null:
                output.WriteRawValue(_names.Null);
                return;
            case string s:
            {
                if (format != NoQuotingOfStrings)
                {
                    output.WriteRawValue(['"', ..s.Replace("\"", "\\\""), '"']);
                }
                else
                {
                    output.WriteRawValue(s);
                }

                return;
            }
        }

        var custom = (ICustomFormatter?)formatProvider?.GetFormat(typeof(ICustomFormatter));
        if (custom != null)
        {
            output.WriteRawValue(custom.Format(format, value, formatProvider));
            return;
        }

        if (value is IFormattable f)
        {
            output.WriteStringValue(f.ToString(format, formatProvider ?? _formatProvider));
        }
        else
        {
            output.WriteStringValue(value.ToString() ?? "null");
        }
    }
}
