using UnityEngine;

namespace IceReversi.Unity
{
    public sealed class PlayerPrefsLanguageStore
    {
        public const string PreferenceKey = "ice-reversi.language";

        public GameLanguage Load()
        {
            return PlayerPrefs.GetString(PreferenceKey, string.Empty) == "zh"
                ? GameLanguage.Chinese
                : GameLanguage.English;
        }

        public void Save(GameLanguage language)
        {
            PlayerPrefs.SetString(PreferenceKey, language == GameLanguage.Chinese ? "zh" : "en");
            PlayerPrefs.Save();
        }
    }
}
