## Context

The rebuilt Unity 6000.5.7f1 game already contains a `PieceView` flip coroutine, but `PieceView.SetColor` stops any active animation whenever the board is synchronized. `GameController` starts a human transition with `Refresh(true, result)` and immediately calls `BeginAiIfRequired`, which performs `Refresh(false)` for AI-thinking state. The same pattern occurs after an AI move: `Refresh(true, moveResult)` is followed by a `finally` block that calls `Refresh(false)`. Both paths cancel the transition almost immediately. Newly instantiated pieces also skip animation because their displayed color starts empty.

The core AI is snapshot-safe, cancellable, and Alpha-Beta bounded, but it searches one fixed depth in generated move order. It has no iterative-deepening completion boundary, no transposition table, no explicit move ordering, one fixed evaluation across all phases, and one private set of controller limits. Representative depth-four searches currently expand about 228 early, 2,847 middle, and 993 late nodes, so there is room to improve strength while keeping worker-thread bounds and the main thread responsive.

The completed rebuild change is the compatibility baseline. This follow-up must preserve its rules, scene hierarchy, safe-area layouts, cancellation semantics, and Unity 6 package set.

## Goals / Non-Goals

**Goals:**

- Make every human and AI placement/flip transition clearly visible and impossible for HUD refreshes to cancel.
- Allow AI computation to overlap animation without allowing the next logical move to overtake the current presentation.
- Cancel and reconcile both presentation and search on restart, undo, mode/side/difficulty changes, exit, destruction, or stale generations.
- Provide four persistent difficulty profiles with responsive HUD access and safe replacement of in-flight searches.
- Improve search strength per unit time through iterative deepening, move ordering, bounded caching, phase-aware evaluation, and bounded exact endgames.
- Preserve legal results, deterministic testability, bounded resources, and pure-core independence from Unity.

**Non-Goals:**

- Solve Reversi perfectly from the opening or claim tournament-engine strength.
- Rewrite all rules around a bitboard representation in this change.
- Add online difficulty tuning, machine learning, opening-book downloads, or cloud persistence.
- Make animation timing affect core rules, AI evaluation, saved history, or undo semantics.
- Guarantee that a stronger profile wins every individual game; strength is statistical while correctness remains deterministic.

## Decisions

### Introduce a cancellable presentation barrier instead of delaying AI computation

`BoardView` will expose an asynchronous main-thread presentation operation for one `MoveResult`. It will animate the placed piece and captured pieces and complete only after the final scheduled transition. `PieceView` will expose cancellable placement and flip operations that snap to an explicit authoritative color/transform on cancellation.

The controller will start AI search from the post-human immutable snapshot as soon as the human move is accepted, concurrently with the human presentation. It will await both the presentation and the current search before applying the AI result. After applying the AI move it will await the AI presentation before reopening human input or advancing spectating.

```text
Core applies human move
        |---------------- AI snapshot search ----------------|
        |---- placement + staggered flips ----|              |
                                               join current generation
                                                        |
                                                Core applies AI move
                                                        |
                                               AI move presentation
                                                        |
                                            human input / next spectator turn
```

This is preferred over adding a fixed delay before starting AI because it keeps available compute time useful. It is also preferred over allowing AI to apply immediately and merely lengthening the flip duration, because two logical moves can affect the same piece and make the first transition unreadable.

### Separate authoritative board synchronization from HUD refresh

`GameController.Refresh` will be split into explicit presentation responsibilities: full board synchronization for restart/undo/recovery, move presentation for a `MoveResult`, and HUD/audio refresh for status-only changes. AI-thinking and difficulty label updates will no longer call `BoardView.Synchronize`.

The controller will add `isPresentingMove` to input gating. A controller-owned turn-work generation and linked cancellation token will cover presentation plus AI work. Existing `AiRequestService` generation validation remains the final guard against stale worker results.

This is preferred over teaching `PieceView.SetColor` to ignore every same-color refresh. That local safeguard is still useful, but it cannot prevent a fast AI move from overtaking a prior animation or solve controller input timing.

### Use short placement easing and distance-based flip staggering

New pieces will scale from zero through a small overshoot to their normal scale. Captured pieces will rotate 180 degrees around the existing disc flip axis and change material at the midpoint. Flip starts will be staggered by board distance from the placed coordinate, with a configured maximum total presentation duration so large captures remain readable without making the game sluggish.

Initial tuning targets are approximately 160–200 ms for placement, 240–300 ms per flip, 30–45 ms distance staggering, and no more than about 500 ms for the complete move. Exact values remain serialized and will be tuned through PlayMode and visual checks.

### Model difficulty as core profiles with a Unity preference adapter

Add a core `AiDifficulty` enum and immutable `AiDifficultyProfile` mapping. Each profile supplies maximum depth, nodes, elapsed time, transposition capacity, exact-endgame threshold, and root-choice variation. Initial profiling targets are:

