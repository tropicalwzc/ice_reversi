# Move Presentation and AI Baseline

Recorded on 2026-08-11 before implementing `improve-move-animation-and-ai-difficulty`.

## Workspace

- Unity: `6000.5.7f1`
- Git worktree: dirty by design because the rebuilt Unity 6 project and OpenSpec artifacts are not yet committed. Legacy Unity 2019 scripts and generated solution/assembly files are deleted; the new `Assets/Reversi`, `Assets/Scenes`, packages, settings, docs, and OpenSpec files are untracked or modified.
- Validator: `ICE_REVERSI_VALIDATION_SUCCESS`
- EditMode: 28 passed, 0 failed
- PlayMode: 3 passed, 0 failed

The raw baseline artifacts are stored outside the repository at:

- `/private/tmp/ice_reversi_baseline_validator.log`
- `/private/tmp/ice_reversi_baseline_editmode.xml`
- `/private/tmp/ice_reversi_baseline_editmode.log`
- `/private/tmp/ice_reversi_baseline_playmode.xml`
- `/private/tmp/ice_reversi_baseline_playmode.log`

## Legacy fixed-depth AI profile

The legacy profile used depth 4, 60,000 nodes, and 500 ms. The deterministic representative-position test reported:

| Stage | Plies | Elapsed | Expanded nodes | Limit reached |
| --- | ---: | ---: | ---: | --- |
| Early | 0 | 7 ms | 228 | No |
| Middle | 24 | 249 ms | 2,847 | No |
| Late | 48 | 21 ms | 993 | No |

These measurements are a local comparison baseline, not cross-machine performance requirements.
