namespace IceReversi.Core
{
    public interface IAiDifficultyPreferenceStore
    {
        string Read();
        void Write(string value);
    }

    public sealed class AiDifficultyPreferences
    {
        private readonly IAiDifficultyPreferenceStore store;

        public AiDifficultyPreferences(IAiDifficultyPreferenceStore store)
        {
            this.store = store;
        }

        public AiDifficulty Load()
        {
            if (store == null)
            {
                return AiDifficulty.Normal;
            }

            if (System.Enum.TryParse(store.Read(), true, out AiDifficulty parsed) &&
                AiDifficultyProfile.IsDefined(parsed))
            {
                return parsed;
            }

            return AiDifficulty.Normal;
        }

        public void Save(AiDifficulty difficulty)
        {
            if (store == null || !AiDifficultyProfile.IsDefined(difficulty))
            {
                return;
            }

            store.Write(difficulty.ToString().ToLowerInvariant());
        }
    }
}
