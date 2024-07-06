using System.Text.Json;

namespace Serilog.Extensions.Formatting;

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

    public string Timestamp { get; } = namingPolicy.ConvertName(TimestampPropertyName);
    public string Level { get; } = namingPolicy.ConvertName(LevelPropertyName);
    public string MessageTemplate { get; } = namingPolicy.ConvertName(MessageTemplatePropertyName);
    public string RenderedMessage { get; } = namingPolicy.ConvertName(RenderedMessagePropertyName);
    public string TraceId { get; } = namingPolicy.ConvertName(TraceIdPropertyName);
    public string SpanId { get; } = namingPolicy.ConvertName(SpanIdPropertyName);
    public string Exception { get; } = namingPolicy.ConvertName(ExceptionPropertyName);
    public string Properties { get; } = namingPolicy.ConvertName(PropertiesPropertyName);
    public string Renderings { get; } = namingPolicy.ConvertName(RenderingsPropertyName);
    public string Null { get; } = namingPolicy.ConvertName(NullPropertyName);
    public string TypeTag { get; } = namingPolicy.ConvertName(TypeTagPropertyName);
    public string Format { get; } = namingPolicy.ConvertName(FormatPropertyName);
    public string Rendering { get; } = namingPolicy.ConvertName(RenderingPropertyName);
}
