---
applyTo: ".github/workflows/*.yml"
---

- **NEVER use `if: always()`** - This can create deadlocked jobs that cannot be canceled.
  - **Use `if: ${{ !cancelled() }}` instead** - This allows proper job cancellation while still running cleanup steps
  - This applies to all workflow steps that should run regardless of previous step failures
- If you need a temp directory use `${{ runner.temp }}`
- Do not bother pinning the actions to commit hashes as you will just hallucinate the hash anyway. Just use the version tags like `v4` or `v1.2.3` etc and Renovate bot will handle the pinning
