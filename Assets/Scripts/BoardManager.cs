using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.Audio.ProcessorInstance;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [SerializeField] private TMP_Text endGameText;
    [SerializeField] private Transform moveHistoryText;
    [SerializeField] private GameObject moveTextPrefab;
    private Stack<GameObject> moveHistoryItems = new Stack<GameObject>();

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

    public bool isCheck = false;
    public bool isCheckmate = false;
    public bool isStalemate = false;
    public bool isDraw = false;

    public bool gameEnded = false;

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

    private void Start()
    {
        MoveGenerator.Instance.GenerateLegalMoves(colorToMove);
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

    public void AddMove(Move move)
    {
        GameObject newItem = Instantiate(moveTextPrefab, moveHistoryText);
        newItem.GetComponent<TextMeshProUGUI>().text = moveHistory.Count.ToString() + ". ";

        if (!gameEnded)
            newItem.GetComponent<TextMeshProUGUI>().text += moveToAlgebraic(move);
        else
        {
            if (isDraw)
                newItem.GetComponent<TextMeshProUGUI>().text = "1/2 - 1/2";
            else
                newItem.GetComponent<TextMeshProUGUI>().text = colorToMove == Piece.White ? "1 - 0" : "0 - 1";
        }

        moveHistoryItems.Push(newItem);
    }

    private void RemoveMove()
    {
        GameObject moveToDelete;
        if (gameEnded)
        {
            moveToDelete = moveHistoryItems.Pop();
            Destroy(moveToDelete);
        }
        moveToDelete = moveHistoryItems.Pop();
        Destroy(moveToDelete);
    }

    public int FindPiece(int piece)
    {
        for (int i = 0; i < 64; i++)
        {
            if (squares[i] == piece)
            {
                return i;
            }
        }
        return -1;
    }

    public string moveToText(Move move)
    {
        string files = "abcdefgh";

        int startIndex = move.startSquare;
        int targetIndex = move.targetSquare;

        int startRank = startIndex / 8 + 1;
        int startFile = startIndex % 8;

        int targetRank = targetIndex / 8 + 1;
        int targetFile = targetIndex % 8;

        string text = files[startFile] + startRank.ToString() + files[targetFile] + targetRank.ToString();

        return text;
    }

    public string moveToAlgebraic(Move move)
    {
        string files = "abcdefgh";
        Dictionary<int, string> pieces = new Dictionary<int, string>()
        {
            { Piece.King, "K"},
            { Piece.Queen, "Q"},
            { Piece.Rook, "R"},
            { Piece.Bishop, "B"},
            { Piece.Knight, "N"},
            { Piece.Pawn, ""},
        };

        if (move.type == MoveType.Castle)
        {
            if (move.targetSquare - move.startSquare == 2)
                return "0-0";
            else
                return "0-0-0";
        }

        string piece = pieces[Piece.GetType(move.piece)];

        int startIndex = move.startSquare;
        int targetIndex = move.targetSquare;

        int targetRank = targetIndex / 8 + 1;
        int targetFile = targetIndex % 8;

        string algebraic = piece;

        if (move.type == MoveType.Take)
        {
            if (Piece.GetType(move.piece) == Piece.Pawn)
            {
                algebraic = files[move.startSquare % 8].ToString();
            }
            algebraic += "x";
        }

        algebraic += files[targetFile] + targetRank.ToString();

        if (isCheckmate)
            algebraic += "#";
        else if (isCheck)
            algebraic += "+";

        return algebraic;
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

        if (fen.Split(' ')[1] == "w")
            colorToMove = 8;
        else 
            colorToMove = 16;
    }

    public void ProcessMove(Move move, bool undo, bool silent = false)
    {
        int fromIndex = move.startSquare;
        int toIndex = move.targetSquare;
        int pieceToMove = squares[fromIndex];
        if (silent)
        {
            move.promoteTo |= Piece.GetColor(pieceToMove);
        }
        Move lastMove = new Move();

        if (undo)
        {
            if (moveHistory.Count == 0)
                return;
            if (!silent)
                RemoveMove();
            isCheckmate = false;
            isStalemate = false;
            gameEnded = false;
            lastMove = moveHistory.Pop();
            move = lastMove.ReverseMove(); // move = reversedMove
        }

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
                ExecuteMove(move, undo, silent: silent);
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

        if (!silent)
        {
            // Проверяем, не находится ли под шахом король оппонента
            if (MoveGenerator.Instance.IsSquareAttacked(FindPiece(Piece.King | Piece.GetReversedColor(colorToMove)), colorToMove))
            {
                isCheck = true;
            }
            else
            {
                isCheck = false;
            }

            // Генерируем новый список легальных ходов для оппонента
            MoveGenerator.Instance.GenerateLegalMoves(Piece.GetReversedColor(colorToMove));

            if (MoveGenerator.Instance.legalMoves.Count == 0)
            {
                // У оппонента нет легальных ходов, значит это либо мат, либо пат
                if (isCheck)
                    isCheckmate = true;
                else
                {
                    isStalemate = true;
                    isDraw = true;
                }
                // Вызвать методы:
                // Выписать в ходы 1-0, 0-1 и т.д.
                // Вывести текст "Draw", "White won", "Black won"
            }

            if (!undo)
            {
                AddMove(move);

                if (MoveGenerator.Instance.legalMoves.Count == 0)
                {
                    gameEnded = true;
                    AddMove(move);
                    ShowEndText(false);
                }
            }

        }

        colorToMove = Piece.GetReversedColor(colorToMove);
    }

    private void ShowEndText(bool clear)
    {
        if (clear)
        {
            endGameText.text = "";
            return;
        }
        if (isDraw)
        {
            endGameText.text = "Draw!";
        }
        if (isCheckmate)
        {
            endGameText.text = "Checkmate!\n";
            endGameText.text += colorToMove == Piece.White ? "White won" : "Black won";
        }
    }

    public void ExecuteMove(Move move, bool undo, bool silent = false)
    {
        // int fromIndex, int toIndex, int piecePromoteTo = 0, int capturedPiece = 0, bool enPassant = false
        int fromIndex = move.startSquare;
        int toIndex = move.targetSquare;
        int capturedPiece = move.capturedPiece;

        if (!undo)
        {
            squares[toIndex] = move.type == MoveType.Promote ? move.promoteTo : squares[fromIndex];
            squares[fromIndex] = 0;
            if (move.type == MoveType.EnPassant)
            {
                squares[move.enPassantTargetPawnSquare] = 0;
            }
            if (!silent)
                BoardRenderer.Instance.VizualizeMove(move);
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
