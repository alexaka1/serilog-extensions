using System.Globalization;
using System.Text;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;

namespace Serilog.Extensions.Formatting;

/// <summary>
///     Formats log events in a simple JSON structure using <see cref="System.Text.Json.Utf8JsonWriter" />.
///     Instances of this class are safe for concurrent access by multiple threads.
/// </summary>
/// <remarks>
///     This formatter formats using camelCase keys. For properties,
///     it simply converts the first character to lower, using the provided format provider
/// </remarks>
public class Utf8JsonFormatter : ITextFormatter
{
    private readonly int _spanBufferSize;
    private readonly string _closingDelimiter;
    private readonly CultureInfo _formatProvider;
    private readonly bool _renderMessage;
    private readonly Utf8JsonWriter _writer;
    private const string TimestampPropertyName = "Timestamp";
    private const string LevelPropertyName = "Level";
    private const string MessageTemplatePropertyName = "MessageTemplate";
    private const string RenderedMessagePropertyName = "RenderedMessage";
    private const string TraceIdPropertyName = "TraceId";
    private const string SpanIdPropertyName = "SpanId";
    private const string ExceptionPropertyName = "Exception";
    private const string PropertiesPropertyName = "Properties";
    private const string RenderingsPropertyName = "Renderings";
    private const string NullPropertyName = "null";
    private const string TypeTagPropertyName = "_typeTag";
    private const string FormatPropertyName = "Format";
    private const string RenderingPropertyName = "Rendering";
    private const string NoQuotingOfStrings = "l";
    private const string DateOnlyFormat = "yyyy-MM-dd";
    private const string TimeFormat = "O";

    /// <summary>
    ///     Formats log events in a simple JSON structure using <see cref="System.Text.Json.Utf8JsonWriter" />.
    ///     Instances of this class are safe for concurrent access by multiple threads.
    /// </summary>
    /// <remarks>
    ///     This formatter formats using camelCase keys. For properties,
    ///     it simply converts the first character to lower, using the provided format provider
    /// </remarks>
    public Utf8JsonFormatter(string? closingDelimiter = null,
        bool renderMessage = false,
        IFormatProvider? formatProvider = null,
        int spanBufferSize = 64,
        bool skipValidation = true)
    {
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
        writer.WriteString(TimestampPropertyName, logEvent.Timestamp.ToString(TimeFormat, _formatProvider));
        writer.WriteString(LevelPropertyName, Enum.GetName(logEvent.Level));
        writer.WriteString(MessageTemplatePropertyName, logEvent.MessageTemplate.Text);
        if (_renderMessage)
        {
            writer.WriteString(RenderedMessagePropertyName, logEvent.MessageTemplate.Render(logEvent.Properties));
        }

        if (logEvent.TraceId != null)
        {
            writer.WriteString(TraceIdPropertyName, logEvent.TraceId.Value.ToString());
        }

        if (logEvent.SpanId != null)
        {
            writer.WriteString(SpanIdPropertyName, logEvent.SpanId.Value.ToString());
        }

        if (logEvent.Exception != null)
        {
            writer.WriteString(ExceptionPropertyName, logEvent.Exception.ToString());
        }

        if (logEvent.Properties.Count != 0)
        {
            writer.WriteStartObject(PropertiesPropertyName);
            foreach (var property in logEvent.Properties)
            {
                writer.WritePropertyName(property.Key);
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
        //     writer.WriteStartObject(RenderingsPropertyName);
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
    public virtual Utf8JsonWriter GetWriter(Stream stream)
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
                writer.WritePropertyName(key);
            }
            else
            {
                writer.WritePropertyName(NullPropertyName);
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
            writer.WritePropertyName(property.Name);
            Visit(property.Value, writer);
        }

        if (value.TypeTag is not null)
        {
            writer.WriteString(TypeTagPropertyName, value.TypeTag);
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
                if (vt is int i)
                {
                    writer.WriteNumberValue(i);
                }
                else if (vt is uint ui)
                {
                    writer.WriteNumberValue(ui);
                }
                else if (vt is long l)
                {
                    writer.WriteNumberValue(l);
                }
                else if (vt is ulong ul)
                {
                    writer.WriteNumberValue(ul);
                }
                else if (vt is decimal dc)
                {
                    writer.WriteNumberValue(dc);
                }
                else if (vt is byte bt)
                {
                    writer.WriteNumberValue(bt);
                }
                else if (vt is sbyte sb)
                {
                    writer.WriteNumberValue(sb);
                }
                else if (vt is short s)
                {
                    writer.WriteNumberValue(s);
                }
                else if (vt is ushort us)
                {
                    writer.WriteNumberValue(us);
                }
                else if (vt is double d)
                {
                    writer.WriteNumberValue(d);
                }
                else if (vt is float f)
                {
                    writer.WriteNumberValue(f);
                }
                else if (vt is bool b)
                {
                    writer.WriteBooleanValue(b);
                }
                else if (vt is char c1)
                {
                    writer.WriteStringValue([c1]);
                }
                else if (vt is DateTime dt)
                {
                    writer.WriteStringValue(dt);
                }
                else if (vt is DateTimeOffset dto)
                {
                    writer.WriteStringValue(dto);
                }

                else if (vt is TimeSpan timeSpan)
                {
                    Span<char> buffer = stackalloc char[_spanBufferSize];
                    if (timeSpan.TryFormat(buffer, out int written, formatProvider: _formatProvider,
                            format: default))
                    {
                        writer.WriteStringValue(buffer[..written]);
                    }
                }
                else if (vt is DateOnly dateOnly)
                {
                    Span<char> buffer = stackalloc char[_spanBufferSize];
                    if (dateOnly.TryFormat(buffer, out int written, provider: _formatProvider,
                            format: DateOnlyFormat))
                    {
                        writer.WriteStringValue(buffer[..written]);
                    }
                }
                else if (vt is TimeOnly timeOnly)
                {
                    Span<char> buffer = stackalloc char[_spanBufferSize];
                    if (timeOnly.TryFormat(buffer, out int written, provider: _formatProvider,
                            format: TimeFormat))
                    {
                        writer.WriteStringValue(buffer[..written]);
                    }
                }
                else if (vt.GetType().IsEnum)
                {
                    writer.WriteStringValue(vt.ToString());
                }
                else if (vt is ISpanFormattable span)
                {
                    Span<char> buffer = stackalloc char[_spanBufferSize];
                    if (span.TryFormat(buffer, out int written, provider: _formatProvider,
                            format: default))
                    {
                        writer.WriteRawValue(buffer[..written]);
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
                writer.WriteString(FormatPropertyName, format.Format);
                writer.WritePropertyName(RenderingPropertyName);
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
                output.WritePropertyName(key);
            }
            else
            {
                output.WritePropertyName(NullPropertyName);
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
            output.WriteRawValue(value.TypeTag);
            output.WriteRawValue([' ']);
        }

        output.WriteStartObject();
        foreach (var property in value.Properties)
        {
            output.WriteRawValue(property.Name);
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
                output.WriteRawValue(NullPropertyName);
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
            output.WriteStringValue(value.ToString() ?? NullPropertyName);
        }
    }
}
