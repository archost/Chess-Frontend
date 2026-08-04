using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    private bool _isSelection;

    private int _selectedIndex;

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
    }

    void Update()
    {
        
    }

    public void ProcessClick(int squareIndex)
    {
        int selectedPiece = BoardManager.Instance.squares[squareIndex];
        
        if (_isSelection)
        {
            // Если это фигура, и НАШ ХОД
            if (selectedPiece != 0 && Piece.GetColor(selectedPiece) == BoardManager.Instance.colorToMove)
            {
                // TODO: Подсветить легальные ходы

                BoardRenderer.Instance.getSquare(squareIndex).SelectSquare(); // TODO: переместить логику подсветки в BoardRenderer
                _selectedIndex = squareIndex;
                _isSelection = false;
            }
        }
        // Если мы выделили новую фигуру
        else if (selectedPiece != 0)
        {
            Debug.Log("Selected a NEW PIECE");
            // И если цвет этой фигуры такой же, как и у предыдущей
            if (Piece.IsSameColor(selectedPiece, BoardManager.Instance.squares[_selectedIndex]))
            {
                Debug.Log("And it's the SAME COLOR");
                // То нам нужно сбросить выделение с выделенной до этого клетки
                BoardRenderer.Instance.getSquare(_selectedIndex).ResetSquareColor();

                // И сбросить _isSelection в true и вызвать ProcessClick() еще раз
                _isSelection = true;
                ProcessClick(squareIndex);
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
        int pieceToMove = BoardManager.Instance.squares[_selectedIndex];
        MoveGenerator.Instance.GenerateMoves();
        Move move = new Move(_selectedIndex, squareIndex);
        bool legal = false;
        // Проверить, легален ли ход
        foreach (var pseudoLegalMove in MoveGenerator.Instance.pseudoLegalMoves)
        {
            if (move.Equals(pseudoLegalMove))
            {
                Debug.Log("This move is legal!");
                legal = true;
            }
        }

        if ((Piece.GetType(pieceToMove) == Piece.Queen ||
            Piece.GetType(pieceToMove) == Piece.Rook ||
            Piece.GetType(pieceToMove) == Piece.Bishop) && legal == false)
        {
            Debug.Log("This move is ILLEGAL!");
            return;
        }

        // Вызвать метод хода у BoardManager
        BoardManager.Instance.ExecuteMove(_selectedIndex, squareIndex);
        // BoardManager сделает ход
        // // И вызовет у BoardRenderer метод обновления визуала

        // BoardRenderer.Instance.getSquare(squareIndex).SelectSquare();
        BoardRenderer.Instance.getSquare(_selectedIndex).ResetSquareColor();
        _isSelection = true;

        // Проиграть звук
    }
}
