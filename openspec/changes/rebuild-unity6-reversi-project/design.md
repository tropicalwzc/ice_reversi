## Context

The repository currently tracks eight legacy C# files plus generated Unity 2019 solution artifacts and a partial `Library`, but it has no `Packages`, `ProjectSettings`, metadata, prefabs, or scenes. The primary implementation is a 1,521-line `mousecatch` MonoBehaviour that mixes rules, AI, raw threads, input, persistence, scene lookup, animation, audio, and IMGUI.

A separate local project at `/Users/wangzicheng/CocosWorkSpace/ice-reversi/ice-reversi` contains the original 2019 scene and approximately 4 MB of textures, audio, fonts, materials, and prefabs. It has been imported by Unity 6000.5.7f1, but its runtime scene is not in Build Settings, its tag registry is incomplete, one serialized audio reference is missing, and its materials and scene composition retain legacy assumptions. It is useful as a read-only asset and visual reference, not as the target project.

The installed editor is Unity 6000.5.7f1. No platform PlaybackEngine module was detected, so the first deliverable must be verifiable in the macOS Unity Editor without requiring an iOS or Android build.

## Goals / Non-Goals

**Goals:**

- Make the current repository a clean, self-contained Unity 6000.5.7f1 project.
- Preserve observable Reversi rules, AI play, undo, restart, side selection, and spectating while making those behaviors testable.
- Rebuild the main game scene and its assets using current Unity components and explicit serialized references.
- Support mouse and touch through one input path and provide responsive safe-area-aware UI.
- Generate and validate the scene through repeatable Unity Editor automation.

**Non-Goals:**

- Recreate the multi-board-game launcher text found in `start.cs` or migrate unrelated Go, Gomoku, Chinese chess, and Sudoku assets.
- Preserve the exact serialized hierarchy, GUIDs, fixed world coordinates, IMGUI layout, or visual defects of `jumping.unity`.
- Redesign the AI strategy or guarantee identical move selection when legacy random tie-breaking produced multiple valid answers.
- Produce a signed iOS/Android build or install additional Unity platform modules.
- Add online multiplayer, accounts, analytics, ads, or cloud saves.

## Decisions

### Seed the project from the installed Unity 6 cross-platform 3D template

The project foundation will be generated for Unity 6000.5.7f1 using the locally installed cross-platform 3D template, with package versions pinned in `Packages/manifest.json`. URP, UGUI, and Input System will be retained; unrelated sample/tutorial and multiplayer packages will be removed when not required.

This is preferred over upgrading the incomplete 2019 repository because there are no authoritative project settings or metadata to upgrade. It is also preferred over copying the sibling project wholesale because that would preserve stale scene state and unrelated assets.

Generated IDE files, `Library`, `Temp`, `Logs`, `obj`, and build output will not be source-controlled. The legacy tracked solution and `Library/ScriptAssemblies` files will be superseded by Unity-generated equivalents.

### Split pure game logic from Unity presentation

The runtime will have two dependency directions:

```text
Reversi.Core
  BoardState -> ReversiRules -> GameSession -> ReversiAi
                                      ^
                                      |
Reversi.Unity
  GameController -> BoardView / HudView / AudioController / Input
```

`Reversi.Core` will contain no `UnityEngine` dependency. It will represent the 8x8 board, colors, legal moves and flip sets, turn/pass/game-over state, move history, and AI scoring/search inputs. `GameController` will own a session, publish state to views, and serialize user intent into session commands.

This separation is preferred over adapting `mousecatch` in place because the current component mutates shared state from worker threads, drives behavior from frame counters, and depends on object names, tags, and `SendMessage`. The legacy source remains available during migration for behavior comparison and is removed from runtime assembly only after replacement behavior is covered by tests.

### Use immutable AI snapshots and cancellation

AI search will receive an immutable/copy-on-search board snapshot and return a selected legal move. Background work may use `Task` plus `CancellationToken`, but it MUST NOT read or mutate Unity objects or live session state. Results are accepted on the main thread only if the request generation still matches the current session. Restart, undo, mode changes, scene exit, and a newer search cancel or invalidate the prior request.

This replaces raw `Thread`, cross-thread counters, concatenated output strings, and `Thread.Abort`. A coroutine-only search was considered, but the existing recursive scoring can take long enough to stall rendering unless it is extensively rewritten as an incremental algorithm.

### Rebuild one committed `Game` scene through an Editor builder

