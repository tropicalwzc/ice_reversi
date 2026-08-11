## 1. Establish the Unity 6 Project

- [x] 1.1 Record the current Git status, legacy script inventory, installed Unity 6000.5.7f1 path, and sibling asset inventory before migration
- [x] 1.2 Generate a Unity 6000.5.7f1 cross-platform 3D seed project in a temporary directory and merge the required `Packages` and `ProjectSettings` into the repository without overwriting source or OpenSpec files
- [x] 1.3 Add a Unity `.gitignore` and stop treating `Library`, `Temp`, `Logs`, `obj`, builds, generated `.csproj`, and generated solution files as authoritative project content
- [x] 1.4 Pin the minimal compatible URP, UGUI, Input System, test framework, and IDE packages and remove unused template sample/tutorial and multiplayer content
- [x] 1.5 Open/import the repository in Unity 6000.5.7f1 batch mode and resolve all project-foundation compile or package errors

## 2. Implement and Test Reversi Core Rules

- [x] 2.1 Create organized runtime, core, presentation, editor, and test folders with assembly definitions that keep `Reversi.Core` independent of `UnityEngine`
- [x] 2.2 Implement immutable board coordinates, piece colors, board snapshots, move results, and standard opening-state construction
- [x] 2.3 Implement legal-move discovery and horizontal, vertical, and diagonal flip collection without mutating the board during queries
- [x] 2.4 Implement move application, score calculation, active-color advancement, automatic pass, and terminal winner/draw detection
- [x] 2.5 Implement session history, restart, single-turn undo, and human-versus-AI exchange undo with fully recomputed derived state
- [x] 2.6 Add an injected local preference abstraction for black/white human-side persistence with a safe default for missing or invalid data
- [x] 2.7 Add EditMode tests for opening state, every flip direction, multi-direction moves, illegal moves, pass, terminal results, restart, history, undo, and side preference fallback

## 3. Migrate the AI Safely

- [x] 3.1 Extract and document the legacy positional, mobility, stability, pattern, recursion-depth, and random tie-breaking behavior from `mousecatch.cs`
- [x] 3.2 Implement the AI evaluator/search against core board snapshots and guarantee that every returned move is legal for the submitted snapshot
- [x] 3.3 Add a cancellable AI request service that performs no Unity or live-session access off the main thread and rejects stale generation results
- [x] 3.4 Add AI tests for representative board positions, legal-result invariants, no-move positions, cancellation, and stale-result rejection
- [x] 3.5 Profile representative early-, middle-, and late-game searches and enforce a bounded search/time policy that keeps the Unity main thread responsive

## 4. Import and Rebuild Visual Assets

- [x] 4.1 Select and copy only the required textures, button art, font, and audio from the sibling legacy project into organized repository-local asset folders, preserving provenance notes
- [x] 4.2 Create Unity 6 URP board, grid, black, white, alternate-piece, and legal-move-hint materials with generated fallback visuals where legacy art is unsuitable
- [x] 4.3 Create reusable piece and legal-move-hint prefabs with explicit components and no custom-tag dependency
- [x] 4.4 Implement `BoardView` containers and board-coordinate/world-coordinate conversion using a single board input surface
- [x] 4.5 Implement piece creation, synchronization, flip animation, legal-move hints, and full refresh after restart or undo

## 5. Implement Controller, Input, UI, and Audio

- [x] 5.1 Implement `GameController` as the sole coordinator of core session commands, presentation refresh, input gating, and AI request generations
- [x] 5.2 Implement unified Input System mouse and single-touch board selection with EventSystem UI blocking and exactly-once move submission
- [x] 5.3 Implement human-black, human-white, and AI-versus-AI spectating flows, including responsive stop/restart/exit behavior while AI is thinking
- [x] 5.4 Build a UGUI HUD for scores, active color, AI thinking, pass notifications, restart, undo, side selection, spectating, and terminal winner/draw state
- [x] 5.5 Add safe-area handling and adaptive portrait, landscape, 16:9, and 4:3 layouts that keep the board and essential actions reachable
- [x] 5.6 Implement typed audio feedback for placement, flipping, actions, and game completion with null-safe optional clip handling

## 6. Generate and Validate the Main Scene

- [x] 6.1 Implement an idempotent Editor builder that creates or updates runtime materials, prefabs, and `Assets/Scenes/Game.unity`
- [x] 6.2 Have the builder assemble the camera, lighting, board hierarchy, systems, EventSystem, Canvas, safe-area HUD, and all explicit serialized references
- [x] 6.3 Configure `Game.unity` as the only enabled startup scene and verify repeated builder runs do not create duplicate objects or scenes
- [x] 6.4 Implement a batch-mode Editor validator for Unity version, startup scene, missing scripts/materials/references, required hierarchy/components, and standard initial game state

## 7. Verify and Complete the Migration

- [x] 7.1 Run all EditMode tests and Unity batch-mode project/scene validation with Unity 6000.5.7f1 and fix every failure
- [x] 7.2 Perform Editor play checks for legal and illegal moves, flipping, AI response, pass, undo, restart, side selection, spectating, and game-over flow
- [x] 7.3 Inspect Game view layout and input at representative portrait, landscape, 16:9, and 4:3 sizes and correct overlaps or inaccessible controls
- [x] 7.4 Verify runtime scenes and prefabs have no dependency on the sibling project and that the sibling directory was not modified
- [x] 7.5 Remove or quarantine superseded legacy runtime scripts and tracked Unity 2019 generated artifacts only after replacement tests and validation pass
- [x] 7.6 Document the scene rebuild/validation commands, local Unity version requirement, controls, asset provenance, and the missing platform Build Support prerequisite for future mobile builds
