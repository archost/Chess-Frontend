using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEditorInternal;
using UnityEngine;

public enum MoveType
{
    Move, 
    Take,
    Castle,
    Promote
}

public struct Move
{
    public int startSquare;
    public int targetSquare;

    public Move(int startSquare, int targetSquare)
    {
        this.startSquare = startSquare;
        this.targetSquare = targetSquare;
    }

    public bool Equals(Move other) => startSquare == other.startSquare && targetSquare == other.targetSquare;
}

public class MoveGenerator : MonoBehaviour
{
    public static MoveGenerator Instance;

    private int[] directionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };
    private int[][] numSquaresToEdge;
    private int[][] knightMoves;
    public List<Move> pseudoLegalMoves;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PrecomputedMoveData();
        pseudoLegalMoves = GenerateMoves();
    }

    public void PrecomputedMoveData()
    {
        numSquaresToEdge = new int[64][];
        // Sliding pieces
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int index = rank * 8 + file;

                int north = 7 - rank;
                int south = rank;
                int west = file;
                int east = 7 - file;

                numSquaresToEdge[index] = new int[]
                {
                    north,
                    south,
                    west, 
                    east,
                    Math.Min(north, west),
                    Math.Min(south, east),
                    Math.Min(north, east),
                    Math.Min(south, west),
                };
            }
        }

        // Knight
        knightMoves = new int[64][];
        
        int[] dr = { 2, 2, -2, -2, 1, 1, -1, -1 };
        int[] df = { 1, -1, 1, -1, 2, -2, 2, -2 };

        for (int square = 0; square < 64; square++)
        {
            int rank = square / 8;
            int file = square % 8;

            List<int> validMoves = new List<int>();

            for (int i = 0; i < 8; i++)
            {
                int newRank = rank + dr[i];
                int newFile = file + df[i];

                if (newRank >= 0 && newRank < 8 && newFile >= 0 && newFile < 8)
                {
                    int targetSquare = newRank * 8 + newFile;
                    validMoves.Add(targetSquare);
                }
            }

            knightMoves[square] = validMoves.ToArray();
        }
    }
    
    public List<Move> GenerateMoves()
    {
        pseudoLegalMoves = new List<Move>();
        for (int startSquare = 0; startSquare < 64; startSquare++)
        {
            int piece = BoardManager.Instance.squares[startSquare];
            if (Piece.IsSameColor(piece, BoardManager.Instance.colorToMove))
            {
                if (Piece.IsSlidingPiece(piece))
                {
                    GenerateSlidingMoves(startSquare, piece);
                }
                if (Piece.GetType(piece) == Piece.Knight)
                {
                    GenerateKnightMoves(startSquare, piece);
                }
                if (Piece.GetType(piece) == Piece.King)
                {
                    GenerateKingMoves(startSquare, piece);
                }
                if (Piece.GetType(piece).Equals(Piece.Pawn)){
                    GeneratePawnMoves(startSquare, piece);
                }
            }
        }
        return pseudoLegalMoves;
    }

    private void GenerateSlidingMoves(int startSquare, int piece)
    {
        int startDirIndex = Piece.GetType(piece) == Piece.Bishop ? 4 : 0;
        int endDirIndex = Piece.GetType(piece) == Piece.Rook ? 4 : 8;

        for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
        {
            for (int n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++)
            {
                int targetSquare = startSquare + directionOffsets[directionIndex] * (n + 1);
                int pieceOnTargetSquare = BoardManager.Instance.squares[targetSquare];

                // Blocked by friendly piece, so can't move any further in this direction
                if (Piece.IsSameColor(pieceOnTargetSquare, piece))
                {
                    break;
                }

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare));

                // Can't move any further in this direction after capturing opponent's piece
                int opponentColor = piece ^ 24;
                if (Piece.IsSameColor(pieceOnTargetSquare, opponentColor))
                {
                    break;
                }
            }
        }
    }

    private void GenerateKnightMoves(int startSquare, int piece)
    {
        int[] targets = knightMoves[startSquare];

        for (int i = 0; i < targets.Length; i++)
        {
            int targetSquare = targets[i];
            int pieceOnTarget = BoardManager.Instance.squares[targetSquare];

            if (!Piece.IsSameColor(pieceOnTarget, piece))
            {
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare));
            }
        }
    }

    private void GenerateKingMoves(int startSquare, int piece)
    {
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            if (numSquaresToEdge[startSquare][directionIndex] > 0)
            {
                // ≈сли в определенную сторону есть хот€ бы одна клетка дл€ хода, то можно туда ходить
                // нужно только проверить, не стоит ли там наша фигура
                int targetSquare = startSquare + directionOffsets[directionIndex];
                int pieceOnTargetSquare = BoardManager.Instance.squares[targetSquare];

                if (Piece.IsSameColor(pieceOnTargetSquare, piece))
                {
                    continue;
                }

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare));
            }
        }
    }

    private void GeneratePawnMoves(int startSquare, int piece)
    {
        int direction = Piece.GetColor(piece) == Piece.White ? 8 : -8;
        // ’од на 1 клетку
        int targetSquare = startSquare + direction;
        if (BoardManager.Instance.squares[targetSquare] == Piece.None)
        {
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare));
        }

        // ’од на 2 клетки
        bool isPawnOnStartingPos = ((Piece.GetColor(piece) == Piece.White) && (startSquare / 8 == 1)) ||
            ((Piece.GetColor(piece) == Piece.Black) && (startSquare / 8 == 6));

        targetSquare = startSquare + direction * 2;
        if (isPawnOnStartingPos && BoardManager.Instance.squares[targetSquare] == Piece.None)
        {
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare));
        }

        // ¬з€тие (нужно проверить две клетки по диагонали)
        targetSquare = startSquare + direction + 1;
        if (BoardManager.Instance.squares[targetSquare] != Piece.None)
        {
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare));
        }

        targetSquare = startSquare + direction - 1;
        if (BoardManager.Instance.squares[targetSquare] != Piece.None)
        {
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare));
        }
    }
}
