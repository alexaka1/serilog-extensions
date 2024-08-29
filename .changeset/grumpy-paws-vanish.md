---
"alexaka1.serilog.extensions.formatting": patch
---

Small performance increase by pre-encoding the default property names, to be able to write them to stream as-is.
