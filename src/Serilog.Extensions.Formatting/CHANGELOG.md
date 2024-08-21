# alexaka1.serilog.extensions.formatting

## 0.1.1-test.0

### Patch Changes

- 1c9501c: Bump Serilog to 4.0.1
- 5eef3ee: Test tagging

## 0.1.0

### Minor Changes

- 6a517fa: Added explicit case for `Guid` value

  Changed the `ISpanFormattable` fallback case to write a string value, just to be safe it is a valid json at the end, since Guid got caught before.
