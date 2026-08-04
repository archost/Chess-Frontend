using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    private SquareView _selectedSquare;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ProcessClick();
        }
    }

    private void ProcessClick()
    {
        RaycastHit2D rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (rayHit.transform.TryGetComponent<SquareView>(out _selectedSquare))
        {
            GameController.Instance.ProcessClick(_selectedSquare.squareIndex);
            /*
            if (_fromSquare != null && _toSquare != null && _fromSquare != _selectedSquare)
            {
                _fromSquare.ResetSquareColor();
                _toSquare.ResetSquareColor();
            }
            if (_isSelection)
            {
                _selectedPiece = _selectedSquare.GetComponentInChildren<PieceView>();
                if (_selectedPiece != null)
                {
                    if (Piece.IsColor(_selectedPiece.pieceType, BoardManager.Instance.colorToMove))
                    {
                        Debug.Log("Found a piece");
                        _fromSquare = _selectedSquare;
                        _fromSquare.SelectSquare();
                        _isSelection = false;
                    }
                }
            }
            else if (_fromSquare != _selectedSquare)
            {
                _toSquare = _selectedSquare;
                _fromSquare.UnselectSquare(_toSquare);
                _fromSquare.MoveTo(_toSquare);
                _isSelection = true;
            }
            */
        }
    }

}
