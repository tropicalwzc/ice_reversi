## ADDED Requirements

### Requirement: Legal moves receive complete visual transitions
The presentation SHALL animate the newly placed piece and every captured piece for each legal human, AI, and AI-versus-AI move, and SHALL leave every rendered piece synchronized with the resulting board snapshot.

#### Scenario: Human move flips pieces
- **WHEN** the human submits a legal move that captures one or more pieces
- **THEN** the placed piece visibly enters its square
- **AND** each captured piece visibly rotates from its prior color to the played color
- **AND** each material changes at the visual midpoint of its flip rather than jumping immediately to the final color

#### Scenario: Computer move flips pieces
- **WHEN** a current AI search result is legally applied
- **THEN** the placed and captured pieces receive the same complete transition used for a human move
- **AND** the final rendered colors and coordinates match the session snapshot

#### Scenario: Move captures along multiple lines
- **WHEN** one move captures pieces at different distances or in multiple directions
- **THEN** the presentation MAY stagger their start times to make the capture readable
- **AND** the entire transition SHALL finish within a configured bounded presentation duration

### Requirement: Turn progression respects a presentation barrier
The controller SHALL distinguish logical move completion from visual presentation completion and SHALL not apply a following AI move or accept a following human move before the required presentation barrier is satisfied.

#### Scenario: AI finishes while the human move is animating
- **WHEN** AI computation completes before the human move transition finishes
- **THEN** the AI result remains pending without changing the live session or board presentation
- **AND** it is applied only after the human transition completes and the request generation is still current

#### Scenario: Computer move is presented to the human
- **WHEN** an AI move is applied in human-versus-AI mode
- **THEN** human board input remains gated until the AI placement and flips finish
- **AND** input becomes available after the transition when the session is still on the human turn

#### Scenario: Spectating advances between moves
- **WHEN** AI-versus-AI spectating is active
- **THEN** each move transition completes before the next AI move is applied
- **AND** the configured spectating pacing does not cancel or conceal the flip transition

### Requirement: Status refreshes do not cancel valid animations
HUD-only changes such as AI-thinking state, scores, pass text, button availability, or difficulty labels SHALL NOT restart, snap, or cancel an otherwise current piece transition.

#### Scenario: AI thinking begins after a human move
- **WHEN** the controller updates the HUD to show that the AI is thinking while human flips are animating
- **THEN** the active piece animations continue to completion
- **AND** the board is not redundantly synchronized through a non-animated refresh

#### Scenario: AI thinking ends after its move
- **WHEN** AI-thinking state clears after a computer move is selected
- **THEN** the computer move transition remains visible to completion
- **AND** the final HUD state may update independently of that transition

### Requirement: Presentation work is cancellable and recoverable
Restart, undo, side changes, mode changes, exit, scene destruction, and newer turn generations SHALL cancel obsolete presentation work and restore the board to the authoritative current snapshot without leaving partial rotations, scales, colors, or stale callbacks.

#### Scenario: Restart during a flip
- **WHEN** restart is requested while one or more pieces are animating
- **THEN** the old animations and pending AI result are invalidated
- **AND** the standard opening board is fully synchronized without rotated or incorrectly colored pieces

#### Scenario: Undo during a computer transition
- **WHEN** undo is requested before a computer move transition finishes
- **THEN** the obsolete transition cannot later mutate the restored view
- **AND** pieces, hints, scores, active turn, and input gating match the restored session

#### Scenario: Scene exits during presentation
- **WHEN** the scene or controller is destroyed during a transition
- **THEN** all presentation completion sources and AI requests are cancelled without an unhandled exception

