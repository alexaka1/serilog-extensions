using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;

namespace Serilog.Extensions.Formatting
{
    /// <summary>
    ///     Formats log events in a simple JSON structure using <see cref="System.Text.Json.Utf8JsonWriter" />.
    /// </summary>
    public sealed class Utf8JsonFormatter : ITextFormatter, IDisposable
    {
        private readonly string _closingDelimiter;
        private readonly CultureInfo _formatProvider;
        private readonly JsonLogPropertyNames _names;
        private readonly JsonNamingPolicy _namingPolicy;
        private readonly bool _renderMessage;
        private readonly ThreadLocal<StringBuilder> _sb;

        // ReSharper disable once NotAccessedField.Local
        private readonly int _spanBufferSize;
        private readonly ThreadLocal<StringWriter> _sw;
        private readonly ThreadLocal<Utf8JsonWriter> _writer;
        private Utf8JsonWriter Writer => _writer.Value;
        private const string TimeFormat = "O";
        private const string TimeSpanFormat = "c";
#if FEATURE_DATE_AND_TIME_ONLY
        private const string DateOnlyFormat = "O";
#endif

#pragma warning disable CS1574, CS1584, CS1581, CS1580
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
#pragma warning restore CS1574, CS1584, CS1581, CS1580
        public Utf8JsonFormatter(string closingDelimiter = null,
            bool renderMessage = false,
            IFormatProvider formatProvider = null,
            int spanBufferSize = 64,
            bool skipValidation = true,
            JsonNamingPolicy namingPolicy = null,
            JavaScriptEncoder jsonWriterEncoder = null)
        {
            _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();
            _names = new JsonLogPropertyNames(_namingPolicy);
            _renderMessage = renderMessage;
            _spanBufferSize = spanBufferSize;
            _closingDelimiter = closingDelimiter ?? Environment.NewLine;
            _formatProvider = formatProvider as CultureInfo ?? CultureInfo.InvariantCulture;
            var jsonWriterOptions = new JsonWriterOptions
            {
                SkipValidation = skipValidation,
                Encoder = jsonWriterEncoder,
            };
            _writer = new ThreadLocal<Utf8JsonWriter>(() => new Utf8JsonWriter(Stream.Null, jsonWriterOptions));
            _sb = new ThreadLocal<StringBuilder>(() => new StringBuilder());
            _sw = new ThreadLocal<StringWriter>(() => new StringWriter(_sb.Value));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">When <paramref name="logEvent" /> is <c>null</c></exception>
        /// <exception cref="ArgumentNullException">When <paramref name="output" /> is <c>null</c></exception>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

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
            writer.WriteString(_names.Level, Enum.GetName(typeof(LogEventLevel), logEvent.Level));
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
                WriteRenderingsObject(tokensWithFormat, logEvent.Properties, writer);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.Flush();
            if (!(output is StreamWriter) && str is MemoryStream mem)
            {
                // if we used memory stream, we wrote to the memory stream, so we need to write to the output manually
                using (mem)
                {
#if NET6_0_OR_GREATER
                    output.Write(Encoding.UTF8.GetString(mem.ToArray()).AsSpan());
#else
                    output.Write(Encoding.UTF8.GetString(mem.ToArray()));
#endif
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
            Writer.Reset(stream);
            return Writer;
        }

        private void Format<TState>(TState value, Utf8JsonWriter writer) where TState : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            switch (value)
            {
                case ScalarValue scalarValue:
                    VisitScalarValue(scalarValue, writer);
                    return;
                case SequenceValue sequenceValue:
                    VisitSequenceValue(sequenceValue, writer);
                    return;
                case StructureValue structureValue:
                    VisitStructureValue(structureValue, writer);
                    return;
                case DictionaryValue dictionaryValue:
                    VisitDictionaryValue(dictionaryValue, writer);
                    return;
                default:
                    throw new NotSupportedException($"The value {value} is not of a type supported by this visitor.");
            }
        }

        private void VisitDictionaryValue(DictionaryValue dictionary, Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            foreach (var element in dictionary.Elements)
            {
                string key = element.Key.Value?.ToString();
                writer.WritePropertyName(key != null ? _namingPolicy.ConvertName(key) : _names.Null);
                Format(element.Value, writer);
            }

            writer.WriteEndObject();
        }

        private void VisitStructureValue(StructureValue structure, Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            foreach (var property in structure.Properties)
            {
                writer.WritePropertyName(_namingPolicy.ConvertName(property.Name));
                Format(property.Value, writer);
            }

            if (structure.TypeTag != null)
            {
                writer.WriteString(_names.TypeTag, structure.TypeTag);
            }

            writer.WriteEndObject();
        }

        private void VisitSequenceValue(SequenceValue sequence, Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var element in sequence.Elements)
            {
                Format(element, writer);
            }

            writer.WriteEndArray();
        }

