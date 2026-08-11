# AI Search Performance

The Reversi AI runs against immutable `BoardState` snapshots on a worker task. Search uses iterative deepening, stable move ordering, phase-aware evaluation, a request-local direct-mapped transposition table, cancellation checks, and the deepest fully completed root iteration as its publication boundary.

## Difficulty policies

| Profile | Maximum depth | Node limit | Think time | Cache entries | Exact endgame | Root choice |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Easy | 2 | 5,000 | 120 ms | 2,048 | disabled | seeded weighted choice among the top three fully scored moves |
| Normal | 6 | 60,000 | 350 ms | 16,384 | disabled | best completed score |
| Hard | 8 | 250,000 | 900 ms | 65,536 | 10 empties | best completed score |
| Expert | 10 | 750,000 | 1,800 ms | 131,072 | 12 empties | best completed score |

All limits are hard search policies. Cancellation and generation invalidation cover restart, undo, mode/side/difficulty changes, exit, scene destruction, and newer requests. Hard and Expert terminal search still falls back to the last complete ordinary iteration if a limit interrupts the exact attempt.

## Representative positions

`ReversiAiBenchmark.RunFromCommandLine` profiles deterministic positions after 0, 24, and 48 plies. The 2026-08-11 Unity 6000.5.7f1 run on the migration Mac recorded:

| Profile | Stage | Move | Completed depth | Nodes | Time | Cache hits | Limit |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Easy | early | (2,3) | 2 | 20 | 6 ms | 0 | No |
| Easy | middle | (7,4) | 2 | 86 | 11 ms | 0 | No |
| Easy | late | (1,1) | 2 | 64 | 8 ms | 0 | No |
| Normal | early | (2,3) | 5 | 1,937 | 350 ms | 10 | Yes |
| Normal | middle | (4,7) | 4 | 2,739 | 350 ms | 6 | Yes |
| Normal | late | (7,1) | 6 | 6,420 | 236 ms | 135 | No |
| Hard | early | (2,3) | 7 | 9,424 | 902 ms | 48 | Yes |
| Hard | middle | (5,7) | 5 | 15,831 | 900 ms | 81 | Yes |
| Hard | late | (0,3) | 8 | 26,961 | 552 ms | 859 | No |
| Expert | early | (2,3) | 7 | 20,716 | 1,820 ms | 116 | Yes |
| Expert | middle | (5,7) | 6 | 29,529 | 1,800 ms | 225 | Yes |
| Expert | late | (0,2) | 10 | 183,491 | 1,800 ms | 7,998 | Yes |

The small elapsed-time overshoot reflects the outer measurement and Unity scheduling; search checks the configured deadline throughout ordering, evaluation, cache access, pass handling, and recursion. Gameplay remains responsive because these requests execute off the Unity main thread.

## Deterministic full games

| Black | White | Result | Score | Plies | Total time | Black nodes / hits / max depth | White nodes / hits / max depth |
| --- | --- | --- | --- | ---: | ---: | --- | --- |
| Easy | Normal | White wins | 21–43 | 60 | 7,999 ms | 3,001 / 0 / 2 | 122,588 / 363 / 6 |
| Hard | Expert | White wins | 15–49 | 60 | 63,671 ms | 341,963 / 3,941 / 10 | 747,782 / 15,750 / 11 |

These games are repeatable observations, not deterministic strength assertions. Win rate requires a larger color-balanced corpus. Correctness tests instead assert legal moves, complete-depth fallback, bounded cache and resources, cancellation, tactical corners, pass handling, and exact endgames.

## Legacy comparison

The pre-change fixed-depth profile expanded 228 early, 2,847 middle, and 993 late nodes at depth four. With ordering and caching enabled, the compatibility depth-four suite measured 247 early, 1,664 middle, and 919 late nodes in the first post-change EditMode run; the middle position used about 42% fewer nodes while evaluating richer phase signals.
