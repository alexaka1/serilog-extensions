using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;

namespace Serilog.Extensions.Formatting
{
    /// <summary>
    ///     Formats log events in a simple JSON structure using <see cref="System.Text.Json.Utf8JsonWriter" />.
    /// </summary>
    public class Utf8JsonFormatter : ITextFormatter, IDisposable, IAsyncDisposable
    {
        private readonly string _closingDelimiter;
        private readonly CultureInfo _formatProvider;
        private readonly JsonLogPropertyNames _names;
        private readonly JsonNamingPolicy _namingPolicy;
        private readonly bool _renderMessage;
        private readonly StringBuilder _sb;

        // ReSharper disable once NotAccessedField.Local
        private readonly int _spanBufferSize;
        private readonly StringWriter _sw;
        private readonly Utf8JsonWriter _writer;
        private const string TimeFormat = "O";
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
            _writer = new Utf8JsonWriter(Stream.Null,
                new JsonWriterOptions
                {
                    SkipValidation = skipValidation,
                    Encoder = jsonWriterEncoder,
                });
            _sb = new StringBuilder();
            _sw = new StringWriter(_sb);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
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

            ResetWriter(str);

            _writer.WriteStartObject();
            _writer.WriteString(_names.Timestamp, logEvent.Timestamp.ToString(TimeFormat, _formatProvider));
            _writer.WriteString(_names.Level, Enum.GetName(typeof(LogEventLevel), logEvent.Level));
            _writer.WriteString(_names.MessageTemplate, logEvent.MessageTemplate.Text);
            if (_renderMessage)
            {
                _writer.WriteString(_names.RenderedMessage,
                    logEvent.MessageTemplate.Render(logEvent.Properties, _formatProvider));
            }

            if (logEvent.TraceId.HasValue)
            {
                _writer.WriteString(_names.TraceId, logEvent.TraceId.Value.ToString());
            }

            if (logEvent.SpanId.HasValue)
            {
                _writer.WriteString(_names.SpanId, logEvent.SpanId.Value.ToString());
            }

            if (logEvent.Exception != null)
            {
                _writer.WriteString(_names.Exception, logEvent.Exception.ToString());
            }

            if (logEvent.Properties.Count != 0)
            {
                _writer.WriteStartObject(_names.Properties);
                foreach (var property in logEvent.Properties)
                {
                    _writer.WritePropertyName(_namingPolicy.ConvertName(property.Key));
                    Format(property.Value);
                }

                _writer.WriteEndObject();
            }


            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null)
                .GroupBy(pt => pt.PropertyName)
                .ToArray();

            if (tokensWithFormat.Length != 0)
            {
                _writer.WriteStartObject(_names.Renderings);
                WriteRenderingsObject(tokensWithFormat, logEvent.Properties);
                _writer.WriteEndObject();
            }

            _writer.WriteEndObject();
            _writer.Flush();
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
        private void ResetWriter(Stream stream)
        {
            _writer.Reset(stream);
        }

