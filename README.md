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

The recommended `ExpressionTemplate` is not as performant as a `Utf8JsonWriter`.
`Serilog.Extensions.Formatting.Benchmark.JsonFormatterEnrichBenchmark`

| Method            | Formatter      |         Mean |        Error |       StdDev |       Median |       Gen0 |    Allocated |
|-------------------|----------------|-------------:|-------------:|-------------:|-------------:|-----------:|-------------:|
| **EmitLogEvent**  | **Json**       | **12.05 μs** | **0.238 μs** | **0.543 μs** | **11.80 μs** | **2.8687** |  **8.81 KB** |
| ComplexProperties | Json           |     17.41 μs |     0.342 μs |     0.512 μs |     17.27 μs |     3.5400 |     10.91 KB |
| IntProperties     | Json           |     11.94 μs |     0.237 μs |     0.433 μs |     11.86 μs |     2.7466 |      8.58 KB |
|                   |                |              |              |              |              |            |              |
| **EmitLogEvent**  | **Utf8Json**   | **12.37 μs** | **0.246 μs** | **0.283 μs** | **12.27 μs** | **2.5024** |  **7.76 KB** |
| ComplexProperties | Utf8Json       |     18.04 μs |     0.237 μs |     0.185 μs |     18.04 μs |     3.1738 |      9.92 KB |
| IntProperties     | Utf8Json       |     12.76 μs |     0.253 μs |     0.470 μs |     12.77 μs |     2.4414 |      7.53 KB |
|                   |                |              |              |              |              |            |              |
| **EmitLogEvent**  | **Expression** | **23.44 μs** | **0.467 μs** | **0.590 μs** | **23.26 μs** | **4.3945** | **13.69 KB** |
| ComplexProperties | Expression     |     35.41 μs |     0.694 μs |     1.215 μs |     35.64 μs |     5.8594 |     18.05 KB |
| IntProperties     | Expression     |     23.56 μs |     0.453 μs |     0.705 μs |     23.46 μs |     4.3945 |     13.82 KB |

Also, I specifically had a use-case in the project which required logs to be in `camelCase`, and none of the built-in formatters supported that, not even `ExpressionTemplate`, since I couldn't find a way to specify a custom `JsonNamingPolicy` for properties.