An Editor-only builder will create or update URP materials, piece and legal-move prefabs, and `Assets/Scenes/Game.unity`, then place it as the only enabled startup scene. Generated assets will be committed so users can open and edit the scene normally; the builder remains available to reproduce its baseline.

The scene will use explicit serialized references and a clear hierarchy rather than tags or global name lookup:

```text
GameRoot
|- Environment
|  |- MainCamera
|  |- Lighting
|  `- Board
|     |- SurfaceAndGrid
|     |- Cells
|     |- Pieces
|     `- MoveHints
|- Systems
|  |- GameController
|  `- AudioController
`- UI
   `- SafeArea
      |- ScoresAndTurn
      |- Actions
      `- ResultPanel
```

The board will expose a single input surface and convert a hit in local space to a board coordinate. This avoids requiring 64 independent physics interaction contracts. Cell visuals may still be generated as children for styling. Legal moves use lightweight unlit hint renderers instead of spawning a point light per move.

### Recreate runtime materials and prefabs while selectively importing raw assets

Useful textures, button art, audio, and the font may be copied into organized `Assets/Reversi/Art` and `Assets/Reversi/Audio` folders. Runtime URP materials and prefabs will be recreated instead of copying legacy `.mat`, scene, or serialized GUIStyle data. Every imported file must exist inside this repository after migration; runtime references cannot point to the sibling project.

Basic generated geometry and neutral materials provide a fallback where a legacy asset is missing or unsuitable. Audio is optional at runtime: a missing optional clip must not prevent gameplay.

### Replace IMGUI with UGUI and unified pointer input

A screen-space Canvas with safe-area adaptation will show scores, turn/pass status, action buttons, AI activity, and the result panel. Buttons call typed controller methods. Board pointer input uses Input System pointer/touch data and EventSystem UI blocking so a UI press cannot also place a piece.

UGUI is chosen over retaining `OnGUI` because the old layout is resolution-dependent and performs state-changing work during GUI events. UI Toolkit was considered, but UGUI is already present in the installed template and is simpler for a small runtime HUD with scene references.

### Preserve behavior through tests and explicit validation

EditMode tests will cover initialization, eight-direction flipping, invalid moves, pass, terminal scoring, history/undo, restart, side selection, and deterministic AI invariants. AI tests assert legality and cancellation rather than one exact move when multiple moves have equivalent/randomized scores.

An Editor validator or batch-mode method will assert the expected Unity version, compilation success, startup scene, required components and serialized references, absence of missing scripts/materials, and successful entry into the initial game state. Manual play checks will cover pointer input, responsive layout, animation, and audio.

## Risks / Trade-offs

- **Legacy AI behavior may shift during extraction** -> Capture representative board positions and legal-result invariants before deleting the old component; keep the scoring weights and tie-breaking policy documented.
- **Recursive AI search may still be slow** -> Use cancellation, bounded search depth/time, an AI-thinking state, and profiling on the target editor; never block the main thread.
- **Old art may render differently in URP** -> Recreate materials and verify in Game view rather than relying on automatic material conversion.
- **The sibling asset source could later disappear** -> Copy only selected source assets during implementation and verify that all runtime GUIDs resolve within this repository.
- **Editor-generated scene output can drift after manual edits** -> Treat the builder as a reproducible baseline and make its behavior idempotent; validate committed scene references in batch mode.
- **No mobile build module is installed** -> Scope acceptance to Editor mouse/touch simulation; document the additional module needed before platform build verification.
- **Removing tracked generated files changes repository layout** -> Perform the migration in reviewable commits/tasks and rely on Git history for rollback.

## Migration Plan

1. Record the legacy source and sibling asset inventory, then seed Unity 6000.5.7f1 project settings without modifying the sibling directory.
2. Add the pure core model, rules tests, session flow, and AI adapter while retaining legacy source as a reference.
3. Import selected raw assets and create URP materials, prefabs, the Editor scene builder, and `Game.unity`.
4. Connect controller, input, UI, audio, animation, persistence, and AI cancellation.
5. Run EditMode tests and batch-mode project/scene validation, then perform manual Game-view checks at representative aspect ratios.
6. Remove or quarantine superseded runtime scripts and generated Unity 2019 artifacts only after replacement validation passes.

Rollback is a Git revert of the migration changes; the sibling legacy project remains read-only and unchanged throughout.

## Open Questions

- The first platform build target remains undecided. Editor playability is the acceptance baseline, and iOS/Android build tasks will be proposed separately after the matching Unity module is installed.