        private void VisitScalarValue(ScalarValue value, Utf8JsonWriter writer)
        {
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
// #if NET8_0_OR_GREATER
//                             _writer.WriteStringValue([c]);
// #else
//                         _writer.WriteStringValue(new[] { c });
// #endif
                            writer.WriteStringValue(new[] { c });
                            break;
                        case DateTime dt:
                            writer.WriteStringValue(dt);
                            break;
                        case DateTimeOffset dto:
                            writer.WriteStringValue(dto);
                            break;
                        case TimeSpan timeSpan:
                        {
#if FEATURE_IUTF8SPANFORMATTABLE
                            Span<byte> buffer = stackalloc byte[_spanBufferSize];
                            if (timeSpan.TryFormat(buffer, out int written, formatProvider: _formatProvider,
                                    format: TimeSpanFormat))
                            {
                                // fallback to string
                                writer.WriteStringValue(buffer.Slice(0, written));
                            }
#elif FEATURE_ISPANFORMATTABLE
                            Span<char> buffer = stackalloc char[_spanBufferSize];
                            if (timeSpan.TryFormat(buffer, out int written, formatProvider: _formatProvider,
                                    format: TimeSpanFormat))
                            {
                                writer.WriteStringValue(buffer.Slice(0, written));
                            }
#else
                            writer.WriteStringValue(timeSpan.ToString(TimeSpanFormat, _formatProvider));
#endif

                            break;
                        }
#if FEATURE_DATE_AND_TIME_ONLY
                        case DateOnly dateOnly:
                        {
                            Span<char> buffer = stackalloc char[_spanBufferSize];
                            if (dateOnly.TryFormat(buffer, out int written, provider: _formatProvider,
                                    format: DateOnlyFormat))
                            {
                                writer.WriteStringValue(buffer.Slice(0, written));
                            }

                            break;
                        }
                        case TimeOnly timeOnly:
                        {
                            Span<char> buffer = stackalloc char[_spanBufferSize];
                            if (timeOnly.TryFormat(buffer, out int written, provider: _formatProvider,
                                    format: TimeFormat))
                            {
                                writer.WriteStringValue(buffer.Slice(0, written));
                            }

                            break;
                        }
#endif
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
#if FEATURE_IUTF8SPANFORMATTABLE
                            else if (vt is IUtf8SpanFormattable utf8Span)
                            {
                                Span<byte> buffer = stackalloc byte[_spanBufferSize * 2];
                                if (utf8Span.TryFormat(buffer, out int written, provider: _formatProvider,
                                        format: default))
                                {
                                    // fallback to string
                                    writer.WriteStringValue(buffer.Slice(0, written));
                                }
                            }
#endif
#if FEATURE_ISPANFORMATTABLE
                            else if (vt is ISpanFormattable span)
                            {
                                Span<char> buffer = stackalloc char[_spanBufferSize];
                                if (span.TryFormat(buffer, out int written, provider: _formatProvider, format: default))
                                {
                                    // fallback to string
                                    writer.WriteStringValue(buffer.Slice(0, written));
                                }
                            }
#endif

                            break;
                        }
                    }

                    break;
#if FEATURE_IUTF8SPANFORMATTABLE
                case IUtf8SpanFormattable span:
                {
                    Span<byte> buffer = stackalloc byte[_spanBufferSize * 4];
                    if (span.TryFormat(buffer, out int written, provider: _formatProvider, format: default))
                    {
                        // fallback to string
                        writer.WriteStringValue(buffer.Slice(0, written));
                    }

                    break;
                }
#endif
#if FEATURE_ISPANFORMATTABLE
                case ISpanFormattable span:
                {
                    Span<char> buffer = stackalloc char[_spanBufferSize * 2];
                    if (span.TryFormat(buffer, out int written, provider: _formatProvider, format: default))
                    {
                        // fallback to string
                        writer.WriteStringValue(buffer.Slice(0, written));
                    }

                    break;
                }
#endif
                default:
                    writer.WriteStringValue(value.Value?.ToString());
                    break;
            }
        }

        private void WriteRenderingsObject(ReadOnlySpan<IGrouping<string, PropertyToken>> tokensWithFormat,
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
                    RenderPropertyToken(format, properties, writer);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }

        private void RenderPropertyToken(PropertyToken pt,
            IReadOnlyDictionary<string, LogEventPropertyValue> properties, Utf8JsonWriter writer)
        {
            if (!properties.TryGetValue(pt.PropertyName, out var propertyValue))
            {
                writer.WriteStringValue(pt.ToString());
                return;
            }

            RenderValue(propertyValue, pt.Format, writer);
        }

        private void RenderValue(LogEventPropertyValue propertyValue,
            string format, Utf8JsonWriter writer)
        {
            var value = propertyValue as ScalarValue;
            if (value?.Value is string str)
            {
                writer.WriteStringValue(str);
                return;
            }

            propertyValue.Render(_sw.Value, format, _formatProvider);
            writer.WriteStringValue(_sw.Value.ToString());
            _sb.Value.Clear();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _writer.Value.Dispose();
            _writer.Dispose();

            _sw.Value.Dispose();
            _sw.Dispose();
            _sb.Dispose();
        }
    }
}
