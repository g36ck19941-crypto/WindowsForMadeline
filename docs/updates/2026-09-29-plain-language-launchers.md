# Plain-language developer launchers

Date: 2026-09-29  
Type: launcher and documentation UX; no runtime feature change

## What changed

- `演示当前进度.cmd` now explains in plain Chinese what the cumulative demo generates, what the report should show and why generated pixels do not prove real desktop visibility.
- `验证当前版本.cmd` now explains what the 365 checks establish, how to recognize success and which claims remain outside the evidence.
- Both entries tell the developer to preserve the first error and its stage when reporting a failure.
- `AGENTS.md` now requires future developer-facing root launchers to retain this plain-language structure.
- `.cmd` files are now explicitly checked out with Windows CRLF endings, and the verifier's source-level evidence markers remain ASCII-safe for Windows PowerShell 5.1.
- After developer clarification, the generated HTML report's proof/limits area was also rewritten as concrete plain-language bullet points. Stable report IDs make the verifier protect both explanations from accidental removal.

## Project role

This makes manual acceptance easier to interpret. The developer can distinguish “the project demonstrates a behavior” from “the automated checks passed” without knowing internal module names.

## Limits

No animation, simulation, rendering or desktop behavior changed. CDR-032 remains pending developer acceptance and was not uploaded by this update.
