using System;
using System.Threading;
using System.Threading.Tasks;
using IceReversi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace IceReversi.Unity
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameHud hud;
        [SerializeField] private AudioController audioController;
        [SerializeField, HideInInspector, Range(1, 10)] private int aiDepth = 4;
        [SerializeField, HideInInspector, Min(1)] private int aiNodeLimit = 60000;
        [SerializeField, HideInInspector, Min(1)] private int aiTimeLimitMilliseconds = 500;
        [SerializeField, Min(0f)] private float spectatingMoveDelay = 0.35f;

        private readonly AiRequestService aiRequests = new AiRequestService();
        private CancellationTokenSource lifetimeCancellation;
        private CancellationTokenSource turnCancellation;
        private GameSession session;
        private HumanSidePreferences sidePreferences;
        private AiDifficultyPreferences difficultyPreferences;
        private PlayerPrefsLanguageStore languageStore;
        private PieceColor humanSide = PieceColor.Black;
        private AiDifficulty difficulty = AiDifficulty.Normal;
        private GameLanguage language = GameLanguage.English;
        private GameMode mode = GameMode.HumanVersusAi;
        private bool isAiThinking;
        private bool isPresentingMove;
        private bool turnSequenceRunning;
        private bool gameOverSoundPlayed;
        private int aiSeed;
        private int turnGeneration;

        public GameSnapshot CurrentSnapshot => session?.Snapshot();
        public PieceColor HumanSide => humanSide;
        public AiDifficulty Difficulty => difficulty;
        public GameLanguage Language => language;
        public GameMode Mode => mode;
        public bool IsAiThinking => isAiThinking;
        public bool IsPresentingMove => isPresentingMove;

        public void Configure(BoardView board, GameHud gameHud, AudioController audio)
        {
            boardView = board;
            hud = gameHud;
            audioController = audio;
        }

        private void Awake()
        {
            lifetimeCancellation = new CancellationTokenSource();
            sidePreferences = new HumanSidePreferences(new PlayerPrefsSideStore());
            difficultyPreferences = new AiDifficultyPreferences(new PlayerPrefsDifficultyStore());
            languageStore = new PlayerPrefsLanguageStore();
            humanSide = sidePreferences.Load(PieceColor.Black);
            difficulty = difficultyPreferences.Load();
            language = languageStore.Load();
            session = new GameSession();
            hud?.Bind(this);
        }

        private void Start()
        {
            SynchronizeBoard();
            RefreshHud();
            StartTurnSequence(null);
        }

        private void Update()
        {
            if (!CanAcceptHumanMove()) return;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                var touch = Touchscreen.current.primaryTouch;
                HandlePointer(touch.position.ReadValue(), touch.touchId.ReadValue());
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandlePointer(Mouse.current.position.ReadValue(), -1);
            }
        }

        public void RestartGame()
        {
            CancelTurnWork();
            session.Restart();
            gameOverSoundPlayed = false;
            audioController?.PlayAction();
            SynchronizeBoard();
            RefreshHud();
            StartTurnSequence(null);
        }

        public void Undo()
        {
            CancelTurnWork();
            var undone = mode == GameMode.HumanVersusAi
                ? session.UndoHumanAiExchange(humanSide)
                : session.UndoOneTurn();
            if (undone)
            {
                gameOverSoundPlayed = false;
                audioController?.PlayAction();
            }

            SynchronizeBoard();
            RefreshHud();
            StartTurnSequence(null);
        }

        public void ToggleHumanSide()
        {
            if (mode != GameMode.HumanVersusAi) return;
            CancelTurnWork();
            humanSide = humanSide.Opponent();
            sidePreferences.Save(humanSide);
            session.Restart();
            gameOverSoundPlayed = false;
            audioController?.PlayAction();
            SynchronizeBoard();
            RefreshHud();
            StartTurnSequence(null);
        }

        public void ToggleSpectating()
        {
            CancelTurnWork();
            mode = mode == GameMode.HumanVersusAi ? GameMode.AiVersusAi : GameMode.HumanVersusAi;
            session.Restart();
            gameOverSoundPlayed = false;
            audioController?.PlayAction();
            SynchronizeBoard();
            RefreshHud();
            StartTurnSequence(null);
        }

        public void CycleDifficulty()
        {
            CancelTurnWork();
            difficulty = AiDifficultyProfile.Next(difficulty);
            difficultyPreferences.Save(difficulty);
            audioController?.PlayAction();
            SynchronizeBoard();
            RefreshHud();
            StartTurnSequence(null);
        }

        public void ToggleLanguage()
        {
            language = language == GameLanguage.English ? GameLanguage.Chinese : GameLanguage.English;
            languageStore.Save(language);
            audioController?.PlayAction();
            RefreshHud();
        }

        public void ExitGame()
        {
            CancelTurnWork();
            audioController?.PlayAction();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandlePointer(Vector2 screenPosition, int pointerId)
        {
            if (EventSystem.current != null)
            {
                var overUi = pointerId >= 0
                    ? EventSystem.current.IsPointerOverGameObject(pointerId)
                    : EventSystem.current.IsPointerOverGameObject();
                if (overUi) return;
            }

            if (boardView != null && boardView.TryScreenToCoordinate(screenPosition, out var coordinate))
            {
                TryHumanMove(coordinate);
            }
        }

        private void TryHumanMove(BoardCoordinate coordinate)
        {
            if (!CanAcceptHumanMove() || !session.TryPlayMove(coordinate, out var result)) return;
            StartTurnSequence(result);
        }

        private bool CanAcceptHumanMove()
        {
            return session != null && !session.Snapshot().IsGameOver && !isAiThinking &&
                !isPresentingMove && !turnSequenceRunning && mode == GameMode.HumanVersusAi &&
                session.ActiveColor == humanSide;
        }

        private void BeginAiIfRequired()
        {
            if (!turnSequenceRunning) StartTurnSequence(null);
        }

        private void StartTurnSequence(MoveResult initialMove)
        {
            if (session == null || turnSequenceRunning) return;
            turnCancellation?.Dispose();
            turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token);
            turnGeneration++;
            var generation = turnGeneration;
            turnSequenceRunning = true;
            _ = RunTurnSequenceAsync(initialMove, generation, turnCancellation.Token);
        }

        private async Task RunTurnSequenceAsync(
            MoveResult moveToPresent,
            int generation,
            CancellationToken cancellationToken)
        {
            try
            {
                while (IsTurnCurrent(generation))
                {
                    var snapshot = session.Snapshot();
                    var aiRequired = RequiresAiTurn(snapshot);
                    AiRequest? pendingRequest = null;
                    if (aiRequired)
                    {
                        isAiThinking = true;
                        pendingRequest = aiRequests.Start(
                            snapshot.Board,
                            snapshot.ActiveColor,
                            CreateSearchOptions(),
                            cancellationToken);
                    }

                    Task presentation = Task.CompletedTask;
                    if (moveToPresent != null)
                    {
                        isPresentingMove = true;
                        presentation = boardView != null
                            ? boardView.PresentMove(
                                snapshot,
                                moveToPresent,
                                cancellationToken,
                                () => audioController?.PlayPlacement(),
                                () => audioController?.PlayFlip())
                            : Task.CompletedTask;
                    }

                    RefreshHud();
                    await presentation;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsTurnCurrent(generation)) return;

                    isPresentingMove = false;
                    RefreshHud();
                    PlayGameOverIfRequired();
                    if (!aiRequired || !pendingRequest.HasValue) break;

                    if (mode == GameMode.AiVersusAi && moveToPresent != null && spectatingMoveDelay > 0f)
                    {
                        await Task.Delay(Mathf.RoundToInt(spectatingMoveDelay * 1000f), cancellationToken);
                    }

                    var request = pendingRequest.Value;
                    var result = await request.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsTurnCurrent(generation) || !aiRequests.IsCurrent(request.Generation) ||
                        session.Result != GameResult.InProgress)
                    {
                        return;
                    }

                    isAiThinking = false;
                    if (!result.Move.HasValue || !session.TryPlayMove(result.Move.Value, out moveToPresent))
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // A restart, undo, mode/side/difficulty change, exit, or newer generation owns the view now.
            }
            catch (Exception exception)
            {
                if (this) Debug.LogException(exception, this);
            }
            finally
            {
                if (IsTurnCurrent(generation))
                {
                    isAiThinking = false;
                    isPresentingMove = false;
                    turnSequenceRunning = false;
                    RefreshHud();
                    PlayGameOverIfRequired();
                }
            }
        }

        private AiSearchOptions CreateSearchOptions()
        {
            var options = AiDifficultyProfile.For(difficulty).CreateSearchOptions(
                unchecked(Environment.TickCount + aiSeed++));

            // Retain the migration PlayMode harness's private-field override without using it in normal scenes.
            if (aiDepth != 4 || aiNodeLimit != 60000 || aiTimeLimitMilliseconds != 500)
            {
                options.MaximumDepth = aiDepth;
                options.MaximumNodes = aiNodeLimit;
                options.MaximumThinkTimeMilliseconds = aiTimeLimitMilliseconds;
            }

            return options;
        }

        private bool RequiresAiTurn(GameSnapshot snapshot)
        {
            return snapshot != null && !snapshot.IsGameOver &&
                (mode == GameMode.AiVersusAi || snapshot.ActiveColor != humanSide);
        }

        private bool IsTurnCurrent(int generation)
        {
            return this && generation == turnGeneration && turnCancellation != null &&
                !turnCancellation.IsCancellationRequested;
        }

        private void SynchronizeBoard()
        {
            if (session != null) boardView?.CancelAndSynchronize(session.Snapshot());
        }

        private void RefreshHud()
        {
            if (session != null)
            {
                hud?.Refresh(session.Snapshot(), humanSide, mode, isAiThinking, difficulty, language);
            }
        }

        private void PlayGameOverIfRequired()
        {
            if (session == null || !session.Snapshot().IsGameOver || gameOverSoundPlayed) return;
            gameOverSoundPlayed = true;
            audioController?.PlayGameOver();
        }

        private void InvalidateAi()
        {
            aiRequests.Invalidate();
            isAiThinking = false;
            RefreshHud();
        }

        private void CancelTurnWork()
        {
            turnGeneration++;
            turnSequenceRunning = false;
            isPresentingMove = false;
            turnCancellation?.Cancel();
            turnCancellation?.Dispose();
            turnCancellation = null;
            InvalidateAi();
            if (session != null) boardView?.CancelAndSynchronize(session.Snapshot());
        }

        private void OnDestroy()
        {
            hud?.Bind(null);
            turnGeneration++;
            turnCancellation?.Cancel();
            turnCancellation?.Dispose();
            turnCancellation = null;
            lifetimeCancellation?.Cancel();
            lifetimeCancellation?.Dispose();
            lifetimeCancellation = null;
            aiRequests.Dispose();
        }
    }
}
