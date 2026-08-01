using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class BoardManager
{
    public static int[] Squares;
    public static string fen = "6k1/5ppp/8/8/8/8/1Q5K/8 w - - 0 1";

    static BoardManager()
    {
        Squares = new int[64];
        fenToBoard();
    }

    public static void fenToBoard()
    {
        var dict = new Dictionary<char, int>()
        {
            { 'p', Piece.Pawn },
            { 'n', Piece.Knight },
            { 'b', Piece.Bishop },
            { 'r', Piece.Rook },
            { 'q', Piece.Queen },
            { 'k', Piece.King }
        };

        int fenIndex = 0, boardIndex = 0;
        while (fen[fenIndex] != ' ')
        {
            int currentPiece = 0;

            if (fen[fenIndex] >= 'A' && fen[fenIndex] <= 'z')
            {
                currentPiece |= fen[fenIndex] <= 'Z' ? Piece.White : Piece.Black;
                currentPiece |= dict[char.ToLowerInvariant(fen[fenIndex])];
                Squares[boardIndex] = currentPiece;
                fenIndex++;
                boardIndex++;
            }
            else if (fen[fenIndex] >= '0' && fen[fenIndex] <= '9')
            {
                boardIndex += (int)fen[fenIndex] - '0';
                fenIndex++;
            }
            else
            {
                fenIndex++;
            }
        }
    }
}
