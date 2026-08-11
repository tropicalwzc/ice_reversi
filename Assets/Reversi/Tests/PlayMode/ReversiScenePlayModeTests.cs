using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IceReversi.Core;
using IceReversi.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace IceReversi.Tests
{
    public sealed class ReversiScenePlayModeTests
    {
        private const string SidePreferenceKey = "ice-reversi.human-side";
        private const string DifficultyPreferenceKey = "ice-reversi.ai-difficulty";
        private static readonly MethodInfo TryHumanMoveMethod = typeof(GameController).GetMethod(
            "TryHumanMove",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo InvalidateAiMethod = typeof(GameController).GetMethod(
            "InvalidateAi",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayerPrefs.DeleteKey(SidePreferenceKey);
            PlayerPrefs.DeleteKey(DifficultyPreferenceKey);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayerPrefs.DeleteKey(SidePreferenceKey);
            PlayerPrefs.DeleteKey(DifficultyPreferenceKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneStartsWithOpeningPiecesHintsAndHud()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var board = Object.FindAnyObjectByType<BoardView>();
            var hud = Object.FindAnyObjectByType<GameHud>();

            Assert.That(controller, Is.Not.Null);
            Assert.That(board, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(controller.CurrentSnapshot.BlackScore, Is.EqualTo(2));
            Assert.That(controller.CurrentSnapshot.WhiteScore, Is.EqualTo(2));
            Assert.That(controller.CurrentSnapshot.LegalMoves, Has.Count.EqualTo(4));
            Assert.That(board.transform.Find("Pieces").childCount, Is.EqualTo(4));
            Assert.That(board.transform.Find("MoveHints").childCount, Is.EqualTo(4));

            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            Assert.That(System.Array.Exists(texts, text => text.text == "Black  2"), Is.True);
            Assert.That(System.Array.Exists(texts, text => text.text == "White  2"), Is.True);
            Assert.That(System.Array.Exists(texts, text => text.text == "Black to move"), Is.True);
            Assert.That(System.Array.Exists(texts, text => text.text == "AI: Normal"), Is.True);

            var passSnapshot = new GameSnapshot(
                controller.CurrentSnapshot.Board,
                PieceColor.Black,
                PieceColor.White,
                GameResult.InProgress,
                controller.CurrentSnapshot.LegalMoves,
                controller.CurrentSnapshot.HistoryCount);
            hud.Refresh(passSnapshot, PieceColor.Black, GameMode.HumanVersusAi, false);
            Assert.That(
                System.Array.Exists(texts, text => text.text.Contains("White passes")),
                Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HumanAiActionsRemainSynchronizedAndResponsive()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            Assert.That(TryHumanMoveMethod, Is.Not.Null);

            PlayHumanMove(controller, new BoardCoordinate(0, 0));
            Assert.That(controller.CurrentSnapshot.HistoryCount, Is.Zero);

            PlayHumanMove(controller, new BoardCoordinate(2, 3));
            Assert.That(controller.CurrentSnapshot.HistoryCount, Is.EqualTo(1));
            Assert.That(controller.CurrentSnapshot.ActiveColor, Is.EqualTo(PieceColor.White));
            Assert.That(InvalidateAiMethod, Is.Not.Null);
            InvalidateAiMethod.Invoke(controller, null);
            yield return new WaitForSeconds(0.3f);
            var flippedPiece = System.Array.Find(
                Object.FindObjectsByType<PieceView>(FindObjectsInactive.Include),
                piece => piece.name == "Piece_3_3");
            Assert.That(flippedPiece, Is.Not.Null);
            Assert.That(flippedPiece.DisplayedColor, Is.EqualTo(PieceColor.Black));
            Assert.That(Object.FindAnyObjectByType<BoardView>().transform.Find("Pieces").childCount, Is.EqualTo(5));
            controller.RestartGame();
            yield return null;
            AssertOpening(controller.CurrentSnapshot);

            controller.ToggleHumanSide();
            Assert.That(controller.HumanSide, Is.EqualTo(PieceColor.White));
            yield return WaitUntil(
                () => !controller.IsAiThinking && !controller.IsPresentingMove,
                3f,
                "opening AI move presentation");
            Assert.That(controller.CurrentSnapshot.HistoryCount, Is.EqualTo(1));
            Assert.That(controller.CurrentSnapshot.ActiveColor, Is.EqualTo(PieceColor.White));

            PlayHumanMove(controller, controller.CurrentSnapshot.LegalMoves[0]);
            yield return WaitUntil(
                () => !controller.IsAiThinking && !controller.IsPresentingMove &&
                    controller.CurrentSnapshot.HistoryCount >= 3,
                3f,
                "AI response");
            controller.Undo();
            Assert.That(controller.CurrentSnapshot.HistoryCount, Is.EqualTo(1));
            Assert.That(controller.CurrentSnapshot.ActiveColor, Is.EqualTo(PieceColor.White));

            controller.ToggleSpectating();
            Assert.That(controller.Mode, Is.EqualTo(GameMode.AiVersusAi));
            yield return WaitUntil(() => controller.CurrentSnapshot.HistoryCount >= 2, 4f, "spectating moves");
            controller.ToggleSpectating();
            Assert.That(controller.Mode, Is.EqualTo(GameMode.HumanVersusAi));
            AssertOpening(controller.CurrentSnapshot);
        }

        [UnityTest]
        public IEnumerator FastSpectatingReachesGameOverAndRestartStaysAvailable()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            SetPrivateField(controller, "aiDepth", 1);
            SetPrivateField(controller, "aiNodeLimit", 10000);
            SetPrivateField(controller, "aiTimeLimitMilliseconds", 100);
            SetPrivateField(controller, "spectatingMoveDelay", 0f);
            Object.FindAnyObjectByType<BoardView>().ConfigurePresentation(0.05f, 0.05f, 0f, 0.1f);

            controller.ToggleSpectating();
            yield return WaitUntil(() => controller.CurrentSnapshot.IsGameOver, 10f, "AI game over");

            Assert.That(controller.CurrentSnapshot.Result, Is.Not.EqualTo(GameResult.InProgress));
            Assert.That(controller.CurrentSnapshot.BlackScore + controller.CurrentSnapshot.WhiteScore, Is.GreaterThan(4));
            var resultPanel = FindChild(Object.FindAnyObjectByType<GameHud>().transform, "ResultPanel");
            Assert.That(resultPanel, Is.Not.Null);
            Assert.That(resultPanel.gameObject.activeInHierarchy, Is.True);
            Assert.That(resultPanel.GetComponentInChildren<Text>().text, Is.Not.Empty);

            controller.RestartGame();
            AssertOpening(controller.CurrentSnapshot);
            Assert.That(controller.IsAiThinking, Is.True);
            controller.ToggleSpectating();
            Assert.That(controller.Mode, Is.EqualTo(GameMode.HumanVersusAi));
            AssertOpening(controller.CurrentSnapshot);
        }

        [UnityTest]
        public IEnumerator HudRefreshAndAiInvalidation_DoNotCancelHumanFlip()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var hud = Object.FindAnyObjectByType<GameHud>();
            PlayHumanMove(controller, new BoardCoordinate(2, 3));

            Assert.That(controller.IsPresentingMove, Is.True);
            var flippedPiece = FindPiece("Piece_3_3");
            Assert.That(flippedPiece.DisplayedColor, Is.EqualTo(PieceColor.White));
            hud.Refresh(controller.CurrentSnapshot, controller.HumanSide, controller.Mode, true, controller.Difficulty);
            InvalidateAiMethod.Invoke(controller, null);

            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(controller.IsPresentingMove, Is.True);
            Assert.That(flippedPiece.IsAnimating, Is.True);
            yield return WaitUntil(() => !controller.IsPresentingMove, 1f, "uncancelled human flip");
            Assert.That(flippedPiece.DisplayedColor, Is.EqualTo(PieceColor.Black));
            Assert.That(flippedPiece.transform.localRotation, Is.EqualTo(Quaternion.identity));
        }

        [UnityTest]
        public IEnumerator PlacementFlipStaggerAndBoundedCompletion_AreVisible()
        {
            var boardView = Object.FindAnyObjectByType<BoardView>();
            var before = BoardState.FromRows(
                "...B....",
                "...W....",
                "...W....",
                "BWW.WWB.",
                "...W....",
                "...W....",
                "...B....",
                "........");
            var beforeSnapshot = new GameSnapshot(
                before,
                PieceColor.Black,
                PieceColor.Empty,
                GameResult.InProgress,
                ReversiRules.GetLegalMoves(before, PieceColor.Black),
                0);
            boardView.CancelAndSynchronize(beforeSnapshot);
            yield return null;
            var session = new GameSession(before, PieceColor.Black);
            Assert.That(session.TryPlayMove(new BoardCoordinate(3, 3), out var move), Is.True);

            var placementSounds = 0;
            var flipSounds = 0;
            var started = Time.realtimeSinceStartup;
            var presentation = boardView.PresentMove(
                session.Snapshot(),
                move,
                CancellationToken.None,
                () => placementSounds++,
                () => flipSounds++);

            var placed = FindPiece("Piece_3_3");
            Assert.That(placed.transform.localScale.sqrMagnitude, Is.LessThan(0.0001f));
            Assert.That(FindPiece("Piece_2_3").DisplayedColor, Is.EqualTo(PieceColor.White));
            yield return new WaitForSecondsRealtime(0.145f);
            Assert.That(placed.transform.localScale, Is.Not.EqualTo(Vector3.zero));
            Assert.That(FindPiece("Piece_2_3").DisplayedColor, Is.EqualTo(PieceColor.Black));
            Assert.That(FindPiece("Piece_1_3").DisplayedColor, Is.EqualTo(PieceColor.White));

            yield return WaitForTask(presentation, 1f, "multi-line move presentation");
            Assert.That(Time.realtimeSinceStartup - started, Is.LessThanOrEqualTo(0.6f));
            Assert.That(placementSounds, Is.EqualTo(1));
            Assert.That(flipSounds, Is.EqualTo(1));
            foreach (var coordinate in move.Flips)
            {
                Assert.That(FindPiece($"Piece_{coordinate.Row}_{coordinate.Column}").DisplayedColor,
                    Is.EqualTo(PieceColor.Black));
            }
        }

        [UnityTest]
        public IEnumerator CancelPresentation_SnapsToAuthoritativeOpening()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            PlayHumanMove(controller, new BoardCoordinate(2, 3));
            Assert.That(controller.IsPresentingMove, Is.True);

            yield return new WaitForSecondsRealtime(0.06f);
            controller.RestartGame();
            yield return WaitUntil(
                () => Object.FindAnyObjectByType<BoardView>().transform.Find("Pieces").childCount == 4,
                0.5f,
                "opening piece cleanup");
            AssertOpening(controller.CurrentSnapshot);
            Assert.That(controller.IsPresentingMove, Is.False);
            Assert.That(Object.FindAnyObjectByType<BoardView>().transform.Find("Pieces").childCount, Is.EqualTo(4));
            foreach (var piece in Object.FindObjectsByType<PieceView>(FindObjectsInactive.Include))
            {
                Assert.That(piece.IsAnimating, Is.False);
                Assert.That(piece.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(piece.transform.localScale, Is.EqualTo(Vector3.one));
            }
        }

        [UnityTest]
        public IEnumerator DifficultyCyclesUpdatesHudAndRestoresFromPlayerPrefs()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            controller.CycleDifficulty();
            Assert.That(controller.Difficulty, Is.EqualTo(AiDifficulty.Hard));
            Assert.That(PlayerPrefs.GetString(DifficultyPreferenceKey), Is.EqualTo("hard"));
            Assert.That(System.Array.Exists(
                Object.FindObjectsByType<Text>(FindObjectsInactive.Include),
                text => text.text == "AI: Hard"), Is.True);

            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            Assert.That(Object.FindAnyObjectByType<GameController>().Difficulty, Is.EqualTo(AiDifficulty.Hard));
        }

        [UnityTest]
        public IEnumerator FastAiResult_WaitsForHumanBarrierAndAiPresentationCompletes()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            SetPrivateField(controller, "aiDepth", 1);
            SetPrivateField(controller, "aiNodeLimit", 10000);
            SetPrivateField(controller, "aiTimeLimitMilliseconds", 100);

            PlayHumanMove(controller, new BoardCoordinate(2, 3));
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(controller.CurrentSnapshot.HistoryCount, Is.EqualTo(1),
                "A completed AI search must remain pending during the human transition.");
            Assert.That(controller.IsPresentingMove, Is.True);

            yield return WaitUntil(
                () => controller.CurrentSnapshot.HistoryCount == 2,
                1f,
                "pending AI move application");
            Assert.That(controller.IsPresentingMove, Is.True,
                "The applied AI move must retain input gating while its own transition is visible.");
            yield return WaitUntil(
                () => !controller.IsPresentingMove && !controller.IsAiThinking,
                1f,
                "AI move presentation completion");
        }

        [UnityTest]
        public IEnumerator DifficultyChangeDuringThinking_ReplacesSearchWithoutChangingPosition()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            controller.ToggleHumanSide();
            var opening = controller.CurrentSnapshot.Board;
            Assert.That(controller.IsAiThinking, Is.True);

            controller.CycleDifficulty();
            Assert.That(controller.Difficulty, Is.EqualTo(AiDifficulty.Hard));
            Assert.That(controller.CurrentSnapshot.Board, Is.EqualTo(opening));
            Assert.That(controller.CurrentSnapshot.HistoryCount, Is.Zero);
            Assert.That(controller.IsAiThinking, Is.True);

            yield return WaitUntil(
                () => controller.CurrentSnapshot.HistoryCount == 1 &&
                    !controller.IsAiThinking && !controller.IsPresentingMove,
                3f,
                "replacement difficulty search and presentation");
            Assert.That(controller.CurrentSnapshot.ActiveColor, Is.EqualTo(PieceColor.White));
        }

        private static void PlayHumanMove(GameController controller, BoardCoordinate coordinate)
        {
            TryHumanMoveMethod.Invoke(controller, new object[] { coordinate });
        }

        private static void AssertOpening(GameSnapshot snapshot)
        {
            Assert.That(snapshot.BlackScore, Is.EqualTo(2));
            Assert.That(snapshot.WhiteScore, Is.EqualTo(2));
            Assert.That(snapshot.HistoryCount, Is.Zero);
            Assert.That(snapshot.Result, Is.EqualTo(GameResult.InProgress));
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, float timeout, string operation)
        {
            var deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(predicate(), Is.True, $"Timed out waiting for {operation}.");
        }

        private static IEnumerator WaitForTask(Task task, float timeout, string operation)
        {
            var deadline = Time.realtimeSinceStartup + timeout;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(task.IsCompleted, Is.True, $"Timed out waiting for {operation}.");
            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
        }

        private static PieceView FindPiece(string name)
        {
            var piece = System.Array.Find(
                Object.FindObjectsByType<PieceView>(FindObjectsInactive.Include),
                candidate => candidate.name == name);
            Assert.That(piece, Is.Not.Null, $"Expected {name}.");
            return piece;
        }

        private static void SetPrivateField<T>(GameController controller, string fieldName, T value)
        {
            var field = typeof(GameController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(controller, value);
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
