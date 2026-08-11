using IceReversi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IceReversi.Unity
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private Text blackScoreText;
        [SerializeField] private Text whiteScoreText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text sideButtonText;
        [SerializeField] private Text spectateButtonText;
        [SerializeField] private Text difficultyButtonText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button sideButton;
        [SerializeField] private Button spectateButton;
        [SerializeField] private Button difficultyButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject resultPanel;

        private GameController controller;

        public void Configure(
            Text blackScore,
            Text whiteScore,
            Text status,
            Text result,
            Text sideLabel,
            Text spectateLabel,
            Button restart,
            Button undo,
            Button side,
            Button spectate,
            Button exit,
            GameObject resultContainer)
        {
            blackScoreText = blackScore;
            whiteScoreText = whiteScore;
            statusText = status;
            resultText = result;
            sideButtonText = sideLabel;
            spectateButtonText = spectateLabel;
            restartButton = restart;
            undoButton = undo;
            sideButton = side;
            spectateButton = spectate;
            exitButton = exit;
            resultPanel = resultContainer;
        }

        public void Configure(
            Text blackScore,
            Text whiteScore,
            Text status,
            Text result,
            Text sideLabel,
            Text spectateLabel,
            Text difficultyLabel,
            Button restart,
            Button undo,
            Button side,
            Button spectate,
            Button difficultyControl,
            Button exit,
            GameObject resultContainer)
        {
            Configure(
                blackScore, whiteScore, status, result, sideLabel, spectateLabel,
                restart, undo, side, spectate, exit, resultContainer);
            difficultyButtonText = difficultyLabel;
            difficultyButton = difficultyControl;
        }

        public void Bind(GameController gameController)
        {
            Unbind();
            controller = gameController;
            if (controller == null)
            {
                return;
            }

            restartButton?.onClick.AddListener(controller.RestartGame);
            undoButton?.onClick.AddListener(controller.Undo);
            sideButton?.onClick.AddListener(controller.ToggleHumanSide);
            spectateButton?.onClick.AddListener(controller.ToggleSpectating);
            difficultyButton?.onClick.AddListener(controller.CycleDifficulty);
            exitButton?.onClick.AddListener(controller.ExitGame);
        }

        public void Refresh(
            GameSnapshot snapshot,
            PieceColor humanSide,
            GameMode mode,
            bool isAiThinking,
            AiDifficulty difficulty = AiDifficulty.Normal)
        {
            if (snapshot == null)
            {
                return;
            }

            SetText(blackScoreText, $"Black  {snapshot.BlackScore}");
            SetText(whiteScoreText, $"White  {snapshot.WhiteScore}");
            SetText(sideButtonText, humanSide == PieceColor.Black ? "Play White" : "Play Black");
            SetText(spectateButtonText, mode == GameMode.AiVersusAi ? "Stop Watching" : "Watch AI");
            SetText(difficultyButtonText, $"AI: {difficulty}");

            var status = isAiThinking ? $"{snapshot.ActiveColor} AI thinking..." : $"{snapshot.ActiveColor} to move";
            if (snapshot.LastPassedColor.IsPlayer())
            {
                status = $"{snapshot.LastPassedColor} passes · {status}";
            }

            SetText(statusText, status);
            var isGameOver = snapshot.IsGameOver;
            if (resultPanel != null)
            {
                resultPanel.SetActive(isGameOver);
            }

            if (isGameOver)
            {
                SetText(resultText, ResultLabel(snapshot.Result));
            }

            if (undoButton != null)
            {
                undoButton.interactable = snapshot.HistoryCount > 0;
            }

            if (sideButton != null)
            {
                sideButton.interactable = mode == GameMode.HumanVersusAi;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (controller == null)
            {
                return;
            }

            restartButton?.onClick.RemoveListener(controller.RestartGame);
            undoButton?.onClick.RemoveListener(controller.Undo);
            sideButton?.onClick.RemoveListener(controller.ToggleHumanSide);
            spectateButton?.onClick.RemoveListener(controller.ToggleSpectating);
            difficultyButton?.onClick.RemoveListener(controller.CycleDifficulty);
            exitButton?.onClick.RemoveListener(controller.ExitGame);
            controller = null;
        }

        private static string ResultLabel(GameResult result)
        {
            switch (result)
            {
                case GameResult.BlackWins:
                    return "Black wins";
                case GameResult.WhiteWins:
                    return "White wins";
                case GameResult.Draw:
                    return "Draw";
                default:
                    return string.Empty;
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
