# Unity 6 migration baseline

Recorded on 2026-08-11 before rebuilding the project.

## Repository state

- Branch: `master`
- Existing untracked paths: `.codex/`, `openspec/`
- Tracked Unity-era content: eight C# scripts under `Assets`, a generated Unity 2019 `Assembly-CSharp.csproj`, `ice reversi(ios).sln`, and nine DLLs under `Library/ScriptAssemblies`
- Legacy C# total: 1,971 lines; `Assets/mousecatch.cs` contains 1,521 lines
- No `Packages`, `ProjectSettings`, Unity metadata, prefab, or scene was present in this repository

## Installed editor

- Version: Unity 6000.5.7f1
- Path: `/Applications/Unity/Hub/Editor/6000.5.7f1`
- Installed project templates include `com.unity.template.3d-cross-platform-17.0.14.tgz`
- macOS Standalone and WebGL support are present; iOS and Android `PlaybackEngines` were not detected

## Read-only legacy asset source

- Path: `/Users/wangzicheng/CocosWorkSpace/ice-reversi/ice-reversi`
- Asset files at baseline: 184
- Aggregate SHA-256 of sorted per-file SHA-256 records: `ef4412183cf5d1e531d4a425eb8b7d46537de7ecbe0de1ae0046f49fc6c4cf2d`
- The source contains `Assets/jumping.unity`, four piece prefabs, a legal-move light prefab, board/button textures, a font, and audio clips
- The source is an input only and must not be modified by this migration
