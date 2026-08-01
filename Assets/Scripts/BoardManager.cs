using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public int[] Squares;
    public string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Squares = new int[64];
            FenToBoard();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void FenToBoard()
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
