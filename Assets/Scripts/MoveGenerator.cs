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

    public int[] DirectionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };
    public int[][] numSquaresToEdge;
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
        numSquaresToEdge = new int[64][];
        PrecomputedMoveData();
        pseudoLegalMoves = GenerateMoves();
    }

    public void PrecomputedMoveData()
    {
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
            }
        }
        return pseudoLegalMoves;
    }

    void GenerateSlidingMoves(int startSquare, int piece)
    {
        int startDirIndex = Piece.GetType(piece) == Piece.Bishop ? 4 : 0;
        int endDirIndex = Piece.GetType(piece) == Piece.Rook ? 4 : 8;

        for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
        {
            for (int n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++)
            {
                int targetSquare = startSquare + DirectionOffsets[directionIndex] * (n + 1);
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

}
