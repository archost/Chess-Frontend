using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEditorInternal;
using UnityEngine;

public enum MoveType
{
    Undefined,
    Move, 
    Take,
    Castle,
    Promote,
    EnPassant
}

public struct Move
{
    public int startSquare;
    public int targetSquare;
    public MoveType type;

    public Move(int startSquare, int targetSquare, MoveType type)
    {
        this.startSquare = startSquare;
        this.targetSquare = targetSquare;
        this.type = type;
    }

    public bool Equals(Move other) => startSquare == other.startSquare && targetSquare == other.targetSquare;

    public MoveType GetMoveType() => type;
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

            List<int> pseudoValidMovesForASquare = new List<int>();

            for (int i = 0; i < 8; i++)
            {
                int newRank = rank + dr[i];
                int newFile = file + df[i];

                if (newRank >= 0 && newRank < 8 && newFile >= 0 && newFile < 8)
                {
                    int targetSquare = newRank * 8 + newFile;
                    pseudoValidMovesForASquare.Add(targetSquare);
                }
            }

            knightMoves[square] = pseudoValidMovesForASquare.ToArray();
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

                MoveType moveType = MoveType.Move;

                // Проверим, берем ли мы фигуру, совершая этот ход
                if (pieceOnTargetSquare != 0)
                {
                    moveType = MoveType.Take;
                }

                // Blocked by friendly piece, so can't move any further in this direction
                if (Piece.IsSameColor(pieceOnTargetSquare, piece))
                {
                    break;
                }

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType));

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

            MoveType moveType = MoveType.Move;

            if (!Piece.IsSameColor(pieceOnTarget, piece))
            {
                if (pieceOnTarget != 0)
                    moveType = MoveType.Take;

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType));
            }
        }
    }

    private void GenerateKingMoves(int startSquare, int piece)
    {
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            if (numSquaresToEdge[startSquare][directionIndex] > 0)
            {
                // Если в определенную сторону есть хотя бы одна клетка для хода, то можно туда ходить
                // нужно только проверить, не стоит ли там наша фигура
                int targetSquare = startSquare + directionOffsets[directionIndex];
                int pieceOnTargetSquare = BoardManager.Instance.squares[targetSquare];

                MoveType moveType = MoveType.Move;

                if (Piece.IsSameColor(pieceOnTargetSquare, piece))
                {
                    continue;
                }

                if (pieceOnTargetSquare != 0)
                    moveType = MoveType.Take;

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType));
            }
        }

        // Рокировка
        // По сути мы всегда нажимаем на 2 и 6 за белых, и 58 и 62 за черных (0-0-0 и 0-0 соответственно)
        // Нужно ли нам прямо здесь проверять, можно ли рокироваться?
        // Хотя бы по флагу? Да
        // По шаху? Пока непонятно

        // Рокировка разрешена, если:
        // - Ни король, ни ладья до этого не двигались * проверить флаг из BoardManager
        // - Нет никаких фигур между королем и ладьей ** эту проверку нужно выполнить здесь
        // - !Король в данный момент не находится под шахом
        // - !Король не будет находиться под шахом после рокировки

        // 0-0-0 для белых
        if (BoardManager.Instance.canWhiteCastleQueenside && (BoardManager.Instance.squares[4] == (Piece.King | Piece.White)))
        {
            // Индексы 1, 2 и 3 - пустые
            if (BoardManager.Instance.squares[1] == 0 &&
                BoardManager.Instance.squares[2] == 0 &&
                BoardManager.Instance.squares[3] == 0)
            {
                pseudoLegalMoves.Add(new Move(4, 2, MoveType.Castle));
            }
        }
        // 0-0 для белых
        if (BoardManager.Instance.canWhiteCastleKingside && (BoardManager.Instance.squares[4] == (Piece.King | Piece.White)))
        {
            if (BoardManager.Instance.squares[5] == 0 &&
                BoardManager.Instance.squares[6] == 0)
            {
                pseudoLegalMoves.Add(new Move(4, 6, MoveType.Castle));
            }
        }
        // 0-0-0 для черных
        if (BoardManager.Instance.canBlackCastleQueenside && (BoardManager.Instance.squares[60] == (Piece.King | Piece.Black)))
        {
            if (BoardManager.Instance.squares[57] == 0 &&
                BoardManager.Instance.squares[58] == 0 &&
                BoardManager.Instance.squares[59] == 0)
            {
                pseudoLegalMoves.Add(new Move(60, 58, MoveType.Castle));
            }
        }
        // 0-0 для черных
        if (BoardManager.Instance.canBlackCastleKingside && (BoardManager.Instance.squares[60] == (Piece.King | Piece.Black)))
        {
            if (BoardManager.Instance.squares[61] == 0 &&
                BoardManager.Instance.squares[62] == 0)
            {
                pseudoLegalMoves.Add(new Move(60, 62, MoveType.Castle));
            }
        }
    }

    private void GeneratePawnMoves(int startSquare, int piece)
    {
        int direction = Piece.GetColor(piece) == Piece.White ? 8 : -8;
        // Ход на 1 клетку
        int targetSquare = startSquare + direction;
        if (targetSquare >= 0 && targetSquare < 64 && 
            BoardManager.Instance.squares[targetSquare] == Piece.None)
        {
            if (targetSquare / 8 == 7 || targetSquare / 8 == 0)
            {
                // Promotion
                Debug.Log("Move " + startSquare + " to " + targetSquare + " is a promotion move!");
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote));
            }
            else
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Move));
        }

        // Ход на 2 клетки
        bool isPawnOnStartingPos = ((Piece.GetColor(piece) == Piece.White) && (startSquare / 8 == 1)) ||
            ((Piece.GetColor(piece) == Piece.Black) && (startSquare / 8 == 6));

        targetSquare = startSquare + direction * 2;
        if (targetSquare >= 0 && targetSquare < 64 && isPawnOnStartingPos && 
            BoardManager.Instance.squares[targetSquare] == Piece.None &&
            BoardManager.Instance.squares[startSquare + direction] == Piece.None)
        {
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Move));
        }

        // Взятие + на проходе (нужно проверить две клетки по диагонали)
        targetSquare = startSquare + direction + 1;
        if (targetSquare >= 0 && targetSquare < 64)
        {
            if (BoardManager.Instance.squares[targetSquare] != Piece.None &&
                !Piece.IsSameColor(BoardManager.Instance.squares[targetSquare], piece))
            {
                if (targetSquare / 8 == 7 || targetSquare / 8 == 0)
                {
                    // Promotion
                    Debug.Log("Move " + startSquare + " to " + targetSquare + " is a promotion move!");
                    pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote));
                }
                else
                    pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Take));
            }
            else if (targetSquare == BoardManager.Instance.enPassantTargetSquare)
            {
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.EnPassant));
            }
        }

        targetSquare = startSquare + direction - 1;
        if (targetSquare >= 0 && targetSquare < 64)
        {
            if (BoardManager.Instance.squares[targetSquare] != Piece.None &&
            !Piece.IsSameColor(BoardManager.Instance.squares[targetSquare], piece))
            {
                if (targetSquare / 8 == 7 || targetSquare / 8 == 0)
                {
                    // Promotion
                    Debug.Log("Move " + startSquare + " to " + targetSquare + " is a promotion move!");
                    pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote));
                }
                else
                    pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Take));
            }
            else if (targetSquare == BoardManager.Instance.enPassantTargetSquare)
            {
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.EnPassant));
            }
        }

    }
}
