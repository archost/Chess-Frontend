using UnityEngine;
using UnityEngine.SocialPlatforms;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    private bool _isSelection;

    private int _selectedIndex;

    private Move lastMove;

    private Move promMove;  

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
        lastMove = new Move();
    }

    void Update()
    {
        
    }

    public void ProcessRightClick(int squareIndex)
    {
        BoardRenderer.Instance.ToggleHighlightSquare(squareIndex);
    }

    public void ProcessClick(int squareIndex)
    {
        int selectedPiece = BoardManager.Instance.squares[squareIndex];
        MoveGenerator.Instance.GenerateMoves();

        if (_isSelection)
        {
            // Если это фигура, и НАШ ХОД
            if (selectedPiece != 0 && Piece.GetColor(selectedPiece) == BoardManager.Instance.colorToMove)
            {
                // Подсветить легальные ходы
                foreach (var pseudoLegalMove in MoveGenerator.Instance.pseudoLegalMoves)
                {
                    if (squareIndex == pseudoLegalMove.startSquare)
                    {
                        BoardRenderer.Instance.ShowMoveIsLegal(pseudoLegalMove.targetSquare);
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
                MakeAMove(squareIndex);
            }
        }
        // Мы выделили пустую клетку и сейчас не выделение, значит мы хотим сделать ход
        else
        {
            MakeAMove(squareIndex);
        }
    }

    private void MakeAMove(int squareIndex)
    {
        Move move = new Move(_selectedIndex, squareIndex, MoveType.Undefined);
        bool legal = false;

        // Проверить, легален ли ход
        foreach (var pseudoLegalMove in MoveGenerator.Instance.pseudoLegalMoves)
        {
            if (move.Equals(pseudoLegalMove))
            {
                Debug.Log("This move is legal (kinda)");
                move = pseudoLegalMove;
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
            BoardRenderer.Instance.ShowPromotionMenu(Piece.GetColor(BoardManager.Instance.squares[_selectedIndex]));

            promMove = move;

            return;
        }

        // Вызвать метод хода у BoardManager
        BoardManager.Instance.ProcessMove(move, false);
        // BoardManager сделает ход
        // // И вызовет у BoardRenderer метод обновления визуала

        UpdateSelectionVisual();

        BoardRenderer.Instance.SelectSquare(squareIndex);
        BoardRenderer.Instance.UnselectSquare(_selectedIndex);
        _isSelection = true;

        AudioManager.Instance.PlayMoveSound(move.type);

        // Сохраняем последний сделанный ход
        lastMove = move;
    }

    public void MakePromotionMove(int squareIndex)
    {
        // Сначала определить, какой цвет фигуры, чтобы брать фигуры из соответствующего меню
        // Это у нас сейчас есть в lastMove
        // Правда значений фигур у нас нет, надо завести в BoardManager?
        int[] deck;
        if (Piece.GetColor(BoardManager.Instance.squares[promMove.startSquare]) == Piece.White)
            deck = BoardManager.Instance.whitePromotionDeck;
        else
            deck = BoardManager.Instance.blackPromotionDeck;

        int pieceType = deck[squareIndex];

        BoardRenderer.Instance.HidePromotionMenu();

        BoardManager.Instance.ProcessMove(promMove, false, pieceType);

        UpdateSelectionVisual();

        BoardRenderer.Instance.SelectSquare(promMove.targetSquare);
        BoardRenderer.Instance.UnselectSquare(_selectedIndex);

        AudioManager.Instance.PlayMoveSound(MoveType.Promote);

        lastMove = promMove;
        _isSelection = true;
    }

    private void UpdateSelectionVisual()
    {
        // Снять красное выделение у всех клеток
        BoardRenderer.Instance.RemoveAllHighlighted();

        // Снять выделение с предыдущих клеток
        BoardRenderer.Instance.ResetSquareColor(lastMove.startSquare);
        BoardRenderer.Instance.ResetSquareColor(lastMove.targetSquare);

        // Сбрасываем выделение легальных ходов
        BoardRenderer.Instance.ResetLegalMovesIndication();
    }

    public void UndoMove()
    {
        UpdateSelectionVisual();
        if (_selectedIndex != -1)
        {
            BoardRenderer.Instance.ResetSquareColor(_selectedIndex);
        }

        _selectedIndex = -1;
        _isSelection = true;
        BoardManager.Instance.ProcessMove(new Move(), true);

        if (BoardManager.Instance.moveHistory.Count > 0)
        {
            lastMove = BoardManager.Instance.moveHistory.Peek();
        }

        if (BoardManager.Instance.moveHistory.Count != 0)
        {
            BoardRenderer.Instance.SelectSquare(lastMove.startSquare);
            BoardRenderer.Instance.UnselectSquare(lastMove.targetSquare);
        }
    }
}
