using System.Text.Json;

namespace Serilog.Extensions.Formatting
{
    internal readonly struct JsonLogPropertyNames(JsonNamingPolicy namingPolicy)
    {
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

        public JsonEncodedText Timestamp { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(TimestampPropertyName));
        public JsonEncodedText Level { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(LevelPropertyName));
        public JsonEncodedText MessageTemplate { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(MessageTemplatePropertyName));
        public JsonEncodedText RenderedMessage { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(RenderedMessagePropertyName));
        public JsonEncodedText TraceId { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(TraceIdPropertyName));
        public JsonEncodedText SpanId { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(SpanIdPropertyName));
        public JsonEncodedText Exception { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(ExceptionPropertyName));
        public JsonEncodedText Properties { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(PropertiesPropertyName));
        public JsonEncodedText Renderings { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(RenderingsPropertyName));
        public JsonEncodedText Null { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(NullPropertyName));
        public JsonEncodedText TypeTag { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(TypeTagPropertyName));
        public JsonEncodedText Format { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(FormatPropertyName));
        public JsonEncodedText Rendering { get; } = JsonEncodedText.Encode(namingPolicy.ConvertName(RenderingPropertyName));
    }
}
