# Alexaka1.Serilog.Extensions.Formatting

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
      "namingPolicy": "System.Text.Json.JsonNamingPolicy::CamelCase"
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

# Why?

First of all, it was a fun mental exercise to discover how `Utf8JsonWriter` works.

Secondly I had a suspicion that the recommended `ExpressionTemplate` is not as performant as a `Utf8JsonWriter`.
`Serilog.Extensions.Formatting.Benchmark.JsonFormatterEnrichBenchmark`

| Method                | Categories            | Formatter      |          Mean |         Error |        StdDev |       Gen0 |    Allocated |
|-----------------------|-----------------------|----------------|--------------:|--------------:|--------------:|-----------:|-------------:|
| **ComplexProperties** | **ComplexProperties** | **Json**       | **11.938 μs** | **0.2943 μs** | **0.8678 μs** | **2.3804** |  **7.45 KB** |
| **ComplexProperties** | **ComplexProperties** | **Utf8Json**   | **13.013 μs** | **0.2968 μs** | **0.8705 μs** | **2.3193** |  **7.16 KB** |
| **ComplexProperties** | **ComplexProperties** | **Expression** | **20.847 μs** | **0.4718 μs** | **1.3537 μs** | **3.5400** | **11.21 KB** |
|                       |                       |                |               |               |               |            |              |
| **EmitLogEvent**      | **EmitLogEvent**      | **Json**       |  **6.750 μs** | **0.1592 μs** | **0.4593 μs** | **1.6479** |  **5.16 KB** |
| **EmitLogEvent**      | **EmitLogEvent**      | **Utf8Json**   |  **7.289 μs** | **0.1435 μs** | **0.2552 μs** | **1.5259** |  **4.81 KB** |
| **EmitLogEvent**      | **EmitLogEvent**      | **Expression** | **11.630 μs** | **0.2741 μs** | **0.8038 μs** | **2.1973** |  **6.78 KB** |
|                       |                       |                |               |               |               |            |              |
| **IntProperties**     | **IntProperties**     | **Json**       |  **6.871 μs** | **0.1919 μs** | **0.5629 μs** | **1.6479** |  **5.12 KB** |
| **IntProperties**     | **IntProperties**     | **Utf8Json**   |  **6.978 μs** | **0.1468 μs** | **0.4212 μs** | **1.5259** |  **4.77 KB** |
| **IntProperties**     | **IntProperties**     | **Expression** | **10.997 μs** | **0.2177 μs** | **0.4687 μs** | **2.2583** |  **6.99 KB** |

Thirdly, I specifically had a usecas in the project thich required logs to be in `camelCase`, and none of the built-in formatters supported that, not even `ExpressionTemplate`, since I couldn't find a way to specify a custom `JsonNamingPolicy` for properties.
