using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public int[] squares;
    private Stack<Move> moveHistory;
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

    public void ProcessMove(Move move, int piecePromoteTo = Piece.None)
    {
        enPassantTargetSquare = -1;

        int fromIndex = move.startSquare;
        int toIndex = move.targetSquare;

        int pieceToMove = squares[fromIndex];
        int pieceToCapture = squares[toIndex];

        // Обычный ход
        if (move.type == MoveType.Move)
        {
            ExecuteMove(fromIndex, toIndex);
        }

        // Превращение - отдельный ход, а не дополнение к ходу "ход"
        if (move.type == MoveType.Promote)
        {
            ExecuteMove(fromIndex, toIndex, piecePromoteTo);
        }

        // Взятие
        if (move.type == MoveType.Take)
        {
            ExecuteMove(fromIndex, toIndex);
        }

        // Рокировка
        if (move.type == MoveType.Castle)
        {
            // Ход королем
            ExecuteMove(fromIndex, toIndex);

            // Ход ладьей
            ExecuteMove(move.castlingRookStartSquare, move.castlingRookTargetSquare);
        }

        // En passant
        if ((move.type == MoveType.EnPassant))
        {
            ExecuteMove(fromIndex, toIndex);
            ExecuteMove(fromIndex, move.enPassantTargetPawnSquare);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Проверяем, был ли сделан ход пешкой на две клетки, чтобы выставить enPassantTargetSquare
        if (Piece.GetType(pieceToMove) == Piece.Pawn && Math.Abs(fromIndex / 8 - toIndex / 8) == 2)
        {
            enPassantTargetSquare = Piece.GetColor(pieceToMove) == Piece.White ? toIndex - 8 : toIndex + 8;
        }

        move.previousCastlingRights = castlingRights;

        // Обновление маски флагов рокировки
        castlingRights &= ~(castlingRightsMask[fromIndex] | castlingRightsMask[toIndex]);

        // Обновляем чей ход
        colorToMove = Piece.GetReversedColor(colorToMove);

        Debug.Log("Добавляю в историю ход, у которого взятая фигура " + move.capturedPiece);
        moveHistory.Push(move);
    }

    private void ExecuteMove(int fromIndex, int toIndex, int piecePromoteTo = 0, int capturedPiece = 0, bool enPassant = false)
    {
        if (piecePromoteTo == -1)
        {
            // То это отмена превращения
            // А КАК БЛЯТЬ ОТМЕНЯТЬ ПРЕВРАЩЕНИЕ ЕСЛИ ПЕШКА ПРОСТО ПРОПАДАЕТ И ВСЕ БЛЯТЬ Я НЕ ЗНАЮ НИХУЯ НЕ ПОНИМАЮ СУКА Я НЕ МОГУ
            // Может вообще хранить превращенную пешку в capturedPiece? Нет, потому что в capturedPiece будет фигура, которую мы взяли, когда превращались
            // Найти цвет фигуры, в которую пешка превратилась:
            int promotedPieceColor = Piece.GetColor(squares[fromIndex]);
            int pawn = Piece.Pawn | promotedPieceColor;

            squares[toIndex] = pawn;
            squares[fromIndex] = capturedPiece == 0 ? 0 : capturedPiece;
        }
        else
        {
            squares[toIndex] = piecePromoteTo == 0 ? squares[fromIndex] : piecePromoteTo;
            squares[fromIndex] = capturedPiece == 0 ? 0 : capturedPiece;
        }

        BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex, piecePromoteTo, capturedPiece, enPassant);
    }

    public void UndoMove()
    {
        if (moveHistory.Count == 0)
            return;
        Move lastMove = moveHistory.Pop();

        Debug.Log("Undoing move: " + lastMove.startSquare + " to " + lastMove.targetSquare);

        switch (lastMove.type)
        {
            case MoveType.Move:
                ExecuteMove(lastMove.targetSquare, lastMove.startSquare);
                break;
            case MoveType.Take:
                ExecuteMove(lastMove.targetSquare, lastMove.startSquare, 0, lastMove.capturedPiece);
                break;
            case MoveType.Castle:
                ExecuteMove(lastMove.targetSquare, lastMove.startSquare);
                ExecuteMove(lastMove.castlingRookTargetSquare, lastMove.castlingRookStartSquare);
                break;
            case MoveType.EnPassant:
                Debug.Log("Captured piece = " + lastMove.capturedPiece);
                ExecuteMove(lastMove.enPassantTargetPawnSquare, lastMove.startSquare, 0, lastMove.capturedPiece, true);
                ExecuteMove(lastMove.targetSquare, lastMove.startSquare);
                break;
            case MoveType.Promote:
                Debug.Log("У последнего хода превращения взятая фигура была " + lastMove.capturedPiece);
                ExecuteMove(lastMove.targetSquare, lastMove.startSquare, -1, lastMove.capturedPiece);
                break;
            default:
                Debug.Log("How did we get here?");
                break;
        }

        // Обновление маски флагов рокировки (не работает)
        castlingRights = lastMove.previousCastlingRights;

        colorToMove = Piece.GetReversedColor(colorToMove);
    }
}
