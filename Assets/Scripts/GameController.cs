using NUnit.Framework;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    private bool _isSelection;

    private int _selectedIndex;

    private Move _lastMove;

    private Move _pendingPromotionMove;  

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
        _isSelection = true;
        _lastMove = new Move();

        
    }

    void Update()
    {
        
    }

    public void ProcessRightClick(int squareIndex)
    {
        BoardRenderer.Instance.ToggleHighlightSquare(squareIndex);
        // StartCoroutine(Engine.Instance.MakeARandomMove());
    }

    public void ProcessClick(int squareIndex)
    {
        int selectedPiece = BoardManager.Instance.squares[squareIndex];
        List<Move> legalMoves = MoveGenerator.Instance.FilterLegalMoves();

        if (_isSelection)
        {
            // Если это фигура, и НАШ ХОД
            if (selectedPiece != 0 && Piece.GetColor(selectedPiece) == BoardManager.Instance.colorToMove)
            {
                // Подсветить легальные ходы
                foreach (var legalMove in legalMoves)
                {
                    if (squareIndex == legalMove.startSquare)
                    {
                        BoardRenderer.Instance.ShowMoveIsLegal(legalMove.targetSquare);
                    }
                }
                
                BoardRenderer.Instance.RemoveAllHighlighted();
                BoardRenderer.Instance.SelectSquare(squareIndex);
                _selectedIndex = squareIndex;
                _isSelection = false;
            }
        }
        // Если мы выделили новую фигуру
        else if (selectedPiece != 0)
        {
            // И если цвет этой фигуры такой же, как и у предыдущей
            if (Piece.IsSameColor(selectedPiece, BoardManager.Instance.squares[_selectedIndex]))
            {
                // То нам нужно сбросить выделение с выделенной до этого клетки
                BoardRenderer.Instance.ResetSquareColor(_selectedIndex);

                // Сбросить выделение легальных ходов для этой клетки
                BoardRenderer.Instance.ResetLegalMovesIndication();

                // И сбросить _isSelection в true и вызвать ProcessClick() еще раз
                _isSelection = true;

                // Если это НЕ та же самая фигура, то мы выделили новую другую
                if (squareIndex != _selectedIndex)
                    ProcessClick(squareIndex);
                else
                    BoardRenderer.Instance.RemoveAllHighlighted();
            }

            // Если нет, значит мы хотим срубить, но контроллеру это не важно, просто ход
            else
            {
                RequestMove(squareIndex);
            }
        }
        // Мы выделили пустую клетку и сейчас не выделение, значит мы хотим сделать ход
        else
        {
            RequestMove(squareIndex);
        }
    }

    public void RequestMove(int targetSquareIndex)
    {
        Move move = new Move(_selectedIndex, targetSquareIndex, MoveType.Undefined);
        bool legal = false;

        List<Move> legalMoves = MoveGenerator.Instance.FilterLegalMoves();

        // Проверить, легален ли ход
        foreach (var legalMove in legalMoves)
        {
            if (move.Equals(legalMove))
            {
                Debug.Log("This move is legal (kinda)");
                move = legalMove;
                legal = true;
            }
        }

        if (!legal)
        {
            Debug.Log("This move is SUPER ILLEGAL");
            return;
        }

        if (move.type == MoveType.Promote)
        {
            InputManager.Instance.isPromotion = true;
            _pendingPromotionMove = move;
            BoardRenderer.Instance.ShowPromotionMenu(Piece.GetColor(BoardManager.Instance.squares[_selectedIndex]));
            return;
        }

        ExecuteMove(move, Piece.None);
    }

    public void ExecuteMove(Move move, int promotionPieceType)
    {
        BoardManager.Instance.ProcessMove(move, false, promotionPieceType);

        BoardRenderer.Instance.SelectSquare(move.targetSquare);
        BoardRenderer.Instance.UnselectSquare(move.startSquare);
        BoardRenderer.Instance.ResetLegalMovesIndication();

        BoardRenderer.Instance.ResetSquareColor(_lastMove.startSquare);
        BoardRenderer.Instance.ResetSquareColor(_lastMove.targetSquare);

        _selectedIndex = -1;
        _isSelection = true;
        _lastMove = move;

        // if (BoardManager.Instance.colorToMove == Piece.Black)
        //     StartCoroutine(Engine.Instance.MakeARandomMove());
    }

    public void OnPromotionPieceSelected(int squareIndex)
    {
        int[] deck;
        if (Piece.GetColor(BoardManager.Instance.squares[_pendingPromotionMove.startSquare]) == Piece.White)
            deck = BoardManager.Instance.whitePromotionDeck;
        else
            deck = BoardManager.Instance.blackPromotionDeck;

        int pieceType = deck[squareIndex];

        BoardRenderer.Instance.HidePromotionMenu();

        if (_pendingPromotionMove.type != MoveType.Undefined)
        {
            ExecuteMove(_pendingPromotionMove, pieceType);
            _pendingPromotionMove = new Move();
        }
    }

    public void UndoMove()
    {
        // Снять красное выделение у всех клеток
        BoardRenderer.Instance.RemoveAllHighlighted();

        // Снять выделение с предыдущих клеток
        BoardRenderer.Instance.ResetSquareColor(_lastMove.startSquare);
        BoardRenderer.Instance.ResetSquareColor(_lastMove.targetSquare);

        // Сбрасываем выделение легальных ходов
        BoardRenderer.Instance.ResetLegalMovesIndication();

        if (_selectedIndex != -1)
        {
            BoardRenderer.Instance.ResetSquareColor(_selectedIndex);
        }

        _selectedIndex = -1;
        _isSelection = true;
        BoardManager.Instance.ProcessMove(new Move(), true);
        // BoardManager.Instance.ProcessMove(new Move(), true);

        if (BoardManager.Instance.moveHistory.Count > 0)
        {
            _lastMove = BoardManager.Instance.moveHistory.Peek();
        }

        if (BoardManager.Instance.moveHistory.Count != 0)
        {
            BoardRenderer.Instance.SelectSquare(_lastMove.startSquare);
            BoardRenderer.Instance.UnselectSquare(_lastMove.targetSquare);
        }
    }
}
