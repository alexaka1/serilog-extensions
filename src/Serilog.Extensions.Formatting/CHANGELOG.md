# alexaka1.serilog.extensions.formatting

## 0.4.1

### Patch Changes

- c82b2ca: Increase performance by using a Utf8JsonWriter on a per thread basis, thus retaining the previous single-threaded performance.

## 0.4.0

### Minor Changes

- 219443c: Fixed Utf8JsonFormatter thread safety. The formatter is now threadsafe, as it holds no state that can produce a race condition, at the cost of performance.

## 0.3.0

### Minor Changes

- db2c006: Add netstandard2.0 support
- ee9314c: Add IUtf8SpanFormattable support as fallback option
- db2c006: Added .NET 6 support

## 0.2.1

### Patch Changes

- 779be2b: Fix incorrect readme statement on Renderings not being supported. It is supported since 0.2.0

## 0.2.0

### Minor Changes

- fd4cfae: Add `Renderings` property to the output JSON, bringing it to feature parity with `JsonFormatter`

  Add `Encoder` parameter to `Utf8JsonFormatter` constructor. If you are sure that the consumer is going to interpret the JSON as UTF-8, you can set this to `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` to not escape non-ASCII characters.

## 0.1.1

### Patch Changes

- 1c9501c: Bump Serilog to 4.0.1

## 0.1.0

### Minor Changes

- 6a517fa: Added explicit case for `Guid` value

  Changed the `ISpanFormattable` fallback case to write a string value, just to be safe it is a valid json at the end, since Guid got caught before.
