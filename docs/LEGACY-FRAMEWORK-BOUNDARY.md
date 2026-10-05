# Legacy framework boundary

2026-10-05. Developer-requested architecture rule, Primary. Documentation only.

Legacy .NET Framework may be used for separately authorized independent compatibility/control tests. It must not enter production feature modules or their direct/indirect runtime dependency chain; a legacy helper process must not host original behavior, character creation, input, animation or rendering. Existing net472 reference compilation, own probes and ignored outputs remain experiments/history, not product integration approval. Results are not deleted or reclassified as passes.

Role: avoid introducing legacy-runtime coupling into product features while allowing experiments to distinguish environmental effects. This rule does not prove a modern migration feasible or remove existing dependencies from original code. The full original-code-led desktop outcome remains. Next assess dependencies/API migration and behavior-preserving boundaries without silently substituting XNA or reviving retired approximate gameplay.

No code, dependency, source transformation or runtime changes in this iteration. No tests executed, GUI/game/system/install/network access or upload. Review the corresponding rule in AGENTS/GOAL and this note; there is no runnable new feature to accept. Next suggested human scope: authorize only a nonlegacy migration design using existing public project contracts and aggregate reports; do not read new dependencies, modify/execute recovered code, run probes or open GUI. Any additional cached-source analysis needs an explicit scope if not already covered.
