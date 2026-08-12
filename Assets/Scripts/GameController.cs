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
            Debug.Log("Selected piece = " + selectedPiece);
            // Если это фигура, и НАШ ХОД
            if (selectedPiece != 0 && Piece.GetColor(selectedPiece) == BoardManager.Instance.colorToMove)
            {
                Debug.Log("Это фигура, и это наш ход");
                // TODO: Подсветить легальные ходы

                // Проверить, легален ли ход
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
            Debug.Log("Выделяем новую фигуру");
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
                // Проверяем, не превращение ли это
                if (GetTypeFromPseudolegalMoves(_selectedIndex, squareIndex) == MoveType.Promote)
                {
                    // Если это превращение, то нам нужно:
                    // Вызвать метод у BoardRenderer, который высветит возможные варианты
                    // Поставить InputManager флаг, который означает, что мы сейчас можем кликать только на те клетки,
                    // которые позволяют нам выбрать тип фигуры, все остальные вводы должны игнорироваться
                    Debug.Log("prom");
                    InputManager.Instance.isPromotion = true;
                    BoardRenderer.Instance.ShowPromotionMenu(Piece.GetColor(BoardManager.Instance.squares[_selectedIndex]));
                    // После этого мы не сможем обрабатывать обычные ходы
                    // Ну мы и выйдем из этой функции тоже
                    // Значит нам нужно запомнить, какой ход привел к превращению, чтобы вызвать его потом
                    promMove = new Move(_selectedIndex, squareIndex, MoveType.Promote);

                    foreach (var pseudoLegalMove in MoveGenerator.Instance.pseudoLegalMoves)
                    {
                        if (promMove.Equals(pseudoLegalMove))
                        {
                            Debug.Log("This move is legal (kinda)");
                            promMove = pseudoLegalMove;
                        }
                    }
                }
                else
                {
                    MakeAMove(squareIndex);
                }
            }
        }
        // Мы выделили пустую клетку и сейчас не выделение, значит мы хотим сделать ход
        else
        {
            // Проверяем, не превращение ли это
            if (GetTypeFromPseudolegalMoves(_selectedIndex, squareIndex) == MoveType.Promote)
            {
                // Если это превращение, то нам нужно:
                // Вызвать метод у BoardRenderer, который высветит возможные варианты
                // Поставить InputManager флаг, который означает, что мы сейчас можем кликать только на те клетки,
                // которые позволяют нам выбрать тип фигуры, все остальные вводы должны игнорироваться
                Debug.Log("prom");
                InputManager.Instance.isPromotion = true;
                BoardRenderer.Instance.ShowPromotionMenu(Piece.GetColor(BoardManager.Instance.squares[_selectedIndex]));
                // После этого мы не сможем обрабатывать обычные ходы
                // Ну мы и выйдем из этой функции тоже
                // Значит нам нужно запомнить, какой ход привел к превращению, чтобы вызвать его потом
                promMove = new Move(_selectedIndex, squareIndex, MoveType.Promote);

                foreach (var pseudoLegalMove in MoveGenerator.Instance.pseudoLegalMoves)
                {
                    if (promMove.Equals(pseudoLegalMove))
                    {
                        Debug.Log("This move is legal (kinda)");
                        promMove = pseudoLegalMove;
                    }
                }

            }
            else
            {
                MakeAMove(squareIndex);
            }

        }
    }

    private void MakeAMove(int squareIndex)
    {
        int pieceToMove = BoardManager.Instance.squares[_selectedIndex];

        // moved to process click MoveGenerator.Instance.GenerateMoves();

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

        // Вызвать метод хода у BoardManager
        BoardManager.Instance.ProcessMove(move);
        // BoardManager сделает ход
        // // И вызовет у BoardRenderer метод обновления визуала

        // Снять красное выделение у всех клеток
        BoardRenderer.Instance.RemoveAllHighlighted();

        // Снять выделение с предыдущих клеток
        BoardRenderer.Instance.ResetSquareColor(lastMove.startSquare);
        BoardRenderer.Instance.ResetSquareColor(lastMove.targetSquare);

        // Сбрасываем выделение легальных ходов
        BoardRenderer.Instance.ResetLegalMovesIndication();

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

        BoardManager.Instance.ProcessMove(promMove, pieceType);

        // Снять красное выделение у всех клеток
        BoardRenderer.Instance.RemoveAllHighlighted();

        // Снять выделение с предыдущих клеток
        BoardRenderer.Instance.ResetSquareColor(lastMove.startSquare);
        BoardRenderer.Instance.ResetSquareColor(lastMove.targetSquare);

        // Сбрасываем выделение легальных ходов
        BoardRenderer.Instance.ResetLegalMovesIndication();

        BoardRenderer.Instance.SelectSquare(promMove.targetSquare);
        BoardRenderer.Instance.UnselectSquare(_selectedIndex);

        AudioManager.Instance.PlayMoveSound(MoveType.Promote);

        lastMove = promMove;
        _isSelection = true;
    }

    private MoveType GetTypeFromPseudolegalMoves(int startSquare, int targetSquare)
    {
        Move checkingMove = new Move(startSquare, targetSquare, MoveType.Undefined);
        foreach (var pseudoLegalMove in MoveGenerator.Instance.pseudoLegalMoves)
        {
            if (checkingMove.Equals(pseudoLegalMove))
            {
                return pseudoLegalMove.type;
            }
        }

        return MoveType.Undefined;
    }

    public void UndoMove()
    {
        _selectedIndex = -1;
        _isSelection = true;
        BoardManager.Instance.UndoMove();
    }
}
