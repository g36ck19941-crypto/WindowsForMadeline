# AssetWorker Protocol and Supervision

## Implemented function

CDR-011 adds a real independent console Worker process plus a client supervisor. The Worker has no parser and no filesystem request message. Its complete protocol surface is:

- `Hello` → `Ready`
- `Ping` → `Pong`
- `Shutdown` → `Stopped`
- bounded `Error`

## Bounded protocol

- protocol version: `1`;
- frame: 4-byte little-endian payload length followed by UTF-8 JSON;
- maximum payload: 4 KiB, checked before allocation;
- maximum error code: 128 characters;
- required non-empty request ID;
- unknown fields, unknown message types, invalid versions, invalid error shapes, truncated frames and malformed JSON are rejected.

The protocol contains no arbitrary payload, path, image, asset or executable behavior.

## Role in the project

CDR-011 gives future asset parsers a separate, supervised process to run in. If a parser later encounters corrupt input, the main desktop application can stop that Worker and start a fresh one instead of being taken down with it. This is the safety shell required before adding the first `.meta` parser.

It does not yet read or understand any game asset, and it does not prove that Madeline can be rendered or moved.

## Launch boundary

The framework-dependent launcher accepts only:

- a process host named `dotnet` or `dotnet.exe`;
- a fully qualified, existing, regular, non-reparse file named exactly `CelesteDesktop.AssetWorker.Process.dll`.

It cannot be configured through this contract to launch Celeste, Everest or another arbitrary executable. The Worker runs with `UseShellExecute=false`, redirected binary stdin/stdout and `CreateNoWindow=true`.

## Supervisor behavior

The supervisor serializes lifecycle operations and applies separate startup, request and shutdown timeouts. Expected failures return `AssetWorkerOperationResult` with:

- stable operation code;
- current state;
- bounded detail code;
- process exit code when observed;
- recovery action.

Startup, Ping and shutdown failures terminate and dispose the session within bounded cleanup windows. After a crashed or rejected session reaches `Faulted`, a later `StartAsync` creates a fresh session. Caller cancellation is distinguished from timeout.

The Worker writes fallback failures as one bounded JSON object to stderr with timestamp, run ID, event ID, subsystem, stage, outcome, code, exception type and recovery flag. Binary protocol data is written only to stdout.

## Verification

Run from the repository root:

```powershell
.\tools\Verify-CDR011.ps1
```

Expected results:

- Release build: 0 warnings, 0 errors;
- CDR-010 regression: `RESULT total=15 passed=15 failed=0`;
- CDR-011: `RESULT total=27 passed=27 failed=0`.

The CDR-011 suite covers malformed and oversized frames, invalid versions, arbitrary launch rejection, startup/request/shutdown timeouts, cancellation, start and runtime crashes, bounded cleanup, recovery with a new session and a real hidden Worker Start→Ping→Stop lifecycle.

## Current limitation

No asset request exists. Passing CDR-011 proves only bounded process isolation and lifecycle communication. It does not prove installation access, `.meta` parsing, `.data` decoding, sprite metadata, original assets or visible rendering.

## Next gate

Developer acceptance authorizes only CDR-012: an independent bounded `.meta` parser using synthetic byte fixtures. Real installation access remains separately gated at CDR-016.
