using System;

namespace IceReversi.Core
{
    public sealed class AiDifficultyProfile
    {
        private AiDifficultyProfile(
            AiDifficulty difficulty,
            int maximumDepth,
            int maximumNodes,
            int maximumThinkTimeMilliseconds,
            int transpositionCapacity,
            int exactEndgameThreshold,
            AiRootChoicePolicy rootChoicePolicy)
        {
            Difficulty = difficulty;
            MaximumDepth = maximumDepth;
            MaximumNodes = maximumNodes;
            MaximumThinkTimeMilliseconds = maximumThinkTimeMilliseconds;
            TranspositionCapacity = transpositionCapacity;
            ExactEndgameThreshold = exactEndgameThreshold;
            RootChoicePolicy = rootChoicePolicy;
        }

        public AiDifficulty Difficulty { get; }
        public int MaximumDepth { get; }
        public int MaximumNodes { get; }
        public int MaximumThinkTimeMilliseconds { get; }
        public int TranspositionCapacity { get; }
        public int ExactEndgameThreshold { get; }
        public AiRootChoicePolicy RootChoicePolicy { get; }

        public AiSearchOptions CreateSearchOptions(int randomSeed = 1977)
        {
            return new AiSearchOptions
            {
                Difficulty = Difficulty,
                MaximumDepth = MaximumDepth,
                MaximumNodes = MaximumNodes,
                MaximumThinkTimeMilliseconds = MaximumThinkTimeMilliseconds,
                TranspositionCapacity = TranspositionCapacity,
                ExactEndgameThreshold = ExactEndgameThreshold,
                RootChoicePolicy = RootChoicePolicy,
                RandomSeed = randomSeed
            };
        }

        public static AiDifficultyProfile For(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy:
                    return Easy;
                case AiDifficulty.Hard:
                    return Hard;
                case AiDifficulty.Expert:
                    return Expert;
                default:
                    return Normal;
            }
        }

        public static AiDifficulty Next(AiDifficulty difficulty)
        {
            return difficulty == AiDifficulty.Expert
                ? AiDifficulty.Easy
                : (AiDifficulty)((int)difficulty + 1);
        }

        public static bool IsDefined(AiDifficulty difficulty)
        {
            return Enum.IsDefined(typeof(AiDifficulty), difficulty);
        }

        public static AiDifficultyProfile Easy { get; } = new AiDifficultyProfile(
            AiDifficulty.Easy, 2, 5000, 120, 2048, 0, AiRootChoicePolicy.ControlledTopThree);

        public static AiDifficultyProfile Normal { get; } = new AiDifficultyProfile(
            AiDifficulty.Normal, 6, 60000, 350, 16384, 0, AiRootChoicePolicy.BestScore);

        public static AiDifficultyProfile Hard { get; } = new AiDifficultyProfile(
            AiDifficulty.Hard, 8, 250000, 900, 65536, 10, AiRootChoicePolicy.BestScore);

        public static AiDifficultyProfile Expert { get; } = new AiDifficultyProfile(
            AiDifficulty.Expert, 10, 750000, 1800, 131072, 12, AiRootChoicePolicy.BestScore);
    }
}
