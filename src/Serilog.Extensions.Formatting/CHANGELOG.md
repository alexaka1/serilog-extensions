# alexaka1.serilog.extensions.formatting

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
