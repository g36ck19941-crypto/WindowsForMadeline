# Original-reference-led priority

Developer request: focus on decompilation and original local resources/logic; stop using self-designed behavior as the future fidelity basis. First assess current tooling capability.

Observed: ignored reference cache contains 1369 C# files, including Player, TheoCrystal, Glider, Spring, Refill, Puffer and Seeker. Its manifest reports gameLaunched=false, installationWrites=0 and exactParityEstablished=false. The builder uses ILSpy project output on one selected assembly; it only requires successful decompilation and nonzero C# output. It does not prove complete dependencies, compilability, original source reconstruction or independent runtime behavior.

Historical CDR-016 separately validated five target asset definitions with 93 animations and 706 resolved frames. The reference builder itself does not parse external Atlas, maps or audio. Those results do not establish visible animation or original motion.

This is an assessment/records update only. Existing gameplay modules/commits remain recoverable, no cached source was copied or compiled, and no game/GUI/installation access or publication occurred. The next plan must enumerate original evidence and dependency gaps before replacing behavior. CDR-075 remains acceptance-pending; CDR-076 is no longer the immediate proposed task.
