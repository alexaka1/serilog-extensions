---
"alexaka1.serilog.extensions.formatting": minor
---

Added explicit case for `Guid` value

Changed the `ISpanFormattable` fallback case to write a string value, just to be safe it is a valid json at the end, since Guid got caught before.
