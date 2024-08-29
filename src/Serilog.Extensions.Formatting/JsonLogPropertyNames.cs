using System.Text.Json;

namespace Serilog.Extensions.Formatting
{
    internal readonly struct JsonLogPropertyNames
    {
        public JsonLogPropertyNames(JsonNamingPolicy namingPolicy)
        {
            Timestamp = JsonEncodedText.Encode(namingPolicy.ConvertName(TimestampPropertyName));
            Level = JsonEncodedText.Encode(namingPolicy.ConvertName(LevelPropertyName));
            MessageTemplate = JsonEncodedText.Encode(namingPolicy.ConvertName(MessageTemplatePropertyName));
            RenderedMessage = JsonEncodedText.Encode(namingPolicy.ConvertName(RenderedMessagePropertyName));
            TraceId = JsonEncodedText.Encode(namingPolicy.ConvertName(TraceIdPropertyName));
            SpanId = JsonEncodedText.Encode(namingPolicy.ConvertName(SpanIdPropertyName));
            Exception = JsonEncodedText.Encode(namingPolicy.ConvertName(ExceptionPropertyName));
            Properties = JsonEncodedText.Encode(namingPolicy.ConvertName(PropertiesPropertyName));
            Renderings = JsonEncodedText.Encode(namingPolicy.ConvertName(RenderingsPropertyName));
            Null = JsonEncodedText.Encode(namingPolicy.ConvertName(NullPropertyName));
            TypeTag = JsonEncodedText.Encode(namingPolicy.ConvertName(TypeTagPropertyName));
            Format = JsonEncodedText.Encode(namingPolicy.ConvertName(FormatPropertyName));
            Rendering = JsonEncodedText.Encode(namingPolicy.ConvertName(RenderingPropertyName));
        }

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

        public JsonEncodedText Timestamp { get; }
        public JsonEncodedText Level { get; }
        public JsonEncodedText MessageTemplate { get; }
        public JsonEncodedText RenderedMessage { get; }
        public JsonEncodedText TraceId { get; }
        public JsonEncodedText SpanId { get; }
        public JsonEncodedText Exception { get; }
        public JsonEncodedText Properties { get; }
        public JsonEncodedText Renderings { get; }
        public JsonEncodedText Null { get; }
        public JsonEncodedText TypeTag { get; }
        public JsonEncodedText Format { get; }
        public JsonEncodedText Rendering { get; }
    }
}