| Profile | Max depth | Nodes | Think time | Exact endgame | Root choice |
| --- | ---: | ---: | ---: | ---: | --- |
| Easy | 2 | 5,000 | 120 ms | disabled | seeded choice from top three with controlled mistake chance |
| Normal | 6 | 60,000 | 350 ms | disabled | best completed score |
| Hard | 8 | 250,000 | 900 ms | 10 empties | best completed score |
| Expert | 10 | 750,000 | 1,800 ms | 12 empties | best completed score |

These values are starting policies, not assumed performance results; representative profiling may tune them while retaining ordered budgets. Normal is the default for invalid or missing data. A small HUD control will cycle profiles and persist through a dedicated preference abstraction. Difficulty changes invalidate active search and start one replacement only when AI owns the current turn.

### Use iterative deepening as the authoritative result boundary

The search will complete root iterations from depth one upward. Root scores and the selected move become publishable only when all legal root moves for that depth complete. If time or nodes expire during the next iteration, the result from the deepest completed iteration is retained. If even depth one cannot finish, a deterministic ordered legal fallback is returned.

`AiSearchResult` will add completed depth and diagnostics such as cache hits. This makes time-limited behavior comparable and avoids treating a partially searched root candidate as equivalent to candidates that were never visited.

### Add deterministic move ordering and a request-local bounded transposition table

Before recursion, moves will be ordered by a stable heuristic: cached best move first, corners, safer positional weights, mobility reduction, and coordinate order as the deterministic tie-break. Better ordering increases Alpha-Beta cutoffs without changing correctness.

Each request receives a capacity-bounded transposition table keyed by an exact compact position identity containing black occupancy, white occupancy, and active color. Entries store searched depth, score, bound type, and best move. Replacement favors deeper/current-generation entries. Request-local ownership avoids locks between searches and guarantees stale requests cannot contaminate later choices.

An exact occupancy key is preferred over the existing 32-bit `GetHashCode`, whose collision rate is unsuitable for pruning. A wholesale bitboard rules rewrite is deferred; occupancy masks may be derived or cached on immutable `BoardState` only for identity and evaluator efficiency.

### Make evaluation phase-aware and keep exact endgames bounded

The evaluator will retain corner dominance, positional safety, mobility, and stable-edge signals while adding frontier exposure and parity. Weights will vary by empty-square phase: mobility/corner safety dominate early, mobility/stability/frontier dominate middle, and disc difference/parity dominate late. Corner-adjacent penalties apply only while the corresponding corner is empty.

Hard and Expert will attempt search-to-terminal within their empty-square threshold. Exact mode still obeys the profile's time, node, and cancellation limits; if it cannot complete, iterative deepening returns the last fully completed result rather than a partial terminal claim.

### Keep Easy variation controlled and legal

Easy may select from a small shortlist only after a complete root iteration. A seeded policy gives the best move the highest probability and occasionally selects another top candidate. Normal, Hard, and Expert choose among equal best scores only. This creates an approachable mode without injecting illegal moves or randomizing internal recursion, which would undermine pruning reproducibility.

## Risks / Trade-offs

- **Async presentation completion could outlive scene objects** → Link every operation to controller lifetime/turn cancellation, cancel in `OnDestroy`, and make completion idempotent.
- **Fast restart or undo could leave partial transforms** → Provide one cancel-and-snap full synchronization path that resets scale, rotation, material, hints, and input state.
- **Animation barriers could make spectating slow** → Bound each presentation and treat any additional spectating pause as pacing after, not instead of, animation.
- **Transposition entries could return invalid bounds because of key collision** → Use exact black/white occupancy plus turn identity rather than a 32-bit hash, and keep entries request-local.
- **Larger difficulty budgets could use excessive CPU on mobile** → Enforce time/node/cache caps, profile on the installed editor, document starting values, and avoid unbounded exact search.
- **A stronger evaluator may differ from legacy move choices** → Preserve strategic intent rather than exact randomized choices and add tactical fixtures plus deterministic benchmark reports.
- **Difficulty UI could crowd verified layouts** → Place a compact control in the status area, regenerate the scene, and repeat portrait/landscape/16:9/4:3 captures.

## Migration Plan

1. Add core difficulty/profile/preferences and iterative-search diagnostics behind tests while retaining the current Normal-equivalent behavior.
2. Implement iterative deepening, ordering, bounded transposition storage, phase evaluation, and endgame mode; tune profiles from repeatable position suites.
3. Refactor board/HUD refresh paths and add cancellable placement/flip presentation primitives.
4. Coordinate controller turn work so AI search overlaps human animation but move application respects presentation barriers.
5. Add the difficulty HUD control, preference adapter, and builder wiring; regenerate and validate `Game.unity`.
6. Run EditMode, PlayMode, batch Validator, AI profiling, full-game benchmarks, and four-aspect visual checks.

Rollback is a Git revert of this follow-up change. No board/history data migration is required; an absent new difficulty preference resolves to Normal.

## Open Questions

- Final profile budgets and animation timings remain subject to measurements on Unity 6000.5.7f1; the ordered policies and bounded behavior are fixed, while exact numeric tuning may change during implementation.
