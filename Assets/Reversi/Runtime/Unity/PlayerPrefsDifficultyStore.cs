using IceReversi.Core;
using UnityEngine;

namespace IceReversi.Unity
{
    public sealed class PlayerPrefsDifficultyStore : IAiDifficultyPreferenceStore
    {
        public const string PreferenceKey = "ice-reversi.ai-difficulty";

        public string Read()
        {
            return PlayerPrefs.GetString(PreferenceKey, string.Empty);
        }

        public void Write(string value)
        {
            PlayerPrefs.SetString(PreferenceKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
