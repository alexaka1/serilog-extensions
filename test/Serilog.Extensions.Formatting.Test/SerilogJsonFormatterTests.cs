using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;
using Xunit.Abstractions;
using static Serilog.Events.LogEventLevel;

namespace Serilog.Extensions.Formatting.Test
{
    public class SerilogJsonFormatterTests
    {
        public SerilogJsonFormatterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        private string FormatToJson(LogEvent @event)
        {
            var formatter = new Utf8JsonFormatter();
            var stringWriter = new StringWriter();
            formatter.Format(@event, stringWriter);
            string result = stringWriter.ToString();
            Helpers.AssertValidJson(result, _output);
            return result;
        }

        private JsonObject FormatJson(LogEvent @event)
        {
            string json = FormatToJson(@event);
#if DEBUG
            _output.WriteLine(json);
#endif
            return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }

        private class MyDictionary : Dictionary<string, object>
        {
        }

        private static JsonObject FormatEvent(LogEvent e)
        {
            var j = new Utf8JsonFormatter();

            var f = new StringWriter();
            j.Format(e, f);
            return JsonNode.Parse(f.ToString())?.AsObject() ?? new JsonObject();
        }

        [Fact]
        public void ABooleanPropertySerializesAsBooleanValue()
        {
            string name = Some.String();
            const bool Value = true;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (bool?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AByteSerializesAsNumericValue()
        {
            string name = Some.String();
            const byte Value = 123;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (byte?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ACharPropertySerializesAsStringValue()
        {
            string name = Some.String();
            const char Value = 'c';
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value.ToString(), (string)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ADecimalSerializesAsNumericValue()
        {
            string name = Some.String();
            const decimal Value = 123.45m;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (decimal?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ADictionaryWithScalarKeySerializesAsAnObject()
        {
            int dictKey = Some.Int();
            int dictValue = Some.Int();
            var dict = new DictionaryValue(new Dictionary<ScalarValue, LogEventPropertyValue>
            {
                { new ScalarValue(dictKey), new ScalarValue(dictValue) },
            });
            var dictProp = new LogEventProperty(Some.String(), dict);
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(dictProp);

            string formatted = FormatToJson(@event);
            string expected = $@"{{""{dictKey}"":{dictValue}}}";
            Assert.Contains(expected, formatted);
        }

        [Fact]
        public void ADoubleSerializesAsNumericValue()
        {
            string name = Some.String();
            const double Value = 123.45;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (double?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AFloatSerializesAsNumericValue()
        {
            string name = Some.String();
            const float Value = 123.45f;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (float?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AGuidPropertySerializesAsStringValue()
        {
            string name = Some.String();
            var value = Guid.NewGuid();
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal(value.ToString(), (string)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ALongSerializesAsNumericValue()
        {
            string name = Some.String();
            const long Value = long.MaxValue;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (long?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AnArrayPropertySerializesAsObjectToStringValue()
        {
            string name = Some.String();
            Guid[] value = { Guid.Empty, Guid.NewGuid(), Guid.NewGuid() };
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal("System.Guid[]", (string)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ANegativeIntegerPropertySerializesAsIntegerValue()
        {
            string name = Some.String();
            int value = -Some.Int();
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal(value, (int?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AnEnumPropertySerializesAsStringValue()
        {
            string name = Some.String();
            var value = TestEnum.Value1;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal(value.ToString(), (string)formatted["Properties"]?[name]);

            value = TestEnum.asdf;
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            formatted = FormatJson(@event);

            Assert.Equal(value.ToString(), (string)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AnIntegerPropertySerializesAsIntegerValue()
        {
            string name = Some.String();
            int value = Some.Int();
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal(value, (int?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ANullIsSerializedAsNullRegardlessOfType()
        {
            string name = Some.String();
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(null)));

            var formatted = FormatJson(@event);
            Assert.Null(formatted["Properties"]?[name]);
            int? value = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            formatted = FormatJson(@event);
            Assert.Null(formatted["Properties"]?[name]);
        }

        [Fact]
        public void AnUnsignedIntegerPropertySerializesAsIntegerValue()
        {
            string name = Some.String();
            const uint Value = 123;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));
            var formatted = FormatJson(@event);
            Assert.Equal(Value, (uint?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AnUnsignedLongSerializesAsNumericValue()
        {
            string name = Some.String();
            const ulong Value = ulong.MaxValue;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (ulong?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AnUnsignedShortSerializesAsIntegerValue()
        {
            string name = Some.String();
            const ushort Value = 15;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (ushort?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ASbyteSerializesAsNumericValue()
        {
            string name = Some.String();
            const sbyte Value = 123;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (sbyte?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void ASequencePropertySerializesAsArrayValue()
        {
            string name = Some.String();
            int?[] ints = { Some.Int(), Some.Int() };
            var value = new SequenceValue(ints.Select(i => new ScalarValue(i)));
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, value));

            var formatted = FormatJson(@event);
            var result = new List<int?>();
            foreach (var el in formatted["Properties"]?[name]?.AsArray() ?? new JsonArray())
            {
                result.Add((int?)el);
            }

            Assert.Equal(ints, result);
        }

        [Fact]
        public void AShortSerializesAsIntegerValue()
        {
            string name = Some.String();
            const short Value = 15;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(Value)));

            var formatted = FormatJson(@event);

            Assert.Equal(Value, (short?)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AStructureSerializesAsAnObject()
        {
            int value = Some.Int();
            var memberProp = new LogEventProperty(Some.String(), new ScalarValue(value));
            var structure = new StructureValue(new List<LogEventProperty> { memberProp }.AsReadOnly());
            var structureProp = new LogEventProperty(Some.String(), structure);
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(structureProp);

            var formatted = FormatJson(@event);
            int? result = (int?)formatted["Properties"]?[structureProp.Name]?[memberProp.Name];
            Assert.Equal(value, result);
        }

        [Fact]
        public void AStructureWithTypeTagSerializesAsAnObject()
        {
            int value = Some.Int();
            var memberProp = new LogEventProperty(Some.String(), new ScalarValue(value));
            var structure = new StructureValue(new List<LogEventProperty> { memberProp }.AsReadOnly(), "_myTypeTag");
            var structureProp = new LogEventProperty(Some.String(), structure);
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(structureProp);

            var formatted = FormatJson(@event);
            int? result = (int?)formatted["Properties"]?[structureProp.Name]?[memberProp.Name];
            Assert.Equal(value, result);
            string typeTag = (string)formatted["Properties"]?[structureProp.Name]?["_typeTag"];
            Assert.Equal("_myTypeTag", typeTag);
        }

        [Fact]
        public void ATimeSpanSerializesAsStringValue()
        {
            string name = Some.String();
            var value = TimeSpan.FromSeconds(123.45);
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal(value.ToString(), (string)formatted["Properties"]?[name]);
        }

        [Fact]
        public void CustomDictionariesAreDestructuredViaDictionaryValue_When_AsDictionary_Applied()
        {
            var dict = new MyDictionary
            {
                { "hello", "world" },
                { "nums", new[] { 1.2 } },
            };

            var e = DelegatingSink.GetLogEvent(l => l.Information("Value is {ADictionary}", dict),
                cfg => cfg.Destructure.AsDictionary<MyDictionary>());
            var f = FormatJson(e);
            Assert.Equal("world", (string)f["Properties"]?["ADictionary"]?["hello"]);
            Assert.Equal(1.2, (double?)f["Properties"]?["ADictionary"]?["nums"]?[0]);
        }

        [Fact]
        public void CustomDictionariesAreDestructuredViaDictionaryValue_When_AsDictionary_Applied_IsValid()
        {
            var dict = new MyDictionary
            {
                { "hello", "world" },
                { "nums", new[] { 1.2 } },
            };

            var e = DelegatingSink.GetLogEvent(l => l.Information("Value is {ADictionary}", dict),
                cfg => cfg.Destructure.AsDictionary<MyDictionary>());
            string json = FormatToJson(e);
            Helpers.AssertValidJson(json);
        }

        [Fact]
        public void DictionariesAreDestructuredViaDictionaryValue()
        {
            var dict = new Dictionary<string, object>
            {
                { "hello", "world" },
                { "nums", new[] { 1.2 } },
            };

            var e = DelegatingSink.GetLogEvent(l => l.Information("Value is {ADictionary}", dict));
            var f = FormatJson(e);

            Assert.Equal("world", (string)f["Properties"]?["ADictionary"]?["hello"]);
            Assert.Equal(1.2, (double?)f["Properties"]?["ADictionary"]?["nums"]?[0]);
        }

        [Fact]
        public void JsonFormattedDate()
        {
            var @event = new LogEvent(
                DateTimeOffset.MaxValue,
                Information,
                null,
                Some.MessageTemplate(),
                new List<LogEventProperty>
                        { new LogEventProperty("name", new ScalarValue(DateTime.Parse("2023-01-01T12:34:56.789000"))) }
                    .AsReadOnly());

            var formatted = FormatJson(@event);
            Assert.Equal(
                // zeroes are trimmed
                "2023-01-01T12:34:56.789",
                (string)formatted["Properties"]?["name"]);
        }

#if FEATURE_DATE_AND_TIME_ONLY
        [Fact]
        public void JsonFormattedDateOnly()
        {
            var @event = new LogEvent(
                DateTimeOffset.MaxValue,
                Information,
                null,
                Some.MessageTemplate(),
                new List<LogEventProperty>
                    { new LogEventProperty("name", new ScalarValue(DateOnly.MaxValue)) }.AsReadOnly());

            var formatted = FormatJson(@event);
            Assert.Equal(
                "9999-12-31",
                (string)formatted["Properties"]?["name"]);
        }
#endif

        [Fact]
        public void JsonFormattedEventsIncludeTimestamp()
        {
            var @event = new LogEvent(
                new DateTimeOffset(2013, 3, 11, 15, 59, 0, 123, TimeSpan.FromHours(10)),
                Information,
                null,
                Some.MessageTemplate(),
                Array.Empty<LogEventProperty>());

            var formatted = FormatJson(@event);

            Assert.Equal(
                // timestamp is formatted as `O`
                "2013-03-11T15:59:00.1230000+10:00",
                (string)formatted["Timestamp"]);
        }

#if FEATURE_DATE_AND_TIME_ONLY
        [Fact]
        public void JsonFormattedTimeOnly()
        {
            var @event = new LogEvent(
                DateTimeOffset.MaxValue,
                Information,
                null,
                Some.MessageTemplate(),
                new List<LogEventProperty>
                    { new LogEventProperty("name", new ScalarValue(TimeOnly.MaxValue)) }.AsReadOnly());

            var formatted = FormatJson(@event);
            Assert.Equal(
                "23:59:59.9999999",
                (string)formatted["Properties"]?["name"]);
        }
#endif

        [Fact]
        public void PropertyTokensWithFormatStringsAreIncludedAsRenderings()
        {
            var p = new MessageTemplateParser();
            var e = new LogEvent(Some.OffsetInstant(), Information, null,
                p.Parse("{AProperty:000}"),
                new List<LogEventProperty> { new LogEventProperty("AProperty", new ScalarValue(12)) }.AsReadOnly());

            var d = FormatEvent(e);
            var rs = d["Renderings"]?.AsObject() ?? new JsonObject();
            Assert.Single(rs);
            var ap = d["Renderings"]?["AProperty"];
            var fs = ap?.AsArray() ?? new JsonArray();
            Assert.Single(fs);
            Assert.Equal("000", (string)fs.Single()?["Format"]);
            Assert.Equal("012", (string)fs.Single()?["Rendering"]);
        }

        [Fact]
        public void PropertyTokensWithoutFormatStringsAreNotIncludedAsRenderings()
        {
            var p = new MessageTemplateParser();
            var e = new LogEvent(Some.OffsetInstant(), Information, null,
                p.Parse("{AProperty}"),
                new List<LogEventProperty> { new LogEventProperty("AProperty", new ScalarValue(12)) }.AsReadOnly());

            var d = FormatEvent(e);

            var rs = (IEnumerable)d["Renderings"];
            Assert.Null(rs);
        }

        [Fact]
        public void ReadonlyDictionariesAreDestructuredViaDictionaryValue()
        {
            var dict = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>
            {
                { "hello", "world" },
                { "nums", new[] { 1.2 } },
            });

            var e = DelegatingSink.GetLogEvent(l => l.Information("Value is {ADictionary}", dict));
            var f = FormatJson(e);

            Assert.Equal("world", (string)f["Properties"]?["ADictionary"]?["hello"]);
            Assert.Equal(1.2, (double?)f["Properties"]?["ADictionary"]?["nums"]?[0]);
        }

        [Fact] // See https://github.com/serilog/serilog/issues/1924
        public void RenderedMessageIsIncludedCorrectlyWhenRequired()
        {
            var p = new MessageTemplateParser();
            var e = new LogEvent(Some.OffsetInstant(), Information, null,
                p.Parse("Value: {AProperty}"),
                new List<LogEventProperty> { new LogEventProperty("AProperty", new ScalarValue(12)) }.AsReadOnly());

            var formatter = new Utf8JsonFormatter(renderMessage: true);

            var buffer = new StringWriter();
            formatter.Format(e, buffer);
            string json = buffer.ToString();

            Assert.Contains(@",""MessageTemplate"":""Value: {AProperty}"",""RenderedMessage"":""Value: 12"",", json);
        }

        [Fact]
        public void SequencesOfSequencesAreSerialized()
        {
            var p = new MessageTemplateParser();
            var e = new LogEvent(Some.OffsetInstant(), Information, null,
                p.Parse("{@AProperty}"),
                new List<LogEventProperty>
                {
                    new LogEventProperty("AProperty", new SequenceValue(new List<LogEventPropertyValue>
                        {
                            new SequenceValue(new List<LogEventPropertyValue>
                                { new ScalarValue("Hello") }.AsReadOnly()),
                        }
                        .AsReadOnly())),
                }.AsReadOnly());

            var d = FormatEvent(e);

            string h = (string)d["Properties"]?["AProperty"]?[0]?[0];
            Assert.Equal("Hello", h);
        }

        [Fact]
        public void TraceAndSpanAreIgnoredWhenAbsent()
        {
            var evt = Some.LogEvent(traceId: default, spanId: default);
            var sw = new StringWriter();
            var formatter = new Utf8JsonFormatter();
            formatter.Format(evt, sw);
            string formatted = sw.ToString();
            Assert.DoesNotContain("TraceId", formatted, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SpanId", formatted, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TraceAndSpanAreIncludedWhenPresent()
        {
            var traceId = ActivityTraceId.CreateRandom();
            var spanId = ActivitySpanId.CreateRandom();
            var evt = Some.LogEvent(traceId: traceId, spanId: spanId);
            var sw = new StringWriter();
            var formatter = new Utf8JsonFormatter();
            formatter.Format(evt, sw);
            string formatted = sw.ToString();
            Assert.Contains($@"""TraceId"":""{traceId}""", formatted);
            Assert.Contains($@"""SpanId"":""{spanId}""", formatted);
        }

#if FEATURE_ISPANFORMATTABLE
        [Fact]
        public void AnISpanFormattablePropertySerializesAsStringValue()
        {
            string name = Some.String();
            var value = IPAddress.Parse("fdca:47ef:b356:4234::1");
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal(value.ToString(), (string)formatted["Properties"]?[name]);
        }

        [Fact]
        public void AnISpanFormattableValueTypePropertySerializesAsStringValue()
        {
            string name = Some.String();
            var value = (Half)123.45;
            var @event = Some.InformationEvent();
            @event.AddOrUpdateProperty(new LogEventProperty(name, new ScalarValue(value)));

            var formatted = FormatJson(@event);

            Assert.Equal(value.ToString(CultureInfo.InvariantCulture), (string)formatted["Properties"]?[name]);
        }
#endif
    }

    internal enum TestEnum
    {
        Value1,

        // ReSharper disable once InconsistentNaming
        asdf,
    }
}
