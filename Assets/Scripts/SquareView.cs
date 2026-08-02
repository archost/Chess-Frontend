using UnityEngine;

public class SquareView : MonoBehaviour
{
    public Vector2 position;
    public PieceView piece;
    public Material material;
    public int squareIndex;

    [SerializeField] public Material selectedMat;    
    [SerializeField] public Material lastMoveSquareMat;    
    

    void Start()
    {
        GetComponent<SpriteRenderer>().material = material;
    }

    public void SelectSquare()
    {
        GetComponentInParent<SpriteRenderer>().material = selectedMat;
    }

    public void UnselectSquare(SquareView destinationSquare)
    {
        GetComponentInParent<SpriteRenderer>().material = lastMoveSquareMat;
        destinationSquare.GetComponent<SpriteRenderer>().material = selectedMat;
    }

    public void ResetSquareColor()
    {
        GetComponent<SpriteRenderer>().material = material;
    }

    public void MoveTo(SquareView destinationSquare)
    {
        if (piece == null) return;

        if (destinationSquare.piece != null)
        {
            Destroy(destinationSquare.piece.gameObject);
            AudioManager.Instance.PlayCaptureSound();
        }
        else
        {
            AudioManager.Instance.PlayMoveSound();
        }

        piece.transform.SetParent(destinationSquare.transform, false);

        destinationSquare.piece = piece;
        piece.square = destinationSquare; // Обновляем ссылку у фигуры на новую клетку
        piece = null;

        BoardManager.Instance.squares[destinationSquare.squareIndex] = destinationSquare.piece.pieceType;
        BoardManager.Instance.squares[squareIndex] = 0;
    }
}
