# CDR-014 — Synthetic sprite metadata reader

Status: implemented and locally verified; awaiting developer acceptance.

## New functionality

- Added an allowlisted, stream-only, bounded `Sprites.xml` reader and immutable animation/origin/frame-metadata descriptors.
- Added 40 generated XML/API-surface positive and negative checks, including DTD, budgets, duplicates, paths, unknown nodes and hair/carry metadata.
- Extended the cumulative generated Atlas demo to parse two sprite definitions and cross-check their animation paths against two generated atlas entries; kept the double-click demo and verification launchers.

## Role and limit

This is the animation-definition layer above generated atlas indexes and page pixels. It still does not extract sprite frames, animate or render a character, read a real game installation or establish real-install compatibility. `goto` is retained as text, not resolved.

## Evidence

- `tools/Verify-CDR014.ps1`: Release 0 warnings/errors; CDR-010 15/15, CDR-011 27/27, CDR-012 35/35, CDR-013 31/31, CDR-014 40/40.
- Cumulative demo: one page, two entries, 48 exact generated pixels, two sprite definitions, two animations, zero commercial bytes.
- Game/Everest/agent GUI/real install access: none.

## Manual acceptance and next function

Double-click `演示当前进度.cmd`, inspect its generated HTML report, then double-click `验证当前版本.cmd`; see `docs/zh-CN/SPRITE_XML.md` for the Chinese checklist. Acceptance would authorize only CDR-015 synthetic normalized asset-catalog work.
