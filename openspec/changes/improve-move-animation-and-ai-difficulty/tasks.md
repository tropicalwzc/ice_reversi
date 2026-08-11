## 1. Establish Regression Baselines

- [x] 1.1 Record the current Git status, Unity 6000.5.7f1 Validator result, 28 EditMode results, 3 PlayMode results, and current early/middle/late AI metrics before modifying behavior
- [x] 1.2 Add deterministic tactical board fixtures for corner capture, unsafe corner adjacency, forced pass, mobility trade-offs, and representative exact endgames
- [x] 1.3 Add a PlayMode regression that demonstrates a human or AI flip coroutine is not cancelled by an AI-thinking/HUD-only refresh

## 2. Add Difficulty Profiles and Persistence

- [x] 2.1 Implement the pure-core `AiDifficulty` enum and immutable Easy, Normal, Hard, and Expert profile mapping with ordered depth, node, time, cache, endgame, and root-choice policies
- [x] 2.2 Extend the local preference abstraction and Unity PlayerPrefs adapter to save/load difficulty with Normal fallback for missing or invalid values
- [x] 2.3 Extend `AiSearchOptions` and `AiSearchResult` with exact-endgame, cache-capacity, completed-depth, cache-hit, and root-score diagnostics while preserving existing call-site compatibility where practical
- [x] 2.4 Add EditMode tests for profile ordering, profile values, cycling order, persistence, and invalid-value fallback

## 3. Improve the Bounded AI Search

- [x] 3.1 Add an exact compact position identity containing black occupancy, white occupancy, and active color without introducing a Unity dependency or changing board immutability
- [x] 3.2 Refactor root search to iterative deepening and publish only the deepest fully completed root iteration, with a deterministic legal fallback when depth one cannot complete
- [x] 3.3 Add stable move ordering using cached best moves, corners, positional safety, opponent mobility, and coordinate tie-breaking before Alpha-Beta recursion
- [x] 3.4 Implement a request-local capacity-bounded transposition table with depth, score, bound type, and best-move entries plus deterministic replacement behavior
- [x] 3.5 Replace the phase-independent evaluator with early/middle/late weights for corners and corner safety, mobility, stable edges, frontier exposure, disc difference, and parity
- [x] 3.6 Add bounded search-to-terminal behavior for Hard and Expert positions inside their configured empty-square thresholds, falling back to the last completed iterative result when limits interrupt exact search
- [x] 3.7 Implement Easy's seeded controlled selection from a fully scored legal shortlist while keeping Normal, Hard, and Expert on the best completed score
- [x] 3.8 Ensure cancellation and node/time checks cover ordering, evaluation, transposition access, iterative boundaries, pass recursion, and exact endgame recursion
- [x] 3.9 Add EditMode tests for completed-depth fallback, deterministic ordering, cache hits/capacity, tactical fixtures, endgame outcomes, every profile's legality, and stale/cancelled request rejection
- [ ] 3.10 Profile the position corpus and tune the four starting policies so budgets remain ordered and the main thread remains responsive on Unity 6000.5.7f1

## 4. Build Complete Move Presentation

- [x] 4.1 Refactor `PieceView` to provide cancellable placement easing and flip animation completion, preserve material changes at flip midpoint, and snap scale/rotation/material to authoritative state on cancellation
- [x] 4.2 Implement `BoardView` move presentation that creates the placed piece, schedules distance-based flip staggering, returns one completion operation, and enforces a bounded total transition duration
- [x] 4.3 Add an explicit BoardView cancel-and-synchronize recovery path for restart, undo, side/mode changes, exit, scene destruction, and newer presentation generations
- [x] 4.4 Prevent same-color or HUD-only refreshes from restarting or cancelling a current valid transition while still allowing a newer authoritative move to supersede it
- [x] 4.5 Add PlayMode tests for placement easing, material midpoint, multi-line/distance staggering, final piece colors, bounded duration, cancellation, and full recovery synchronization

## 5. Coordinate Turns, Difficulty, and HUD

- [x] 5.1 Split `GameController` board synchronization, move presentation, HUD refresh, and audio feedback paths so status-only changes never touch active piece animations
- [x] 5.2 Add controller turn-work generation, linked cancellation, and `isPresentingMove` input gating shared by presentation and AI orchestration
- [x] 5.3 Start AI snapshot computation concurrently with the human move transition, retain a fast result as pending, and apply it only after both work items complete for the current generation
- [x] 5.4 Keep the AI move presentation visible to completion before reopening human input or advancing the next AI-versus-AI turn
- [x] 5.5 Make restart, undo, human-side changes, spectating changes, exit, and destruction cancel presentation plus AI work and reconcile the scene to the authoritative snapshot
- [x] 5.6 Implement difficulty cycling in `GameController`, persist the selection, update the HUD immediately, and replace at most one in-flight AI request without changing the current board/history
- [x] 5.7 Add a compact `AI: <Difficulty>` HUD control that remains readable in the safe area without displacing existing essential actions
- [ ] 5.8 Update the Editor builder and Validator to wire and require the difficulty control, regenerated scene references, and new presentation configuration
- [x] 5.9 Synchronize placement/flip audio with visible presentation milestones and retain null-safe optional clips

## 6. Verify Strength, Presentation, and Compatibility

- [ ] 6.1 Run all EditMode tests under Unity 6000.5.7f1 and fix every rules, difficulty, iterative-search, cache, tactical, cancellation, or bound failure
- [ ] 6.2 Run all PlayMode tests and verify human and AI placement/flips, pending fast AI results, input gating, pass, undo/restart cancellation, side changes, difficulty changes, spectating, exit, and game over
- [ ] 6.3 Run deterministic position and full-game benchmark suites across all four profiles and document completed depth, nodes, elapsed time, cache hits, selected moves, and outcomes without treating win rate as a deterministic assertion
- [ ] 6.4 Capture and inspect portrait, landscape, 16:9, and 4:3 Game views and correct any board, difficulty-label, score, result, or action overlap
- [ ] 6.5 Run the idempotent Builder twice and the batch Validator once, confirming `Game.unity` remains the only startup scene with no duplicate objects, missing references, scripts, materials, or external dependencies
- [ ] 6.6 Update README controls and `Docs/AiPerformance.md` with difficulty behavior, animation timing, final profile budgets, benchmark methodology, and measured representative results
