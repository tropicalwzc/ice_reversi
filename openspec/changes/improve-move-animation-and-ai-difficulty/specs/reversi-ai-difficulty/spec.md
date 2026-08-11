## ADDED Requirements

### Requirement: Users can choose a persistent AI difficulty
The game SHALL expose Easy, Normal, Hard, and Expert difficulty profiles, SHALL display the current profile in the HUD, SHALL persist the selection locally, and SHALL use Normal when stored data is missing or invalid.

#### Scenario: Cycle the difficulty
- **WHEN** the user activates the difficulty control
- **THEN** the selection advances through Easy, Normal, Hard, and Expert in a documented order
- **AND** the HUD immediately shows the new selection

#### Scenario: Restore a saved difficulty
- **WHEN** a later application session starts after the user selected a valid difficulty
- **THEN** the saved profile supplies the AI search policy

#### Scenario: Invalid stored difficulty
- **WHEN** the stored difficulty value is absent or unrecognized
- **THEN** the game selects Normal and remains playable

#### Scenario: Responsive difficulty control
- **WHEN** the scene is viewed at the supported portrait, landscape, 16:9, or 4:3 sizes
- **THEN** the difficulty selection and its label remain readable and reachable inside the safe area
- **AND** existing essential controls remain reachable without overlap

### Requirement: Difficulty changes safely replace active searches
Changing difficulty SHALL cancel or invalidate the active AI request and SHALL start at most one replacement request for the same authoritative position when AI still controls the active color.

#### Scenario: Change difficulty while AI is thinking
- **WHEN** the user changes difficulty before the active AI request completes
- **THEN** the old generation cannot apply a move
- **AND** the replacement search uses the new profile against the unchanged session snapshot

#### Scenario: Change difficulty on a human turn
- **WHEN** difficulty changes while the human controls the active color
- **THEN** the session board, history, active color, and presentation remain unchanged
- **AND** the next AI turn uses the selected profile

### Requirement: Difficulty profiles provide ordered bounded policies
Each difficulty SHALL define bounded maximum depth, node count, think time, endgame threshold, and root-choice policy; higher profiles SHALL receive no smaller search budget than lower profiles, and only Easy MAY deliberately select a non-best move from a scored legal shortlist.

#### Scenario: Easy chooses a move
- **WHEN** Easy completes a search with multiple scored legal candidates
- **THEN** it may use seeded controlled variation among its best shortlist
- **AND** it never returns a move outside the submitted snapshot's legal moves

#### Scenario: Hard or Expert chooses a move
- **WHEN** Hard or Expert completes at least one root iteration
- **THEN** it selects from the best score of the deepest fully completed iteration
- **AND** it does not deliberately downgrade to a lower-scored candidate

#### Scenario: Search reaches a policy limit
- **WHEN** any profile reaches its node or elapsed-time bound during a deeper iteration
- **THEN** the search stops promptly
- **AND** returns a legal result from the deepest fully completed root iteration, or a legal deterministic fallback if no iteration completed

### Requirement: Search uses iterative deepening and efficient pruning
The AI SHALL search completed depths in increasing order, SHALL order promising moves before weaker moves, and SHALL use a capacity-bounded position cache without changing legal-move, pass, terminal, cancellation, or immutable-snapshot semantics.

#### Scenario: Complete successive depths
- **WHEN** time and node budgets permit additional work
- **THEN** the AI completes depth one before depth two and continues up to the profile maximum
- **AND** reports the deepest fully completed depth in its result diagnostics

#### Scenario: Revisit a cached position
- **WHEN** the same position, active color, and sufficient search depth are encountered within a request
- **THEN** a valid exact or bounded cache entry may be reused for Alpha-Beta pruning
- **AND** cache capacity remains within the configured limit

#### Scenario: Cancellation during optimized search
- **WHEN** cancellation is requested during move ordering, evaluation, cache lookup, or recursion
- **THEN** the search terminates with cancellation
- **AND** cannot publish a partial or stale move to the live session

### Requirement: Evaluation adapts to game phase
The AI SHALL evaluate corner ownership and safety, mobility, stable edges, frontier exposure, disc difference, and parity with weights appropriate to early, middle, and late game rather than relying on one phase-independent score.

#### Scenario: Early or middle game choice
- **WHEN** material gain conflicts with mobility, corner safety, or frontier exposure before the late game
- **THEN** the evaluator accounts for the strategic signals configured for that phase

#### Scenario: Late game choice
- **WHEN** few empty squares remain
- **THEN** disc difference and parity receive increased influence
- **AND** Hard or Expert may search to terminal positions within its profile bounds

#### Scenario: Expert exact endgame completes
- **WHEN** the position is inside Expert's exact-search threshold and terminal search completes before its limits
- **THEN** the selected move has the best terminal outcome found among all legal root moves

### Requirement: AI strength and performance remain verifiable
Automated tests and repeatable profiling SHALL cover tactical fixtures, completed-depth fallback, cache behavior, every difficulty's legality and bounds, cancellation, early/middle/late positions, and representative endgames.

#### Scenario: Run the AI verification suite
- **WHEN** EditMode AI tests run under Unity 6000.5.7f1
- **THEN** every difficulty returns only legal moves or no-move results as appropriate
- **AND** measured requests respect configured bounds with documented tolerance

#### Scenario: Compare difficulty profiles
- **WHEN** deterministic benchmark games or position suites compare stronger profiles with weaker profiles
- **THEN** the report records completed depth, nodes, elapsed time, cache hits, selected moves, and outcomes
- **AND** benchmark observations do not replace deterministic correctness assertions

