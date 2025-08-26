# Alexaka1.Serilog.Extensions.Formatting

[![NuGet Version](https://img.shields.io/nuget/v/Alexaka1.Serilog.Extensions.Formatting)](https://www.nuget.org/packages/Alexaka1.Serilog.Extensions.Formatting)  
[![Copilot Setup Steps](https://github.com/alexaka1/serilog-extensions/actions/workflows/copilot-setup-steps.yml/badge.svg?branch=main)](https://github.com/alexaka1/serilog-extensions/actions/workflows/copilot-setup-steps.yml)

## Utf8JsonFormatter

A simple JSON formatter for Serilog that uses the `System.Text.Json.Utf8JsonWriter` to write the log events to the output stream. As the name suggests, it is entirely built around UTF-8, with all the [.NET optimizations for UTF-8](https://github.com/dotnet/runtime/issues/81500), so using other encodings will most likely result in invalid characters. The default for the File sink is UTF-8.

### Usage

```csharp
var logger = new LoggerConfiguration()
    .WriteTo.File(new Utf8JsonFormatter(), "log.json")
    .CreateLogger();
```

```json5
{
  "Name": "Console",
  "Args": {
    "formatter": {
      "type": "Serilog.Extensions.Formatting.Utf8JsonFormatter, Serilog.Extensions.Formatting",
      // if you want to use a custom naming policy, you can specify it here
      "namingPolicy": "System.Text.Json.JsonNamingPolicy::CamelCase, System.Text.Json"
    }
  }
}
```

### Options

The `Utf8JsonFormatter` constructor accepts the following options:

- `closingDelimiter`: Closing delimiter of the log event. Defaults to `Environment.NewLine`.
- `renderMessage`: A boolean that determines whether the message template will be rendered. Defaults to `false`.
- `formatProvider`: An `IFormatProvider` that will be used to format the message template. Defaults to `CultureInfo.InvariantCulture`.
- `spanBufferSize`: The size of the buffer used to format the `ISpanFormattable` values. Defaults to `64`.
- `skipValidation`: A boolean that determines whether the JSON writer will skip validation. Defaults to `true`.
- `namingPolicy`: A `JsonNamingPolicy` that will be used to convert the property names. Default is leaving the property names as they are.
- `jsonWriterEncoder`: A `JavaScriptEncoder` that will be used to encode the JSON output. Defaults to `null` which is considered to be safe. However please note that some Unicode characters will be escaped in the output. [More info](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding).

# Why?

I specifically had a use-case in a project which required logs to be in `camelCase`, and none of the built-in formatters supported that, not even `ExpressionTemplate`, since I couldn't find a way to specify a custom `JsonNamingPolicy` for properties.
