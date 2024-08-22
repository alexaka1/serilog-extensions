using System.Text.Json;

namespace Serilog.Extensions.Formatting
{
    internal readonly struct JsonLogPropertyNames
    {
        public JsonLogPropertyNames(JsonNamingPolicy namingPolicy)
        {
            Timestamp = namingPolicy.ConvertName(TimestampPropertyName);
            Level = namingPolicy.ConvertName(LevelPropertyName);
            MessageTemplate = namingPolicy.ConvertName(MessageTemplatePropertyName);
            RenderedMessage = namingPolicy.ConvertName(RenderedMessagePropertyName);
            TraceId = namingPolicy.ConvertName(TraceIdPropertyName);
            SpanId = namingPolicy.ConvertName(SpanIdPropertyName);
            Exception = namingPolicy.ConvertName(ExceptionPropertyName);
            Properties = namingPolicy.ConvertName(PropertiesPropertyName);
            Renderings = namingPolicy.ConvertName(RenderingsPropertyName);
            Null = namingPolicy.ConvertName(NullPropertyName);
            TypeTag = namingPolicy.ConvertName(TypeTagPropertyName);
            Format = namingPolicy.ConvertName(FormatPropertyName);
            Rendering = namingPolicy.ConvertName(RenderingPropertyName);
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

        public string Timestamp { get; }
        public string Level { get; }
        public string MessageTemplate { get; }
        public string RenderedMessage { get; }
        public string TraceId { get; }
        public string SpanId { get; }
        public string Exception { get; }
        public string Properties { get; }
        public string Renderings { get; }
        public string Null { get; }
        public string TypeTag { get; }
        public string Format { get; }
        public string Rendering { get; }
    }
}
