# CDR-081 read-only assembly identity and dependency inventory

Owner: Primary. This contract implements the developer's explicit read-only installation authorization of 2026-10-03; it does not authorize decompilation, recovered-code compilation or runtime execution.

## Input and isolation

The command requires an explicitly supplied absolute installation root and an absolute output file outside it. No installation auto-discovery. Candidate slots are exactly Celeste.dll, Celeste.exe, orig/Celeste.dll and orig/Celeste.exe. Dependency lookup checks only direct root/orig DLLs with validated assembly names; exact matches may be traversed transitively. Content, Saves, Mods, log contents and directory links are never traversed. Root, ancestor, candidate and output reparse points are rejected.

Reads use FileMode.Open, FileAccess.Read, FileShare.Read. PEReader/MetadataReader parse PE/CLR metadata; no Assembly.Load, game entrypoint, runtime instantiation, method-body extraction or native library loading. No install writes or network restore. Maximum file size 32 MiB, cumulative reads 128 MiB, 512 cached lookup slots, 100000 type/method rows per file and 256 assembly-reference rows. This is trusted-local-install metadata tooling, not a sandbox for hostile assemblies. Microsoft's [metadata reader guidance](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.metadatareader) warns against treating malformed/untrusted metadata as safely handled in all circumstances.

## Report

JSON contains fixed relative slots, SHA-256/length, assembly name/version/culture/key token, target-framework and metadata-version strings, machine kind, type counts, fixed qualified-type presence facts, mod-marker counts, reference identities and native module names. No source, IL, resource bytes or absolute installation paths.

No Mod marker is classified as unmodified-candidate-not-authenticated, not pristine vanilla. AssemblyVersion does not establish the game's displayed version. Mod reference/type indicators establish mod-bearing metadata, not every patch or its provenance.

Dependency status exact-metadata-match requires name, version, culture and public-key-token equality. Report all root/orig matches; never quietly substitute FNA for XNA. Identity-mismatch, missing-or-unreadable and framework-not-inspected are distinct; missing only means absent/unreadable within the authorized root/orig scope. Neither managed identities nor native-module names prove loader binding, ABI compatibility, operating-system framework availability or semantic dependency closure. CompilationEstablished, DependencyClosureEstablished and PristineVanillaEstablished remain false.

## Diagnostics and developer entries

ASSEMBLY_INVENTORY_COMPLETED records candidate/node counts and assemblyExecuted=false. ASSEMBLY_INVENTORY_FAILED records phase/code plus exception type, message, HResult, stack and inner exception with selected argument paths redacted. Candidate unreadable/invalid results retain redacted exception details. PROGRESS_REPORT_FAILED distinguishes summary errors.

检查原版来源与依赖.cmd prompts for the explicitly selected installation and generates only local report artifacts. It does not open a GUI. 演示当前进度.cmd consumes a previous report (if available), clearly dated, and opens a summary page only on the developer's invocation; it never re-reads installation automatically. 验证当前版本.cmd runs synthetic tests and report checks only, never the real installation.

## Observed local result and next gate

Current root Celeste.dll matches the old cache hash and is mod-bearing/.NET 8. orig/Celeste.exe has no tested Mod markers, is .NET Framework 4.5, contains 19 of 20 queried qualified type names (Actor is Celeste.Actor, not Monocle.Actor), and directly references three XNA assemblies not found in root/orig. This candidate is not authenticated against an official clean baseline. See docs/evidence/CDR-081.json and bilingual acceptance records.

Next proposed CDR-082 requires an explicit source choice and authorization to recover/decompile the chosen original candidate into ignored local storage and attempt a minimal local compile. Keep original behaviors; any framework/engine compatibility adapter must be explicit and tested, never a guessed replacement controller. No execution of the full game or original runtime is authorized by this inventory task.
