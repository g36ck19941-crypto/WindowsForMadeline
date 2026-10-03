# CDR-080 Acceptance — retirement and local recomposition preparation
Owner: Primary. State: offline verified, developer acceptance pending; not uploaded.
Removed 24 projects and dependent self-designed gameplay source/tests, demo fragments and obsolete verification scripts. Retained tools: install/asset safety, worker/parsers/catalog, animation/rendering, anonymous desktop geometry, extension data contracts and local-reference builder.
Rollback: local branch archive/self-designed-logic-before-removal-20261003 at e0ba567529dfc014bdeb15c5b46cbb306f045fd7.
Evidence: Release zero warnings/errors, 12 suites / 298 passes; current report cacheCount=1, sourceFiles=1369, modNamedFiles=256, runtimeIntegrated=false. No game, visible GUI, live input or installation access/write.
Both root cmd launchers passed with CDR_NO_PAUSE=1 and CDR_DEMO_NO_OPEN=1; retired folders containing only bin/obj outputs were also removed after containment and reparse checks. These generated files can be rebuilt from archived sources. Source/history rollback remains available.
Role: remove the competing gameplay baseline and keep independently useful tools ready for original-code-led local assembly.
How to accept: run root progress and verification cmd entries; expect a status-only report and CDR080_VERIFIED suites=12 passed=298. No original character should be claimed present.
Next proposed feature: CDR-081 source identity/dependency inventory. Commercial contents and derived builds remain local ignored; acceptance upload applies only to tools/records, not cached sources.
See ../ORIGINAL_RECOMPOSITION.md for phased dependencies and remaining gates.
