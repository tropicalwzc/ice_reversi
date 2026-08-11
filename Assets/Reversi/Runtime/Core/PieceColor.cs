namespace IceReversi.Core
{
    public enum PieceColor
    {
        White = -1,
        Empty = 0,
        Black = 1
    }

    public static class PieceColorExtensions
    {
        public static PieceColor Opponent(this PieceColor color)
        {
            return color == PieceColor.Black ? PieceColor.White :
                color == PieceColor.White ? PieceColor.Black : PieceColor.Empty;
        }

        public static bool IsPlayer(this PieceColor color)
        {
            return color == PieceColor.Black || color == PieceColor.White;
        }
    }
}
