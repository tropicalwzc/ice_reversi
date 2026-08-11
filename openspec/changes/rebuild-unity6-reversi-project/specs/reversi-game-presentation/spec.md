## ADDED Requirements

### Requirement: Rebuilt playable game scene
The `Game` scene SHALL provide a camera, lighting, an 8-by-8 board, piece and move-hint containers, game controllers, EventSystem, and screen-space HUD using explicit serialized references.

#### Scenario: Enter the Game scene
- **WHEN** the `Game` scene starts
- **THEN** the complete board and standard opening position are visible
- **AND** no required component relies on legacy tags, global object-name lookup, `SendMessage`, or IMGUI

### Requirement: Board state is visibly represented
The presentation SHALL render each occupied square with the correct piece color, mark current legal moves, animate changed pieces, and keep visuals synchronized after moves, passes, undo, restart, and game over.

#### Scenario: Apply a move with flips
- **WHEN** the controller reports a legal move and its flipped coordinates
- **THEN** the placed piece appears at the selected square
- **AND** every flipped piece visibly transitions to the new color
- **AND** legal-move hints update for the next playable color

#### Scenario: Restore an earlier position
- **WHEN** undo restores a previous session state
- **THEN** pieces, hints, scores, active-turn display, and result UI all match the restored state

### Requirement: Mouse and touch place pieces through one interaction path
The board SHALL accept both mouse and single-touch selection, map the pointer hit to the same board coordinate system, and ignore board placement when the pointer is over UI or input is not currently allowed.

#### Scenario: Select a legal square with mouse
- **WHEN** the user clicks a displayed legal square during the human turn
- **THEN** exactly one move is submitted for that coordinate

#### Scenario: Select a legal square with touch
- **WHEN** the user taps a displayed legal square during the human turn
- **THEN** the same move outcome occurs as for a mouse selection of that coordinate

#### Scenario: Press a UI action over the board
- **WHEN** a pointer press is consumed by a HUD control
- **THEN** the board does not also submit a move

### Requirement: HUD exposes game state and actions
The HUD SHALL show black and white scores, active color or AI-thinking state, pass notification, terminal result, and controls for restart, undo, side selection, and AI spectating.

#### Scenario: Game ends
- **WHEN** the session enters a terminal state
- **THEN** the HUD displays the final black and white scores and winner or draw
- **AND** restart remains available

#### Scenario: Action temporarily unavailable
- **WHEN** an action such as undo cannot be performed in the current state
- **THEN** its control is disabled or the HUD provides non-destructive feedback

### Requirement: Layout adapts to supported displays
The scene SHALL keep the complete board and essential HUD controls visible without overlap at representative portrait, landscape, 16:9, and 4:3 Game-view sizes, and SHALL respect the device safe area.

#### Scenario: Change aspect ratio
- **WHEN** the Game view changes between representative portrait and landscape sizes
- **THEN** the board remains fully visible
- **AND** scores, result state, and primary action controls remain reachable inside the safe area

### Requirement: Presentation provides resilient audio feedback
The scene SHALL play configured feedback for placement, flipping, actions, and game completion without making gameplay dependent on optional audio assets.

#### Scenario: Play a move with configured clips
- **WHEN** a legal move is presented and the relevant clips are assigned
- **THEN** placement and flip feedback play through the scene audio controller

#### Scenario: Optional clip is absent
- **WHEN** an optional feedback clip is unassigned
- **THEN** the corresponding game action completes without an exception or blocked state

