using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.Audio.ProcessorInstance;

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
    public int promoteTo;
    public int piece;

    public Move(int startSquare, int targetSquare, MoveType type, int capturedPiece = Piece.None, int rookStart = -1, int rookTarget = -1, int enPassantTargetPawn = -1, int promotedPawn = Piece.None, int promoteTo = Piece.None, int piece = Piece.None)
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
        this.promoteTo = promoteTo;
        this.piece = piece;
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

    private int[] _directionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };
    private int[][] _numSquaresToEdge;
    private int[][] _knightMoves;

    public List<Move> legalMoves;

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
    }

    public void GenerateLegalMoves(int colorToMove)
    {
        legalMoves = FilterLegalMoves(colorToMove);
    }

    public void PrecomputedMoveData()
    {
        _numSquaresToEdge = new int[64][];

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

                _numSquaresToEdge[index] = new int[]
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
        _knightMoves = new int[64][];

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

            _knightMoves[square] = pseudoValidMovesForASquare.ToArray();
        }
    }

    public List<Move> GeneratePseudoLegalMoves(int colorToMove)
    {
        List<Move> pseudoLegalMoves = new List<Move>();

        for (int startSquare = 0; startSquare < 64; startSquare++)
        {
            int piece = BoardManager.Instance.squares[startSquare];
            if (Piece.IsSameColor(piece, colorToMove))
            {
                if (Piece.IsSlidingPiece(piece))
                {
                    GenerateSlidingMoves(ref pseudoLegalMoves, startSquare, piece);
                }
                if (Piece.GetType(piece) == Piece.Knight)
                {
                    GenerateKnightMoves(ref pseudoLegalMoves, startSquare, piece);
                }
                if (Piece.GetType(piece) == Piece.King)
                {
                    GenerateKingMoves(ref pseudoLegalMoves, startSquare, piece);
                }
                if (Piece.GetType(piece) == Piece.Pawn) {
                    GeneratePawnMoves(ref pseudoLegalMoves, startSquare, piece);
                }
            }
        }

        return pseudoLegalMoves;
    }

    public List<Move> FilterLegalMoves(int colorToMove)
    {
        List<Move> pseudoLegalMoves = GeneratePseudoLegalMoves(colorToMove);
        List<Move> legalMoves = new List<Move>();
        
        foreach (Move moveToVerify in pseudoLegalMoves)
        {
            BoardManager.Instance.ProcessMove(moveToVerify, false, silent: true);

            int opponentColor = Piece.GetReversedColor(colorToMove);
            int kingIndex = BoardManager.Instance.FindPiece(Piece.King | Piece.GetReversedColor(opponentColor));

            bool isKingAttacked = IsSquareAttacked(kingIndex, opponentColor);
            bool isCastlingAllowed = true;

            int[] castlingSquaresToCheck = { };

            if (moveToVerify.type == MoveType.Castle)
            {
                if (Piece.GetReversedColor(opponentColor) == Piece.White)
                {
                    // 0-0-0
                    if (moveToVerify.castlingRookStartSquare == 0)
                    {
                        castlingSquaresToCheck = new int[3] { 2, 3, 4 };
                    }
                    // 0-0
                    else if (moveToVerify.castlingRookStartSquare == 7)
                    {
                        castlingSquaresToCheck = new int[3] { 4, 5, 6 };
                    }
                }
                else
                {
                    // 0-0-0
                    if (moveToVerify.castlingRookStartSquare == 56)
                    {
                        castlingSquaresToCheck = new int[3] { 58, 59, 60 };
                    }
                    // 0-0
                    else if (moveToVerify.castlingRookStartSquare == 63)
                    {
                        castlingSquaresToCheck = new int[3] { 60, 61, 62 };
                    }
                }

                foreach (int index in castlingSquaresToCheck)
                {
                    if (IsSquareAttacked(index, opponentColor))
                        isCastlingAllowed = false;
                }
            }

            if (!isKingAttacked && isCastlingAllowed)
            {
                legalMoves.Add(moveToVerify);
            }

            BoardManager.Instance.ProcessMove(moveToVerify, true, silent: true);
        }

        return legalMoves;
    }

    public int MoveGenerationTest(int depth, ref string movesPositions, int detailedDepth = 0)
    {
        if (depth == 0)
            return 1;
        List<Move> moves = FilterLegalMoves(BoardManager.Instance.colorToMove);
        int numPositions = 0;

        foreach (Move move in moves)
        {

            BoardManager.Instance.ProcessMove(move, false, silent: true);

            int positions = MoveGenerationTest(depth - 1, ref movesPositions, detailedDepth);
            numPositions += positions;

            if (depth == detailedDepth)
            {
                movesPositions += BoardManager.Instance.moveToText(move) + ": " + positions + "\n";
            }

            BoardManager.Instance.ProcessMove(move, true, silent: true);
        }

        return numPositions;
    }

    public bool IsSquareAttacked(int squareIndex, int opponentColor)
    {
        bool attackedByPawn = false, attackedByKnight = false, attackedByKing = false, attackedBySlidingPiece = false;

        // ѕроверка пешек
        int[] pawnSquaresOffset = { 7, 9 };

        foreach (int offset in pawnSquaresOffset)
        {
            int pawnSquare = opponentColor == Piece.Black ? squareIndex + offset : squareIndex - offset;
            if (pawnSquare < 0 || pawnSquare >= 64 || Math.Abs(pawnSquare % 8 - squareIndex % 8) != 1)
                continue;
            if (BoardManager.Instance.squares[pawnSquare] == (Piece.Pawn | opponentColor))
                attackedByPawn = true;
        }

        // ѕроверка королей
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            if (_numSquaresToEdge[squareIndex][directionIndex] > 0)
            {
                int targetSquare = squareIndex + _directionOffsets[directionIndex];
                int pieceOnTargetSquare = BoardManager.Instance.squares[targetSquare];
                if (pieceOnTargetSquare == (Piece.King | opponentColor))
                    attackedByKing = true;
            }
        }

        // ѕроверка коней
        int[] knightSquares = _knightMoves[squareIndex];

        foreach (int square in knightSquares)
        {
            if (BoardManager.Instance.squares[square] == (Piece.Knight | opponentColor))
            {
                attackedByKnight = true;
            }
        }

        // ѕроверка слайдеров

        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            for (int n = 0; n < _numSquaresToEdge[squareIndex][directionIndex]; n++)
            {
                int targetSquare = squareIndex + _directionOffsets[directionIndex] * (n + 1);
                int pieceOnTargetSquare = BoardManager.Instance.squares[targetSquare];
                int pieceOnTargetSquareType = Piece.GetType(pieceOnTargetSquare);
                if (Piece.IsSameColor(pieceOnTargetSquare, Piece.GetReversedColor(opponentColor)))
                {
                    break;
                }

                // Can't move any further in this direction after capturing opponent's piece
                if (Piece.IsSameColor(pieceOnTargetSquare, opponentColor))
                {
                    if (pieceOnTargetSquareType == Piece.Pawn || 
                        pieceOnTargetSquareType == Piece.King ||
                        pieceOnTargetSquareType == Piece.Knight)
                        break;
                    if (pieceOnTargetSquareType == Piece.Queen ||
                        pieceOnTargetSquareType == Piece.Rook && directionIndex >= 0 && directionIndex < 4 ||
                        pieceOnTargetSquareType == Piece.Bishop && directionIndex >= 4 && directionIndex < 8)
                    {
                        attackedBySlidingPiece = true;
                        break;
                    }
                    break;
                }
            }
        }

        if (attackedByPawn || attackedByKnight || attackedByKing || attackedBySlidingPiece)
        {
            return true;
        }

        return false;
    }

    /*
    public List<Move> FilterLegalMoves()
    {
        List<Move> pseudoLegalMoves = GeneratePseudoLegalMoves();
        List<Move> legalMoves = new List<Move>();
        foreach (Move moveToVerify in pseudoLegalMoves)
        {
            BoardManager.Instance.ProcessMove(moveToVerify, false, silent: true);

            List<Move> opponentResponses = GeneratePseudoLegalMoves();

            if (!opponentResponses.Any(response => Piece.GetType(response.capturedPiece) == Piece.King))
                legalMoves.Add(moveToVerify);

            BoardManager.Instance.ProcessMove(moveToVerify, true, silent: true);
        }
        return legalMoves;
    }
    */

    private void GenerateSlidingMoves(ref List<Move> pseudoLegalMoves, int startSquare, int piece)
    {
        int startDirIndex = Piece.GetType(piece) == Piece.Bishop ? 4 : 0;
        int endDirIndex = Piece.GetType(piece) == Piece.Rook ? 4 : 8;

        for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
        {
            for (int n = 0; n < _numSquaresToEdge[startSquare][directionIndex]; n++)
            {
                int targetSquare = startSquare + _directionOffsets[directionIndex] * (n + 1);
                int pieceOnTargetSquare = BoardManager.Instance.squares[targetSquare];

                MoveType moveType = MoveType.Move;

                // ѕроверим, берем ли мы фигуру, соверша€ этот ход
                if (pieceOnTargetSquare != 0)
                {
                    moveType = MoveType.Take;
                }

                // Blocked by friendly piece, so can't move any further in this direction
                if (Piece.IsSameColor(pieceOnTargetSquare, piece))
                {
                    break;
                }

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType, pieceOnTargetSquare, piece: piece));

                // Can't move any further in this direction after capturing opponent's piece
                if (Piece.IsSameColor(pieceOnTargetSquare, Piece.GetReversedColor(piece)))
                {
                    break;
                }
            }
        }
    }

    private void GenerateKnightMoves(ref List<Move> pseudoLegalMoves, int startSquare, int piece)
    {
        int[] targets = _knightMoves[startSquare];

        for (int i = 0; i < targets.Length; i++)
        {
            int targetSquare = targets[i];
            int pieceOnTarget = BoardManager.Instance.squares[targetSquare];

            MoveType moveType = MoveType.Move;

            if (!Piece.IsSameColor(pieceOnTarget, piece))
            {
                if (pieceOnTarget != 0)
                    moveType = MoveType.Take;

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType, pieceOnTarget, piece: piece));
            }
        }
    }

    private void GenerateKingMoves(ref List<Move> pseudoLegalMoves, int startSquare, int piece)
    {
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            if (_numSquaresToEdge[startSquare][directionIndex] > 0)
            {
                // ≈сли в определенную сторону есть хот€ бы одна клетка дл€ хода, то можно туда ходить
                // нужно только проверить, не стоит ли там наша фигура
                int targetSquare = startSquare + _directionOffsets[directionIndex];
                int pieceOnTargetSquare = BoardManager.Instance.squares[targetSquare];

                MoveType moveType = MoveType.Move;

                if (Piece.IsSameColor(pieceOnTargetSquare, piece))
                {
                    continue;
                }

                if (pieceOnTargetSquare != 0)
                    moveType = MoveType.Take;

                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, moveType, pieceOnTargetSquare, piece: piece));
            }
        }

        // –окировка разрешена, если:
        // - Ќи король, ни ладь€ до этого не двигались * проверить флаг из BoardManager
        // - Ќет никаких фигур между королем и ладьей ** эту проверку нужно выполнить здесь
        // - ! ороль в данный момент не находитс€ под шахом
        // - ! ороль не будет находитьс€ под шахом после рокировки
        // - !¬ообще ни одна клетка, которую посещает король во врем€ рокировки, не должна находитьс€ под шахом

        if (Piece.GetColor(piece) == Piece.White)
        {
            // 0-0-0 дл€ белых
            if ((BoardManager.Instance.castlingRights & 2) != 0 && (BoardManager.Instance.squares[4] == (Piece.King | Piece.White)))
            {
                TryAddCastleMove(ref pseudoLegalMoves, 4, 2, new int[] { 1, 2, 3 }, 0, 3, piece);
            }
            // 0-0 дл€ белых
            if ((BoardManager.Instance.castlingRights & 1) != 0 && (BoardManager.Instance.squares[4] == (Piece.King | Piece.White)))
            {
                TryAddCastleMove(ref pseudoLegalMoves, 4, 6, new int[] { 5, 6 }, 7, 5, piece);
            }
        }

        if (Piece.GetColor(piece) == Piece.Black)
        {
            // 0-0-0 дл€ черных
            if ((BoardManager.Instance.castlingRights & 8) != 0 && (BoardManager.Instance.squares[60] == (Piece.King | Piece.Black)))
            {
                TryAddCastleMove(ref pseudoLegalMoves, 60, 58, new int[] { 57, 58, 59 }, 56, 59, piece);
            }
            // 0-0 дл€ черных
            if ((BoardManager.Instance.castlingRights & 4) != 0 && (BoardManager.Instance.squares[60] == (Piece.King | Piece.Black)))
            {
                TryAddCastleMove(ref pseudoLegalMoves, 60, 62, new int[] { 61, 62 }, 63, 61, piece);
            }
        }
    }

    private void GeneratePawnMoves(ref List<Move> pseudoLegalMoves, int startSquare, int piece)
    {
        int direction = Piece.GetColor(piece) == Piece.White ? 8 : -8;

        int startFile = startSquare % 8, startRank = startSquare / 8;
        int targetSquare = startSquare + direction;

        // ’од на 1 клетку
        if (targetSquare >= 0 && targetSquare < 64 && BoardManager.Instance.squares[targetSquare] == Piece.None)
        {
            AddPawnMove(ref pseudoLegalMoves, startSquare, targetSquare, MoveType.Move, promotedPawn: piece, piece: piece);

            // ’од на 2 клетки: мы уже проверили, что на первой клетке пусто, нужно проверить только вторую
            // ќграничение на targetSquare нас не волнует, потому что мы начинаем с 1 или 6 ранга (индексы)
            bool isStartingPos =    (Piece.GetColor(piece) == Piece.White && startRank == 1) ||
                                    (Piece.GetColor(piece) == Piece.Black && startRank == 6);
            targetSquare = startSquare + direction * 2;
            if (isStartingPos && BoardManager.Instance.squares[targetSquare] == Piece.None)
            {
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Move, piece: piece));
            }
        }

        // ¬з€тие
        int[] captureOffsets = { direction - 1, direction + 1 };

        foreach (var offset in captureOffsets)
        {
            targetSquare = startSquare + offset;

            // «ащита от переноса: целева€ клетка должна быть на соседнем файле
            if (targetSquare < 0 || targetSquare >= 64 || Math.Abs(targetSquare % 8 - startFile) != 1)
                continue;

            int targetPiece = BoardManager.Instance.squares[targetSquare];

            if (targetPiece != Piece.None && !Piece.IsSameColor(piece, targetPiece))
            {
                AddPawnMove(ref pseudoLegalMoves, startSquare, targetSquare, MoveType.Take, targetPiece, promotedPawn: piece, piece: piece);
            }
            else if (targetSquare == BoardManager.Instance.enPassantTargetSquare)
            {
                // —разу же вычисл€ем клетку пешки, которую мы будем рубить
                int enPassantedPawnSquare = Piece.GetColor(piece) == Piece.White ? targetSquare - 8 : targetSquare + 8;
                pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.EnPassant, BoardManager.Instance.squares[enPassantedPawnSquare], - 1, -1, enPassantedPawnSquare, piece: piece));
            }
        }
    }

    private void TryAddCastleMove(ref List<Move> pseudoLegalMoves, int startSquare, int targetSquare, int[] pathSquares, int rookStart, int rookTarget, int piece)
    {
        for (int i = 0; i < pathSquares.Length; i++)
        {
            if (BoardManager.Instance.squares[pathSquares[i]] != 0)
                return;
        }

        pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Castle, 0, rookStart, rookTarget, piece: piece));
    }

    private void AddPawnMove(ref List<Move> pseudoLegalMoves, int startSquare, int targetSquare, MoveType baseType, int capturedPiece = Piece.None, int promotedPawn = Piece.None, int piece = Piece.None)
    {
        // ≈сли пешка оказываетс€ на последнем или первом ранге, то это точно превращение
        if (targetSquare / 8 == 7 || targetSquare / 8 == 0)
        {
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote, capturedPiece, promotedPawn: promotedPawn, promoteTo: Piece.Queen, piece: piece));
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote, capturedPiece, promotedPawn: promotedPawn, promoteTo: Piece.Knight, piece: piece));
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote, capturedPiece, promotedPawn: promotedPawn, promoteTo: Piece.Rook, piece: piece));
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, MoveType.Promote, capturedPiece, promotedPawn: promotedPawn, promoteTo: Piece.Bishop, piece: piece));
        }
        else
            pseudoLegalMoves.Add(new Move(startSquare, targetSquare, baseType, capturedPiece, piece: piece));
    }
}
