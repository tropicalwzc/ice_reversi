namespace IceReversi.Core
{
    public enum AiDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3
    }

    public enum AiRootChoicePolicy
    {
        BestScore = 0,
        ControlledTopThree = 1
    }
}
