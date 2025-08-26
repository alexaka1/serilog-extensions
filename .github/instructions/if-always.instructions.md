---
applyTo: ".github/workflows/*.yml"
---

- **NEVER use `if: always()`** - This can create deadlocked jobs that cannot be canceled.
- **Use `if: ${{ !cancelled() }}` instead** - This allows proper job cancellation while still running cleanup steps
- This applies to all workflow steps that should run regardless of previous step failures