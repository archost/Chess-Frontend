using UnityEngine;

public class SquareView : MonoBehaviour
{
    public Vector2 position;
    public PieceView piece;
    public Material material;
    public int squareIndex;

    [SerializeField] public Material selectedMaterial;    
    

    void Start()
    {
        
    }

    public void SelectSquare()
    {
        GetComponentInParent<SpriteRenderer>().material = selectedMaterial;
    }

    public void UnselectSquare()
    {
        GetComponentInParent<SpriteRenderer>().material = material;
    }

    public void MoveTo(SquareView destinationSquare)
    {
        if (piece == null) return;

        if (destinationSquare.piece != null)
        {
            Destroy(destinationSquare.piece.gameObject);
        }

        piece.transform.SetParent(destinationSquare.transform, false);

        destinationSquare.piece = piece;
        piece.square = destinationSquare; // Обновляем ссылку у фигуры на новую клетку
        piece = null;

        BoardManager.Instance.Squares[destinationSquare.squareIndex] = destinationSquare.piece.pieceType;
        BoardManager.Instance.Squares[squareIndex] = 0;
    }
}
