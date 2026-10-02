# Local Behavior Reference

CDR-070 adds a development-only builder for a local, decompiled behavior reference. It helps inspect state ordering, constants, collider setup and entity interaction logic without launching Celeste or Everest. It is not a runtime dependency, a code generator, or proof of exact parity.

## Safety contract

- The developer provides one explicit absolute installation path; there is no drive, registry or process discovery.
- The builder reads `Celeste.dll`, falling back to `Celeste.exe` only when the DLL is absent, and never writes the installation.
- ILSpy runs as a pinned repository-local .NET tool; no global tool is installed.
- Decompiled files exist only below `local-cache/celeste-reference/<assembly-hash>/`, which Git ignores.
- Decompiled source, IL, commercial assets and real installation paths must never be committed, published, packaged or copied into evidence.
- The manifest contains only logical filename, length, SHA-256, tool identity/version, cache key, source count and negative safety facts. It cannot contain a path or source bytes.
- The builder does not load or execute the selected assembly.

## Workflow and use

Double-click `建立本地行为参考.cmd` and supply the legitimate installation folder. On first use it explains the pinned MIT-licensed ILSpy download from the official NuGet source and asks for consent before restore. Output is immutable per assembly hash; failed staging is removed.

Before changing product behavior, write a project-owned behavior specification or test matrix. Use the local reference as evidence, then implement through this project's own modules and diagnostics. Do not translate files line-by-line or compile decompiled source. Static evidence can strengthen confidence but runtime timing, engine interaction and human feel may remain `partial` or `unknown`.
