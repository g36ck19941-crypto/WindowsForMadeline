# Remote Repository Workflow

## Canonical remote

`https://github.com/g36ck19941-crypto/WindowsForMadeline`

## Update lifecycle

1. Implement and verify an update locally.
2. Commit it locally with a bounded message.
3. Report outcome, evidence, remaining gate and local commit to the developer.
4. Wait for explicit confirmation that the update is effective.
5. Audit the exact outgoing commit range for commercial assets, decoded frames, audio, binaries, caches, installation paths and unrelated files.
6. Push an independent remote branch without force.
7. Read back the remote ref and compare its SHA with the local commit.
8. Explain the remote branch, SHA, changes, tests, limitations and asset-audit result.

Remote `main` is never force-pushed or silently replaced. A merge, pull request or direct `main` update is a separate developer decision.

## Connection-test evidence

- Remote main before and after: `d237277e10af090cf60ec015c22174565e6cdc0a`
- Test branch: `codex/connection-test-20260921`
- Test commit: `df00e2c04c3a654da1f4cb7ce12900502d3888d8`
- Content: `docs/connection-test-2026-09-21.md`
- Commercial assets, decoded frames, game binaries and installation paths: zero
- Product code or acceptance claims: zero
