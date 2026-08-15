using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public int[] squares;
    public Stack<Move> moveHistory;
    public string fen = "";
    // rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
    // r3k2r/2b4q/8/8/8/8/2B4Q/R3K2R w KQkq - 0 1
    // 7k/3N2qp/b5r1/2p1Q1N1/Pp4PK/7P/1P3p2/6r1 w - - 7 4

    public int[] whitePromotionDeck;
    public int[] blackPromotionDeck;

    public int colorToMove = 8;

    public int enPassantTargetSquare = -1;

    public int castlingRights = 15; // 1111 - B0-0-0, B0-0, W0-0-0, W0-0 - 8, 4, 2, 1 
    private int[] castlingRightsMask;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            squares = new int[64];
            moveHistory = new Stack<Move>();
            FenToBoard();
            InitializePromotionDecks();
            InitializeCastlingMasks();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCastlingMasks()
    {
        castlingRightsMask = new int[64];
        castlingRightsMask[0] = 2;
        castlingRightsMask[4] = 3;
        castlingRightsMask[7] = 1;
        castlingRightsMask[56] = 8;
        castlingRightsMask[60] = 12;
        castlingRightsMask[63] = 4;
    }

    private void InitializePromotionDecks()
    {
        whitePromotionDeck = new int[4]{
            Piece.Rook | Piece.White,
            Piece.Bishop | Piece.White,
            Piece.Queen | Piece.White,
            Piece.Knight | Piece.White
        };
        blackPromotionDeck = new int[4]{
            Piece.Rook | Piece.Black,
            Piece.Bishop | Piece.Black,
            Piece.Queen | Piece.Black,
            Piece.Knight | Piece.Black
        };

    }

    private void FenToBoard()
    {
        var piecesValues = new Dictionary<char, int>()
        {
            { 'p', Piece.Pawn },
            { 'n', Piece.Knight },
            { 'b', Piece.Bishop },
            { 'r', Piece.Rook },
            { 'q', Piece.Queen },
            { 'k', Piece.King }
        };

        string fenBoard = fen.Split(' ')[0];
        int currentPiece;
        int file = 0, rank = 7;

        foreach (char symbol in fenBoard)
        {
            currentPiece = 0;
            if (symbol == '/')
            {
                file = 0;
                rank--;
            }
            else
            {
                if (char.IsDigit(symbol))
                {
                    file += (int)char.GetNumericValue(symbol);
                }
                else
                {
                    currentPiece |= (char.IsUpper(symbol)) ? Piece.White : Piece.Black;
                    currentPiece |= piecesValues[char.ToLower(symbol)];
                    squares[file + 8 * rank] = currentPiece;
                    file++;
                }
            }

        }
    }

    public void ProcessMove(Move move, bool undo, int piecePromoteTo = Piece.None, bool silent = false)
    {
        int fromIndex = move.startSquare;
        int toIndex = move.targetSquare;
        int pieceToMove = squares[fromIndex];
        Move lastMove = new Move();

        if (undo)
        {
            if (moveHistory.Count == 0)
                return;
            lastMove = moveHistory.Pop();
            move = lastMove.ReverseMove(); // move = reversedMove
        }

        if (silent | piecePromoteTo == Piece.None)
            piecePromoteTo = Piece.Queen | colorToMove;

        switch (move.type)
        {
            case MoveType.Move:
                ExecuteMove(move, undo, silent: silent);
                break;
            case MoveType.Take:
                ExecuteMove(move, undo, silent: silent);
                break;
            case MoveType.Castle:
                ExecuteMove(move, undo, silent: silent);
                Move rookCastlingMove = new Move(move.castlingRookStartSquare, move.castlingRookTargetSquare, MoveType.Move);
                ExecuteMove(rookCastlingMove, undo, silent: silent);
                break;
            case MoveType.EnPassant:
                ExecuteMove(move, undo, silent: silent);
                break;
            case MoveType.Promote:
                ExecuteMove(move, undo, piecePromoteTo, silent: silent);
                break;
            default:
                Debug.Log("How did we get here?");
                break;
        }

        //// Обновление правил доски

        if (undo)
        {
            castlingRights = lastMove.previousCastlingRights;
            enPassantTargetSquare = lastMove.previousEnPassantSquare;
        }
        else
        {
            move.previousCastlingRights = castlingRights;
            move.previousEnPassantSquare = enPassantTargetSquare;

            // Проверяем, был ли сделан ход пешкой на две клетки, чтобы выставить enPassantTargetSquare
            if (Piece.GetType(pieceToMove) == Piece.Pawn && Math.Abs(fromIndex / 8 - toIndex / 8) == 2)
            {
                enPassantTargetSquare = Piece.GetColor(pieceToMove) == Piece.White ? toIndex - 8 : toIndex + 8;
            }
            else
                enPassantTargetSquare = -1;

            // Обновление маски флагов рокировки
            castlingRights &= ~(castlingRightsMask[fromIndex] | castlingRightsMask[toIndex]);

            moveHistory.Push(move);
        }

        // Обновляем чей ход
        colorToMove = Piece.GetReversedColor(colorToMove);
    }

    public void ExecuteMove(Move move, bool undo, int piecePromoteTo = Piece.None, bool silent = false)
    {
        // int fromIndex, int toIndex, int piecePromoteTo = 0, int capturedPiece = 0, bool enPassant = false
        int fromIndex = move.startSquare;
        int toIndex = move.targetSquare;
        int capturedPiece = move.capturedPiece;

        if (!undo)
        {
            squares[toIndex] = move.type == MoveType.Promote ? piecePromoteTo : squares[fromIndex];
            squares[fromIndex] = 0;
            if (move.type == MoveType.EnPassant)
            {
                squares[move.enPassantTargetPawnSquare] = 0;
            }
            if (!silent)
                BoardRenderer.Instance.VizualizeMove(move, piecePromoteTo);
        }
        else
        {
            // Обратные ходы
            int promotedPieceColor = Piece.GetColor(squares[fromIndex]);
            int pawn = Piece.Pawn | promotedPieceColor;

            squares[toIndex] = move.type == MoveType.Promote ? pawn : squares[fromIndex];
            squares[fromIndex] = move.type != MoveType.EnPassant ? capturedPiece : 0;

            if (move.type == MoveType.EnPassant)
            {
                squares[move.enPassantTargetPawnSquare] = capturedPiece;
            }
            if (!silent)
                BoardRenderer.Instance.UndoVizualizeMove(move);
        }
    }
}
