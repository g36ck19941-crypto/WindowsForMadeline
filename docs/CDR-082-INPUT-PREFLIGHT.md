# CDR-082 — Authorized original input/time test preflight

2026-10-03. Primary. Developer now explicitly permits limited original input/time testing using cache and generated input only, no Player, game/GUI, live input, Steam/audio, installation/new assets or original-source modification. Stop on dependency/side-effect doubt.

## Current measured result: stopped before execution

Read existing project cache inventory and cached source only. Five cached OriginalCoreCompileProbe outputs were listed; no Microsoft.Xna.Framework / Microsoft.Xna.Framework.Game / Microsoft.Xna.Framework.Graphics runtime DLL was found in the project cache DLL inventory. Previous evidence located the three XNA metadata identities in system GAC, not project cache. That earlier permission established compile references, not runtime-loading or private caching permission for those system files.

Original Engine inherits XNA Game; original input states use XNA input data types. A cached recovered library is not a self-contained runtime. Silently relying on GAC/default CLR resolution would extend the current cache-only boundary and leave external initialization/native effects unreviewed. No attempted original assembly load, constructor, initialization, input update or time setter occurred. No installation/system-XNA reread/copy, original compilation retry, test runner or GUI was started.

This is a permission/dependency preflight stop, not a failed or passed original-input test. Original-bound/time/input/runtime evidence remains absent. The source check also confirms MInput initialization/device polling/virtual-input updating are distinct, but does not establish transitive runtime safety.

## Exact remaining decision

Confirm whether the three already-found system XNA files may be read/copied into ignored local cache and statically audited for the limited runtime dependency scope. Copying/auditing alone does NOT prove or authorize all native initialization. Runtime use must be explicitly limited to reviewed input value types/required managed dependencies; any unexpected native/window/device/Steam/audio path stops before original invocation. No fresh installation read or new download is proposed.

Alternative: identify an existing approved cache containing those exact runtime assemblies. Do not use reference-only BCL assemblies as runtime implementations or silently substitute FNA. Framework CLR availability/runtime binding and relevant external initialization remain gates, not inferred from net472 compilation.

## Role and acceptance

This record explains why the newly authorized original test has not run and prevents an accidental external dependency load. It adds no runtime feature or synthetic pass claim; previous CDR-082 compile/adapter evidence remains separate. No commercial bytes/source excerpt included, upload still pending developer acceptance.
