# CDR-082 — Existing reference follow-up

2026-10-03. Primary. Developer authorized the next read-only existing-XNA/framework inspection after the permission question. No acceptance or publication inferred.

## Scope and measured facts

Fixed installed reference assembly/SDK and named GAC slots only; no drive-wide scan. No game installation read/write this follow-up, downloads, assembly/code execution, GUI or new compilation. Own PEReader metadata mode accepts only seven fixed reference names, retains bounds/reparse guards and exports summaries only.

- Three XNA assemblies found in modern GAC_32: Microsoft.Xna.Framework, Microsoft.Xna.Framework.Game and Microsoft.Xna.Framework.Graphics. Each identity is version 4.0.0.0, neutral culture, token 842cf8be1de50553, matching the original candidate's references.
- Reference directories v4.0/v4.5 exist, but mscorlib/System/System.Core/System.Xml DLL slots are missing. Folder presence is not reference availability.
- v4.7.2 contains all four managed reference assemblies, identity version 4.0.0.0, neutral culture, token b77a5c561934e089, matching the original reference identities. This is NOT proof of an exact .NET Framework 4.5 targeting pack or API/runtime compatibility.
- 15 metadata slots checked: seven managed, eight missing; all seven found file hashes stable on second read. Runtime framework DLL presence was observed separately, not substituted for targeting references. Expected XNA SDK/runtime directories were absent; modern GAC_32 resolved the scoped search instead.
- Previous root/orig-only missing-XNA conclusion remains correct within its scope, not a whole-machine absence assertion. Original build still blocked; no FNA replacement and no compile retry.

## Verification and role

Four new generated reference-mode cases cover fixed-name selection, path/name rejection, missing slots and absence of loading the synthetic assembly. Focused metadata suite 39/39; current no-window regression 317 cases, 26 probe checks and four compile-summary checks. This is dependency diagnostic progress, not a playable feature.

Local raw summaries under artifacts/cdr-082-reference-search contain identities/counts/hashes only; no source/IL/binary payload. Progress page consumes the local summary without scanning the machine again. Original commercial cache stays ignored, never uploaded. Read-only findings do not update acceptance.

## Manual acceptance and next functionality

Use 验证当前版本.cmd: expected retainedCases=317, syntheticProbeChecks=26, compileReportChecks=4, windowProbesExecuted=0. For no-GUI progress generation set CDR_DEMO_NO_OPEN=1 and CDR_NO_PAUSE=1; newly generated HTML describes three matching XNA and four v4.7.2 identities. Do not run the default GUI-opening demo automatically.

Next needs explicit direction to use these existing XNA/v4.7.2 references in a local ignored compile-only experiment and close the original core's source dependencies. No downloads/install/code execution; do not claim v4.7.2 is identical to v4.5. CDR-082 tooling still unaccepted/unuploaded, CDR-083 stepping remains gated.
