using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    private SquareView _selectedSquare;

    private SquareView _fromSquare;
    private SquareView _toSquare;

    private PieceView _selectedPiece;

    private bool _isSelection = true;

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
                    Debug.Log("Found a piece");
                    _fromSquare = _selectedSquare;
                    _fromSquare.SelectSquare();
                    _isSelection = false;
                }
            }
            else if (_fromSquare != _selectedSquare)
            {
                _toSquare = _selectedSquare;
                _fromSquare.UnselectSquare(_toSquare);
                _fromSquare.MoveTo(_toSquare);
                _isSelection = true;
            }
        }
    }

}
