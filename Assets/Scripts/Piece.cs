public static class Piece
{
    public const int None = 0;
    public const int King = 1;
    public const int Pawn = 2;
    public const int Knight = 3;
    public const int Bishop = 4;
    public const int Rook = 5;
    public const int Queen = 6;

    public const int White = 8;
    public const int Black = 16;

    public static int GetType(int piece)
    {
        return piece & 7;
    }

    public static bool IsSlidingPiece(int piece)
    {
        return (piece & 4) != 0;
    }

    public static int GetColor(int piece)
    {
        return piece & 24;
    }

    public static bool IsSameColor(int piece1, int piece2)
    {
        return GetColor(piece1) == GetColor(piece2);
    }

    public static int GetReversedColor(int piece)
    {
        return (piece ^ 24) & 24;
    }
}
