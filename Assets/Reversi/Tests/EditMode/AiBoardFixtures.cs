using IceReversi.Core;

namespace IceReversi.Tests
{
    internal static class AiBoardFixtures
    {
        public static BoardState CornerCapture => BoardState.FromRows(
            ".WB.....",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");

        public static BoardState UnsafeCornerAdjacency => BoardState.FromRows(
            ".B......",
            "BW......",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");

        public static BoardState ForcedPass => BoardState.FromRows(
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BW.BW.BB");

        public static BoardState MobilityTradeOff => BoardState.FromRows(
            "........",
            "...B....",
            "..BBB...",
            ".BWWW...",
            "..BWW...",
            "...W....",
            "........",
            "........");

        public static BoardState ExactEndgame => BoardState.FromRows(
            "BBBBBBBB",
            "BWWWWWWB",
            "BWBBBBWB",
            "BWBWWBWB",
            "BWBWWBWB",
            "BWBBBBWB",
            "BWWWWW..",
            "BBBBB..." );
    }
}
