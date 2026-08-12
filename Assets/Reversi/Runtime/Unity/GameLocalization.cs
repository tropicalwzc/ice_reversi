using IceReversi.Core;

namespace IceReversi.Unity
{
    public static class GameLocalization
    {
        public static string BlackScore(GameLanguage language, int score)
        {
            return language == GameLanguage.Chinese ? $"黑棋  {score}" : $"Black  {score}";
        }

        public static string WhiteScore(GameLanguage language, int score)
        {
            return language == GameLanguage.Chinese ? $"白棋  {score}" : $"White  {score}";
        }

        public static string Status(
            GameLanguage language,
            PieceColor activeColor,
            PieceColor passedColor,
            bool isAiThinking)
        {
            var active = ColorName(language, activeColor);
            var status = language == GameLanguage.Chinese
                ? isAiThinking ? $"{active} AI 思考中…" : $"{active}行棋"
                : isAiThinking ? $"{active} AI thinking..." : $"{active} to move";
            if (!passedColor.IsPlayer()) return status;

            var passed = ColorName(language, passedColor);
            return language == GameLanguage.Chinese
                ? $"{passed}停一手 · {status}"
                : $"{passed} passes · {status}";
        }

        public static string Restart(GameLanguage language) =>
            language == GameLanguage.Chinese ? "重新开始" : "Restart";

        public static string Undo(GameLanguage language) =>
            language == GameLanguage.Chinese ? "悔棋" : "Undo";

        public static string SideAction(GameLanguage language, PieceColor humanSide)
        {
            if (language == GameLanguage.Chinese)
            {
                return humanSide == PieceColor.Black ? "执白棋" : "执黑棋";
            }

            return humanSide == PieceColor.Black ? "Play White" : "Play Black";
        }

        public static string SpectateAction(GameLanguage language, GameMode mode)
        {
            if (language == GameLanguage.Chinese)
            {
                return mode == GameMode.AiVersusAi ? "停止观看" : "观看 AI";
            }

            return mode == GameMode.AiVersusAi ? "Stop Watching" : "Watch AI";
        }

        public static string Difficulty(GameLanguage language, AiDifficulty difficulty)
        {
            var name = language == GameLanguage.Chinese ? DifficultyChinese(difficulty) : difficulty.ToString();
            return language == GameLanguage.Chinese ? $"AI：{name}" : $"AI: {name}";
        }

        public static string LanguageAction(GameLanguage language) =>
            language == GameLanguage.Chinese ? "EN" : "中文";

        public static string Exit(GameLanguage language) =>
            language == GameLanguage.Chinese ? "退出" : "Exit";

        public static string Result(GameLanguage language, GameResult result)
        {
            if (language == GameLanguage.Chinese)
            {
                switch (result)
                {
                    case GameResult.BlackWins:
                        return "黑棋获胜";
                    case GameResult.WhiteWins:
                        return "白棋获胜";
                    case GameResult.Draw:
                        return "平局";
                    default:
                        return string.Empty;
                }
            }

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

        public static string ColorName(GameLanguage language, PieceColor color)
        {
            if (language == GameLanguage.Chinese)
            {
                return color == PieceColor.Black ? "黑棋" : color == PieceColor.White ? "白棋" : string.Empty;
            }

            return color == PieceColor.Black ? "Black" : color == PieceColor.White ? "White" : string.Empty;
        }

        private static string DifficultyChinese(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy:
                    return "简单";
                case AiDifficulty.Hard:
                    return "困难";
                case AiDifficulty.Expert:
                    return "专家";
                default:
                    return "普通";
            }
        }
    }
}
