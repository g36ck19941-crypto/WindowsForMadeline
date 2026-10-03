# CDR-081 prerequisite preflight

Date: 2026-10-03. Owner: Primary. Scope: existing cache metadata and filenames, not implementation or an acceptance claim.

The new thread goal is confirmed active. CDR-080 remains developer-acceptance pending, so this preflight does not bypass the ordered gate or begin original runtime integration.

Measured cached facts:
- Assembly recorded by the cache builder: Celeste.dll.
- Recorded SHA-256: 54EB42784C99480D3C95DB5B2B9F18FDA3AE4460BE049DC245EF2FCD1D66916D. Not independently rehashed against installation in this turn.
- Actual cached C# file count: 1369.
- Generated project target: net8.0; 17 reference entries, 15 unique names. Duplicate entries: FNA and Steamworks.NET.
- Names include FNA, NETCoreifier, MonoMod.Patcher, MonoMod.RuntimeDetour, MonoMod.Utils, MonoMod.Core, Mono.Cecil, Steamworks.NET, Jdenticon, MAB.DotIgnore, YamlDotNet, DiscordGameSDK, Newtonsoft.Json, NLua and KeraLua. Actual reference availability/version compatibility not checked.
- All 19 queried filenames exist: Player, Actor, Solid, Entity, Scene, StateMachine, Sprite, PlayerHair, Input, Engine, Session, TheoCrystal, Glider, Spring, Refill, Water, Bumper, Puffer and Seeker. Names do not prove type semantics, dependency closure or successful compilation.
- Cached metadata and generated project are confirmed Git-ignored.

Not established: assembly version identity, pristine vanilla source, dependency closure, successful original compilation or runtime behavior. Mod-bearing filenames and reference names require source selection, not blind integration.

Next gate: CDR-080 acceptance, followed by explicit read-only installation identity/dependency inspection. Use metadata/file identity checks without loading or executing game assemblies; check original candidates only within the authorized installation. If no clean original candidate can be verified, ask whether to obtain a clean original installation or explicitly target the modded edition. Do not silently choose.

No game/Everest/GUI/live input/install access or writes, no original source export, no upload in this preflight.
