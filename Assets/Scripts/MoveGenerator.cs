using System;
using System.Collections.Generic;
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
    public int capturedPiece;
    public int castlingRookStartSquare;
    public int castlingRookTargetSquare;
    public int enPassantTargetPawnSquare;
    public int previousCastlingRights;
    public int promotedPawn;
    public int previousEnPassantSquare;

    public Move(int startSquare, int targetSquare, MoveType type, int capturedPiece = Piece.None, int rookStart = -1, int rookTarget = -1, int enPassantTargetPawn = -1, int promotedPawn = Piece.None)
    {
        this.startSquare = startSquare;
        this.targetSquare = targetSquare;
        this.type = type;
        this.capturedPiece = capturedPiece;
        castlingRookStartSquare = rookStart;
        castlingRookTargetSquare = rookTarget;
        this.enPassantTargetPawnSquare = enPassantTargetPawn;
        previousCastlingRights = 0;
        this.promotedPawn = promotedPawn;
        previousEnPassantSquare = -1;
    }

    public bool Equals(Move other) => startSquare == other.startSquare && targetSquare == other.targetSquare;

    public Move ReverseMove()
    {
        Move reversedMove = new Move(targetSquare, startSquare, type, capturedPiece, castlingRookTargetSquare, castlingRookStartSquare, enPassantTargetPawnSquare, promotedPawn);
        return reversedMove;
    }
}

public class MoveGenerator : MonoBehaviour
{
    public static MoveGenerator Instance;

    private int[] directionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };
    private int[][] numSquaresToEdge;
    private int[][] knightMoves;
    public List<Move> pseudoLegalMoves = new List<Move>();

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
        pseudoLegalMoves.Clear();
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
                if (Piece.GetType(piece) == Piece.Pawn) {
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

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType, pieceOnTargetSquare));

                // Can't move any further in this direction after capturing opponent's piece
                if (Piece.IsSameColor(pieceOnTargetSquare, Piece.GetReversedColor(piece)))
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

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType, pieceOnTarget));
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

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType, pieceOnTargetSquare));
            }
        }

        // Рокировка разрешена, если:
        // - Ни король, ни ладья до этого не двигались * проверить флаг из BoardManager
        // - Нет никаких фигур между королем и ладьей ** эту проверку нужно выполнить здесь
        // - !Король в данный момент не находится под шахом
        // - !Король не будет находиться под шахом после рокировки
        // - !Вообще ни одна клетка, которую посещает король во время рокировки, не должна находиться под шахом

        // 0-0-0 для белых
        if ((BoardManager.Instance.castlingRights & 2) != 0 && (BoardManager.Instance.squares[4] == (Piece.King | Piece.White)))
        {
            TryAddCastleMove(4, 2, new int[] { 1, 2, 3 }, 0, 3);
        }
        // 0-0 для белых
        if ((BoardManager.Instance.castlingRights & 1) != 0 && (BoardManager.Instance.squares[4] == (Piece.King | Piece.White)))
        {
            TryAddCastleMove(4, 6, new int[] { 5, 6 }, 7, 5);
        }
        // 0-0-0 для черных
        if ((BoardManager.Instance.castlingRights & 8) != 0 && (BoardManager.Instance.squares[60] == (Piece.King | Piece.Black)))
        {
            TryAddCastleMove(60, 58, new int[] { 57, 58, 59 }, 56, 59);
        }
        // 0-0 для черных
        if ((BoardManager.Instance.castlingRights & 4) != 0 && (BoardManager.Instance.squares[60] == (Piece.King | Piece.Black)))
        {
            TryAddCastleMove(60, 62, new int[] { 61, 62 }, 63, 61);
        }
    }

    private void GeneratePawnMoves(int startSquare, int piece)
    {
        int direction = Piece.GetColor(piece) == Piece.White ? 8 : -8;

        int startFile = startSquare % 8, startRank = startSquare / 8;
        int targetSquare = startSquare + direction;

        // Ход на 1 клетку
        if (targetSquare >= 0 && targetSquare < 64 && BoardManager.Instance.squares[targetSquare] == Piece.None)
        {
            AddPawnMove(startSquare, targetSquare, MoveType.Move, promotedPawn: piece);

            // Ход на 2 клетки: мы уже проверили, что на первой клетке пусто, нужно проверить только вторую
            // Ограничение на targetSquare нас не волнует, потому что мы начинаем с 1 или 6 ранга (индексы)
            bool isStartingPos =    (Piece.GetColor(piece) == Piece.White && startRank == 1) ||
                                    (Piece.GetColor(piece) == Piece.Black && startRank == 6);
            targetSquare = startSquare + direction * 2;
            if (isStartingPos && BoardManager.Instance.squares[targetSquare] == Piece.None)
            {
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Move));
            }
        }

        // Взятие
        int[] captureOffsets = { direction - 1, direction + 1 };

        foreach (var offset in captureOffsets)
        {
            targetSquare = startSquare + offset;

            // Защита от переноса: целевая клетка должна быть на соседнем файле
            if (targetSquare < 0 || targetSquare >= 64 || Math.Abs(targetSquare % 8 - startFile) != 1)
                continue;

            int targetPiece = BoardManager.Instance.squares[targetSquare];

            if (targetPiece != Piece.None && !Piece.IsSameColor(piece, targetPiece))
            {
                AddPawnMove(startSquare, targetSquare, MoveType.Take, targetPiece, promotedPawn: piece);
            }
            else if (targetSquare == BoardManager.Instance.enPassantTargetSquare)
            {
                // Сразу же вычисляем клетку пешки, которую мы будем рубить
                int enPassantedPawnSquare = Piece.GetColor(piece) == Piece.White ? targetSquare - 8 : targetSquare + 8;
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.EnPassant, BoardManager.Instance.squares[enPassantedPawnSquare], - 1, -1, enPassantedPawnSquare));
            }
        }
    }

    private void TryAddCastleMove(int startSquare, int targetSquare, int[] pathSquares, int rookStart, int rookTarget)
    {
        for (int i = 0; i < pathSquares.Length; i++)
        {
            if (BoardManager.Instance.squares[pathSquares[i]] != 0)
                return;
        }

        pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Castle, 0, rookStart, rookTarget));
    }

    private void AddPawnMove(int startSquare, int targetSquare, MoveType baseType, int capturedPiece = Piece.None, int promotedPawn = Piece.None)
    {
        // Если пешка оказывается на последнем или первом ранге, то это точно превращение
        if (targetSquare / 8 == 7 || targetSquare / 8 == 0)
        {
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote, capturedPiece, promotedPawn: promotedPawn));
        }
        else
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, baseType, capturedPiece));
    }
}
