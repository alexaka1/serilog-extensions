---
"alexaka1.serilog.extensions.formatting": patch
---

Increase performance by using a Utf8JsonWriter on a per thread basis, thus retaining the previous single-threaded performance.
