## Why

The repository contains the Reversi rules and AI source from Unity 2019.2.6f1, but it is not a complete Unity project and cannot be opened or run with the locally installed Unity 6000.5.7f1. A complete Unity 6 project and a newly assembled main scene are needed so the game can be developed, tested, and played again without depending on the obsolete project layout.

## What Changes

- Create a complete Unity project in this repository targeting the locally installed Unity 6000.5.7f1, with reproducible package and project settings.
- Reuse suitable raw textures, audio, fonts, and visual references from the local legacy asset project while making this repository self-contained.
- Preserve the existing Reversi rules and AI behavior, but separate engine-independent game logic from Unity scene presentation and replace unsafe legacy threading.
- Rebuild the main `Game` scene, board, piece and legal-move prefabs, camera, lighting, audio, and responsive Canvas UI.
- Support both mouse and touch input and retain restart, undo, side selection, AI opponent, AI spectating, scoring, pass, and game-over behavior.
- Add automated rules tests plus Unity batch-mode/editor validation for project compilation, scene references, and basic playability.
- **BREAKING**: Replace the legacy IMGUI-, tag-, `SendMessage`-, and fixed-world-coordinate scene contract; the old `jumping` scene is retained only as a visual/reference source and is not a runtime scene.

## Capabilities

### New Capabilities

- `unity6-project-foundation`: A self-contained Unity 6000.5.7f1 project with pinned packages, valid settings, deterministic scene construction, and a configured startup scene.
- `reversi-gameplay`: Engine-independent Reversi rules and game flow, including legal moves, flipping, turns, pass/game-over handling, undo, side selection, and cancellable AI play.
- `reversi-game-presentation`: A rebuilt playable Unity scene with board and piece visuals, legal-move hints, mouse/touch input, responsive UI, audio feedback, and visible game state.

### Modified Capabilities

None.

## Impact

- Affects the repository root Unity structure, `Assets`, `Packages`, `ProjectSettings`, scene build settings, and generated project metadata.
- Refactors behavior currently concentrated in `Assets/mousecatch.cs` and supersedes legacy `OnGUI`, raw `Thread`/`Thread.Abort`, tag lookup, and `SendMessage` integration.
- Imports selected source assets from `/Users/wangzicheng/CocosWorkSpace/ice-reversi/ice-reversi` but leaves that legacy project unchanged.
- Uses Unity Input System, UGUI, and URP packages compatible with Unity 6000.5.7f1.
- Editor play-mode validation is in scope; platform builds such as iOS require the corresponding Unity Build Support module to be installed separately.
