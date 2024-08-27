---
"alexaka1.serilog.extensions.formatting": minor
---

Fixed Utf8JsonFormatter thread safety. The formatter is now threadsafe, as it holds no state that can produce a race condition, at the cost of performance.
