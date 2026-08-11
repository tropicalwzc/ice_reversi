## Why

The current piece flip coroutine is repeatedly cancelled by non-animated board refreshes, so human and computer moves appear to jump directly to their final state even though animation code exists. The AI also exposes only one fixed search budget, leaving no user-facing difficulty choice and leaving search strength and late-game efficiency below what the current background snapshot architecture can support.

## What Changes

- Make placed pieces visibly enter and captured pieces complete a clear, optionally staggered flip transition for both human and computer moves.
- Coordinate move presentation with AI computation so search may run concurrently, but an AI result is not applied until the preceding animation finishes and input is not reopened until the computer animation finishes.
- Keep restart, undo, side changes, spectating changes, exit, and newer generations able to cancel pending animation or AI work immediately.
- Add persistent Easy, Normal, Hard, and Expert AI difficulty choices to the HUD without compromising the verified responsive layouts.
- Replace fixed-depth root search with bounded iterative deepening and retain the best fully completed iteration when time or node limits are reached.
- Improve Alpha-Beta efficiency with move ordering and a bounded transposition table, strengthen phase-aware evaluation, and add exact late-game search where the selected difficulty budget permits.
- Add animation sequencing, difficulty persistence, tactical AI, legality, cancellation, performance, and stronger-versus-weaker benchmark coverage.

## Capabilities

### New Capabilities

- `reversi-move-presentation`: Complete, cancellable placement and flip animation sequencing for human, AI, and spectating moves, including presentation input gating.
- `reversi-ai-difficulty`: Persistent user-selectable AI profiles and stronger bounded search behavior with iterative deepening, ordering, caching, phase evaluation, and late-game solving.

### Modified Capabilities

None. The previously completed rebuild change has not yet published canonical specs under `openspec/specs`; these follow-up behaviors are therefore captured as new capabilities.

## Impact

- Affects `PieceView`, `BoardView`, `GameController`, `GameHud`, the Editor scene builder, generated `Game.unity`, and presentation-focused PlayMode tests.
- Affects `AiSearchOptions`, `AiSearchResult`, `ReversiAi`, preference storage, AI profiling documentation, and EditMode AI tests/benchmarks.
- Preserves `BoardState`, `ReversiRules`, legal move application, immutable AI snapshots, cancellation, stale-generation rejection, existing controls, and the Unity 6000.5.7f1 project baseline.
- Adds no external runtime package dependency; all work remains in the existing core, Unity presentation, Editor, and test assemblies.