        private void Format<TState>(TState value) where TState : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            switch (value)
            {
                case ScalarValue scalarValue:
                    VisitScalarValue(scalarValue);
                    return;
                case SequenceValue sequenceValue:
                    VisitSequenceValue(sequenceValue);
                    return;
                case StructureValue structureValue:
                    VisitStructureValue(structureValue);
                    return;
                case DictionaryValue dictionaryValue:
                    VisitDictionaryValue(dictionaryValue);
                    return;
                default:
                    throw new NotSupportedException($"The value {value} is not of a type supported by this visitor.");
            }
        }

        private void VisitDictionaryValue(DictionaryValue dictionary)
        {
            _writer.WriteStartObject();
            foreach (var element in dictionary.Elements)
            {
                string key = element.Key.Value?.ToString();
                _writer.WritePropertyName(key != null ? _namingPolicy.ConvertName(key) : _names.Null);
                Format(element.Value);
            }

            _writer.WriteEndObject();
        }

        private void VisitStructureValue(StructureValue structure)
        {
            _writer.WriteStartObject();
            foreach (var property in structure.Properties)
            {
                _writer.WritePropertyName(_namingPolicy.ConvertName(property.Name));
                Format(property.Value);
            }

            if (structure.TypeTag != null)
            {
                _writer.WriteString(_names.TypeTag, structure.TypeTag);
            }

            _writer.WriteEndObject();
        }

        private void VisitSequenceValue(SequenceValue sequence)
        {
            _writer.WriteStartArray();
            foreach (var element in sequence.Elements)
            {
                Format(element);
            }

            _writer.WriteEndArray();
        }

        private void VisitScalarValue(ScalarValue value)
        {
            switch (value.Value)
            {
                case null:
                    _writer.WriteNullValue();
                    break;
                case string str:
                    _writer.WriteStringValue(str);
                    break;
                case ValueType vt:
                    switch (vt)
                    {
                        case int i:
                            _writer.WriteNumberValue(i);
                            break;
                        case uint ui:
                            _writer.WriteNumberValue(ui);
                            break;
                        case long l:
                            _writer.WriteNumberValue(l);
                            break;
                        case ulong ul:
                            _writer.WriteNumberValue(ul);
                            break;
                        case decimal dc:
                            _writer.WriteNumberValue(dc);
                            break;
                        case byte bt:
                            _writer.WriteNumberValue(bt);
                            break;
                        case sbyte sb:
                            _writer.WriteNumberValue(sb);
                            break;
                        case short s:
                            _writer.WriteNumberValue(s);
                            break;
                        case ushort us:
                            _writer.WriteNumberValue(us);
                            break;
                        case double d:
                            _writer.WriteNumberValue(d);
                            break;
                        case float f:
                            _writer.WriteNumberValue(f);
                            break;
                        case bool b:
                            _writer.WriteBooleanValue(b);
                            break;
                        case char c:
// #if NET8_0_OR_GREATER
//                             _writer.WriteStringValue([c]);
// #else
//                         _writer.WriteStringValue(new[] { c });
// #endif
                            _writer.WriteStringValue(new[] { c });
                            break;
                        case DateTime dt:
                            _writer.WriteStringValue(dt);
                            break;
                        case DateTimeOffset dto:
                            _writer.WriteStringValue(dto);
                            break;
                        case TimeSpan timeSpan:
                        {
#if FEATURE_ISPANFORMATTABLE
                            Span<char> buffer = stackalloc char[_spanBufferSize];
                            if (timeSpan.TryFormat(buffer, out int written, formatProvider: _formatProvider,
                                    format: "c"))
                            {
                                _writer.WriteStringValue(buffer.Slice(0, written));
                            }
#else
                            _writer.WriteStringValue(timeSpan.ToString("c", _formatProvider));
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
                                _writer.WriteStringValue(buffer.Slice(0, written));
                            }

                            break;
                        }
                        case TimeOnly timeOnly:
                        {
                            Span<char> buffer = stackalloc char[_spanBufferSize];
                            if (timeOnly.TryFormat(buffer, out int written, provider: _formatProvider,
                                    format: TimeFormat))
                            {
                                _writer.WriteStringValue(buffer.Slice(0, written));
                            }

                            break;
                        }
#endif
                        case Guid guid:
                        {
                            _writer.WriteStringValue(guid);
                            break;
                        }
                        default:
                        {
                            if (vt.GetType().IsEnum)
                            {
                                _writer.WriteStringValue(vt.ToString());
                            }
#if FEATURE_ISPANFORMATTABLE
                            else if (vt is ISpanFormattable span)
                            {
                                Span<char> buffer = stackalloc char[_spanBufferSize];
                                if (span.TryFormat(buffer, out int written, provider: _formatProvider, format: default))
                                {
                                    // fallback to string
                                    _writer.WriteStringValue(buffer.Slice(0, written));
                                }
                            }
#endif

                            break;
                        }
                    }

                    break;
#if FEATURE_ISPANFORMATTABLE
                case ISpanFormattable span:
                {
                    Span<char> buffer = stackalloc char[_spanBufferSize * 2];
                    if (span.TryFormat(buffer, out int written, provider: _formatProvider, format: default))
                    {
                        // fallback to string
                        _writer.WriteStringValue(buffer.Slice(0, written));
                    }

                    break;
                }
#endif
                default:
                    _writer.WriteStringValue(value.Value?.ToString());
                    break;
            }
        }

        private void WriteRenderingsObject(ReadOnlySpan<IGrouping<string, PropertyToken>> tokensWithFormat,
            IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            foreach (var propertyFormats in tokensWithFormat)
            {
                _writer.WriteStartArray(propertyFormats.Key);
                foreach (var format in propertyFormats)
                {
                    _writer.WriteStartObject();
                    _writer.WriteString(_names.Format, format.Format);
                    _writer.WritePropertyName(_names.Rendering);
                    RenderPropertyToken(format, properties);
                    _writer.WriteEndObject();
                }

                _writer.WriteEndArray();
            }
        }

        private void RenderPropertyToken(PropertyToken pt,
            IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            if (!properties.TryGetValue(pt.PropertyName, out var propertyValue))
            {
                _writer.WriteStringValue(pt.ToString());
                return;
            }

            RenderValue(propertyValue, pt.Format);
        }

        private void RenderValue(LogEventPropertyValue propertyValue,
            string format)
        {
            var value = propertyValue as ScalarValue;
            if (value?.Value is string str)
            {
                _writer.WriteStringValue(str);
                return;
            }

            propertyValue.Render(_sw, format, _formatProvider);
            _writer.WriteStringValue(_sw.ToString());
            _sb.Clear();
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer.Dispose();
            }
        }

        private async ValueTask DisposeAsyncCore()
        {
            await _writer.DisposeAsync();
        }
    }
}
