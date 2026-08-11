namespace IceReversi.Core
{
    public sealed class AiSearchOptions
    {
        public static AiSearchOptions Default => new AiSearchOptions();

        public int MaximumDepth { get; set; } = 4;
        public int MaximumNodes { get; set; } = 60000;
        public int MaximumThinkTimeMilliseconds { get; set; } = 500;
        public int TranspositionCapacity { get; set; } = 16384;
        public int ExactEndgameThreshold { get; set; }
        public AiDifficulty Difficulty { get; set; } = AiDifficulty.Normal;
        public AiRootChoicePolicy RootChoicePolicy { get; set; } = AiRootChoicePolicy.BestScore;
        public int RandomSeed { get; set; } = 1977;

        internal AiSearchOptions Sanitized()
        {
            return new AiSearchOptions
            {
                MaximumDepth = MaximumDepth < 1 ? 1 : MaximumDepth > 64 ? 64 : MaximumDepth,
                MaximumNodes = MaximumNodes < 1 ? 1 : MaximumNodes,
                MaximumThinkTimeMilliseconds = MaximumThinkTimeMilliseconds < 1 ? 1 : MaximumThinkTimeMilliseconds,
                TranspositionCapacity = TranspositionCapacity < 0 ? 0 : TranspositionCapacity > 1048576 ? 1048576 : TranspositionCapacity,
                ExactEndgameThreshold = ExactEndgameThreshold < 0 ? 0 : ExactEndgameThreshold > 20 ? 20 : ExactEndgameThreshold,
                Difficulty = AiDifficultyProfile.IsDefined(Difficulty) ? Difficulty : AiDifficulty.Normal,
                RootChoicePolicy = RootChoicePolicy == AiRootChoicePolicy.ControlledTopThree
                    ? AiRootChoicePolicy.ControlledTopThree
                    : AiRootChoicePolicy.BestScore,
                RandomSeed = RandomSeed
            };
        }
    }
}
