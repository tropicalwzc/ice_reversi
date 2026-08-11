# Legacy Reversi AI behavior

The original AI is implemented inside `Assets/mousecatch.cs`. This note records the behavior being preserved conceptually while removing its unsafe threading and scene coupling.

## Evaluation signals

- Immediate flips: `100` per captured piece
- Mobility difference: `3000` per legal-move advantage
- Low-mobility penalty: `-40000`
- Stable pieces: `100007` per estimated stable piece
- Edge-adjacent gamble squares: `-17000`
- Favorable edge squares: `9000`
- Diagonal corner-adjacent X squares: `-300000`
- Corners receive a dominant `10000000` positional score
- A special edge pattern contributes approximately `100005` per detected pattern
- Terminal states scale the piece difference by approximately `20000007`

The legacy evaluator searches approximately five plies for each root candidate, reduces depth on unfavorable mobility branches, and extends late-game search. Equivalent candidate scores receive a small random value before selection.

## Migration constraints

- Preserve the preference for corners, mobility, stable edges, and avoiding X/C squares.
- Preserve legal move selection and pass behavior; exact moves are not promised when several candidates are tied.
- Evaluate an immutable board snapshot and never read or mutate live Unity objects from a worker thread.
- Bound depth, elapsed time, and expanded nodes so the UI remains responsive.
- Replace shared counters, unsynchronized string writes, raw `Thread`, and `Thread.Abort` with cancellation and stale-generation rejection.

## Known legacy defects not preserved

- `stable_point_number` mixes the supplied simulation board with the live global board and writes several vertical results to the wrong coordinate.
- Worker threads mutate shared counters, score maps, and diagnostic strings without synchronization.
- A timed-out search aborts threads and can consume a partially written score map.

