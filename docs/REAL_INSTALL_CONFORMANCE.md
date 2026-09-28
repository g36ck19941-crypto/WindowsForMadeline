# CDR-016 Real-install conformance

## Outcome

CDR-016 passed against the developer-selected正版 Windows installation on 2026-09-28. The opt-in verifier opened only the three required resource files in read-only mode, built the selected catalog twice, decoded the required Atlas page twice, and compared the source snapshots before and after.

The safe evidence contains hashes and counts only. It contains no selected absolute root, pixels, XML, Atlas payload, executable content or other commercial bytes.

## Observed compatibility

- 3 source files; 1 Atlas page; 6,824 Atlas entries.
- 5 explicitly selected definitions: player, Theo Crystal, Glider, Bumper and Puffer.
- 93 animations and 706 resolved frames.
- decoder and catalog fingerprints were stable across repeated work.
- source length, last-write time and SHA-256 were unchanged before and after.
- game launches, GUI launches, installation writes and persisted commercial bytes: 0.

The real files required four compatibility corrections that now have synthetic regression coverage: page names whose metadata omits `.data`, animation paths with one trailing separator, case-insensitive Atlas lookup that preserves original spelling, and numeric frame suffixes with variable padding. Sprite metadata can also bind through a shared Atlas path and can represent an empty hair table.

## Developer verification

Double-click `验证当前版本.cmd` for the 191 offline regression cases. This path does not read a real installation.

For an explicitly selected installation, drag its folder onto `验证真实安装兼容性.cmd` or double-click the launcher and paste the folder path. Expected final lines are `CDR-016 REAL CONFORMANCE PASSED`, `installation_writes=0` and `commercial_bytes_persisted=0`. The report is written under `artifacts/cdr-016-verification`, outside the installation.

## Role in the project

This task closes the gap between synthetic test data and one real, user-selected installation: it proves the current readers can understand that installed resource set and resolve the five selected entity definitions deterministically. It gives later rendering work a verified catalog input.

It does not prove that any character is visible, animated correctly or feels like Celeste. It does not validate every game version, every entity, levels, Mods, audio, simulation, rendering or desktop integration.

## Next gate

CDR-020 starts the pure deterministic Actor/Solid simulation kernel using generated geometry only. CDR-016 remains local and must not be uploaded until the developer accepts it.
