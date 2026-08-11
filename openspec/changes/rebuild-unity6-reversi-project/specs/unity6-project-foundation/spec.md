## ADDED Requirements

### Requirement: Unity 6 project opens and compiles
The repository SHALL be a complete Unity project pinned to Unity 6000.5.7f1 and SHALL compile in that editor without C# errors.

#### Scenario: Open the repository as a Unity project
- **WHEN** a developer opens the repository root with Unity 6000.5.7f1
- **THEN** Unity imports the project using the committed `Packages` and `ProjectSettings` without requiring files from another project
- **AND** script compilation completes without errors

### Requirement: Dependencies are reproducible
The project SHALL pin the runtime and editor packages required by the game and SHALL exclude generated caches and IDE metadata from the authoritative project source.

#### Scenario: Reimport from a clean checkout
- **WHEN** Unity-generated directories and IDE files are absent from a clean checkout
- **THEN** Unity restores the pinned dependencies and regenerates its caches and IDE metadata
- **AND** the playable project content remains unchanged

### Requirement: Main scene is generated and configured
The project SHALL contain a committed `Assets/Scenes/Game.unity` produced by an idempotent Editor builder and SHALL configure that scene as the only enabled startup scene.

#### Scenario: Rebuild the baseline scene
- **WHEN** the Editor scene builder is run more than once with unchanged inputs
- **THEN** it produces a valid `Game` scene and required generated assets without duplicate runtime objects
- **AND** `Game` remains the only enabled startup scene

### Requirement: Runtime assets are self-contained
Every scene, prefab, material, font, texture, audio clip, script, and settings asset used at runtime SHALL resolve within this repository and SHALL have no dependency on the sibling legacy project.

#### Scenario: Validate serialized references
- **WHEN** the project validation command scans runtime scenes and prefabs
- **THEN** it reports no missing scripts, missing required assets, unresolved material references, or runtime paths outside the repository

### Requirement: Editor validation is automatable
The project SHALL expose a non-interactive Unity validation path that checks compilation, startup-scene configuration, required scene components, and initial game startup.

#### Scenario: Run batch-mode validation
- **WHEN** Unity 6000.5.7f1 runs the documented batch-mode validation command
- **THEN** the command exits successfully only when the project and `Game` scene satisfy all required structural checks

