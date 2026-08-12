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
        [SerializeField] private Text restartButtonText;
        [SerializeField] private Text undoButtonText;
        [SerializeField] private Text languageButtonText;
        [SerializeField] private Text exitButtonText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button sideButton;
        [SerializeField] private Button spectateButton;
        [SerializeField] private Button difficultyButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject resultPanel;

        private GameController controller;
        private Font englishFont;
        private Font chineseFont;
        private GameLanguage appliedLanguage = (GameLanguage)(-1);

        private static readonly string[] ChineseFontNames =
        {
            "PingFang SC",
            "Microsoft YaHei",
            "Noto Sans CJK SC",
            "Droid Sans Fallback",
            "Arial Unicode MS",
            "Arial"
        };

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
            restartButtonText = restart != null ? restart.GetComponentInChildren<Text>(true) : null;
            undoButtonText = undo != null ? undo.GetComponentInChildren<Text>(true) : null;
            exitButtonText = exit != null ? exit.GetComponentInChildren<Text>(true) : null;
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

        public void Configure(
            Text blackScore,
            Text whiteScore,
            Text status,
            Text result,
            Text sideLabel,
            Text spectateLabel,
            Text difficultyLabel,
            Text languageLabel,
            Button restart,
            Button undo,
            Button side,
            Button spectate,
            Button difficultyControl,
            Button languageControl,
            Button exit,
            GameObject resultContainer)
        {
            Configure(
                blackScore, whiteScore, status, result, sideLabel, spectateLabel, difficultyLabel,
                restart, undo, side, spectate, difficultyControl, exit, resultContainer);
            languageButtonText = languageLabel;
            languageButton = languageControl;
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
            languageButton?.onClick.AddListener(controller.ToggleLanguage);
            exitButton?.onClick.AddListener(controller.ExitGame);
        }

        public void Refresh(
            GameSnapshot snapshot,
            PieceColor humanSide,
            GameMode mode,
            bool isAiThinking,
            AiDifficulty difficulty = AiDifficulty.Normal,
            GameLanguage language = GameLanguage.English)
        {
            if (snapshot == null)
            {
                return;
            }

            ApplyLanguageFont(language);
            SetText(blackScoreText, GameLocalization.BlackScore(language, snapshot.BlackScore));
            SetText(whiteScoreText, GameLocalization.WhiteScore(language, snapshot.WhiteScore));
            SetText(restartButtonText, GameLocalization.Restart(language));
            SetText(undoButtonText, GameLocalization.Undo(language));
            SetText(sideButtonText, GameLocalization.SideAction(language, humanSide));
            SetText(spectateButtonText, GameLocalization.SpectateAction(language, mode));
            SetText(difficultyButtonText, GameLocalization.Difficulty(language, difficulty));
            SetText(languageButtonText, GameLocalization.LanguageAction(language));
            SetText(exitButtonText, GameLocalization.Exit(language));
            SetText(statusText, GameLocalization.Status(
                language, snapshot.ActiveColor, snapshot.LastPassedColor, isAiThinking));
            var isGameOver = snapshot.IsGameOver;
            if (resultPanel != null)
            {
                resultPanel.SetActive(isGameOver);
            }

            if (isGameOver)
            {
                SetText(resultText, GameLocalization.Result(language, snapshot.Result));
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
            if (chineseFont != null)
            {
                Destroy(chineseFont);
                chineseFont = null;
            }
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
            languageButton?.onClick.RemoveListener(controller.ToggleLanguage);
            exitButton?.onClick.RemoveListener(controller.ExitGame);
            controller = null;
        }

        private void ApplyLanguageFont(GameLanguage language)
        {
            if (appliedLanguage == language) return;
            if (englishFont == null && blackScoreText != null) englishFont = blackScoreText.font;
            Font target = englishFont;
            if (language == GameLanguage.Chinese)
            {
                if (chineseFont == null)
                {
                    chineseFont = Font.CreateDynamicFontFromOSFont(ChineseFontNames, 32);
                }

                if (chineseFont != null) target = chineseFont;
            }

            if (target != null)
            {
                foreach (var text in GetComponentsInChildren<Text>(true))
                {
                    text.font = target;
                }
            }

            appliedLanguage = language;
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
