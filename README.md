# Alexaka1.Serilog.Extensions.Formatting

[![NuGet Version](https://img.shields.io/nuget/v/Alexaka1.Serilog.Extensions.Formatting)](https://www.nuget.org/packages/Alexaka1.Serilog.Extensions.Formatting)

## Utf8JsonFormatter

A simple JSON formatter for Serilog that uses the `System.Text.Json.Utf8JsonWriter` to write the log events to the output stream.

> [!IMPORTANT]
> This formatter currently does not support the `Renderings` property of Serilog.

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

The recommended `ExpressionTemplate` is not as performant as a `Utf8JsonWriter`.
`Serilog.Extensions.Formatting.Benchmark.JsonFormatterEnrichBenchmark`

| Method                | Formatter      |         Mean |        Error |       StdDev |       Median |       Gen0 |    Allocated |
|-----------------------|----------------|-------------:|-------------:|-------------:|-------------:|-----------:|-------------:|
| **EmitLogEvent**      | **Json**       | **11.81 μs** | **0.230 μs** | **0.236 μs** | **11.89 μs** | **2.8687** |  **8.81 KB** |
| **EmitLogEvent**      | **Utf8Json**   | **12.82 μs** | **0.256 μs** | **0.647 μs** | **12.49 μs** | **2.5024** |  **7.76 KB** |
| **EmitLogEvent**      | **Expression** | **23.40 μs** | **0.455 μs** | **0.773 μs** | **23.11 μs** | **4.3945** | **13.69 KB** |
|                       |                |              |              |              |              |            |              |
| **ComplexProperties** | **Json**       | **18.63 μs** | **0.373 μs** | **0.591 μs** | **18.73 μs** | **3.5400** | **10.91 KB** |
| **ComplexProperties** | **Utf8Json**   | **18.48 μs** | **0.362 μs** | **0.519 μs** | **18.37 μs** | **3.1738** |  **9.92 KB** |
| **ComplexProperties** | **Expression** | **33.72 μs** | **0.613 μs** | **0.776 μs** | **33.68 μs** | **5.8594** | **18.05 KB** |
|                       |                |              |              |              |              |            |              |
| **IntProperties**     | **Json**       | **11.45 μs** | **0.209 μs** | **0.186 μs** | **11.44 μs** | **2.7466** |  **8.58 KB** |
| **IntProperties**     | **Utf8Json**   | **13.12 μs** | **0.254 μs** | **0.356 μs** | **12.97 μs** | **2.4414** |  **7.53 KB** |
| **IntProperties**     | **Expression** | **23.84 μs** | **0.464 μs** | **0.534 μs** | **23.69 μs** | **4.3945** | **13.82 KB** |

Also, I specifically had a use-case in a project which required logs to be in `camelCase`, and none of the built-in formatters supported that, not even `ExpressionTemplate`, since I couldn't find a way to specify a custom `JsonNamingPolicy` for properties.
