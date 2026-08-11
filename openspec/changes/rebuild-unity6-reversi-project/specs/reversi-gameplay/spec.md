## ADDED Requirements

### Requirement: Standard Reversi initialization
The game SHALL initialize an 8-by-8 board with the standard four center pieces and black as the first active color.

#### Scenario: Start a new game
- **WHEN** a new session is created
- **THEN** black and white each have two center pieces in the standard diagonal arrangement
- **AND** exactly the legal opening moves for black are available

### Requirement: Legal move and flipping rules
The game SHALL accept a move only on an empty square that brackets at least one contiguous opponent line, and SHALL flip every bracketed opponent piece in all applicable horizontal, vertical, and diagonal directions.

#### Scenario: Place a legal multi-direction move
- **WHEN** the active player places a piece that brackets opponent pieces in more than one direction
- **THEN** the placed piece and every bracketed line are changed to the active color
- **AND** no unbracketed pieces are changed

#### Scenario: Reject an illegal move
- **WHEN** the active player selects an occupied square or an empty square that flips no opponent piece
- **THEN** the board, scores, history, and active color remain unchanged

### Requirement: Turn, pass, and game-over flow
After a legal move, the game SHALL advance to the opponent when that opponent has a legal move, SHALL automatically pass a color with no legal move, and SHALL end when neither color has a legal move or the board is full.

#### Scenario: Automatic pass
- **WHEN** the next color has no legal move and the other color has at least one legal move
- **THEN** the next color is marked as passed
- **AND** play continues with the color that can move

#### Scenario: Determine the result
- **WHEN** neither color can move or all 64 squares are occupied
- **THEN** the session enters a terminal state
- **AND** the color with more pieces is declared the winner, or a draw is declared for equal counts

### Requirement: Restart and undo
The game SHALL allow the current session to restart and SHALL allow completed turns to be undone without leaving derived legal-move, score, or turn state inconsistent.

#### Scenario: Restart a game
- **WHEN** restart is requested during human play, AI thinking, or after game over
- **THEN** pending AI work is invalidated
- **AND** the board, history, score, active color, pass state, and result return to the configured opening state

#### Scenario: Undo a human-versus-AI exchange
- **WHEN** undo is requested after both the human move and responding AI move have completed
- **THEN** the session restores the position before the human move when that history is available
- **AND** recomputes scores and legal moves for the restored active color

### Requirement: Human and spectating modes
The game SHALL support a human-versus-AI mode with the human assigned black or white and an AI-versus-AI spectating mode.

#### Scenario: Human chooses white
- **WHEN** the human side is changed to white and a new game starts
- **THEN** the AI plays the opening black turn
- **AND** human input is accepted only during white turns

#### Scenario: Start AI spectating
- **WHEN** spectating mode is enabled
- **THEN** both colors are controlled by AI until the game ends or the mode is stopped
- **AND** user restart and exit actions remain responsive

### Requirement: Side preference persists
The selected human side SHALL be stored locally and restored for a later session without corrupting the game when no stored value exists.

#### Scenario: Restore saved side
- **WHEN** a new application session begins after the user selected a side
- **THEN** the saved black or white selection becomes the configured human side

#### Scenario: Missing or invalid saved side
- **WHEN** no valid side preference can be read
- **THEN** the game uses the documented default side and remains playable

### Requirement: AI returns legal cancellable results
AI search SHALL operate on a board snapshot, SHALL return only a legal move for that snapshot, and SHALL support cancellation or stale-result rejection without mutating live Unity or session state from a worker thread.

#### Scenario: Complete an AI turn
- **WHEN** AI search finishes for the current session generation
- **THEN** the selected move is legal for the submitted snapshot
- **AND** the controller applies it through the same rules path used for human moves

#### Scenario: Invalidate a pending search
- **WHEN** restart, undo, a mode change, scene exit, or a newer AI request occurs while search is active
- **THEN** the prior search is cancelled or its result is ignored
- **AND** it cannot modify the current board or presentation

