# CDR-050 — Offline App orchestration

## Added

- Added a lifecycle-owned App session with start, pause, resume, stop, fault and dispose transitions.
- Enforced one simulation step per App tick and stable entity/effect ordering.
- Routed velocity, resource and holder fall-limit effects to Player, Theo and Glider without moving gameplay rules into App.
- Isolated optional entity and presentation failures with stable events and full exception details.
- Connected the verified offline animation/presentation contract after simulation.
- Added 50 focused tests and a four-tick cumulative generated demo.

## Verification

- Focused App tests: 50/50.
- Complete offline regression target: 800/800.
- Visible GUI, live input, installation access and commercial bytes: 0.

## Role in the project

CDR-050 is the coordinator between the existing parts. It proves they can advance as one deterministic application session and survive an isolated presentation failure. It does not prove visible desktop output, real input, commercial assets or original parity.

## Acceptance

Double-click `演示当前进度.cmd`, inspect the CDR-050 table for App tick = World tick from 1 through 4, one isolated presentation failure, continued simulation and final stopped state. Then double-click `验证当前版本.cmd` and require `CDR-050 OFFLINE VERIFICATION PASSED` with 800/800. Do not treat either result as human-visible acceptance.
