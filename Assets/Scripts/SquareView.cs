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

    public void UnselectSquare()
    {
        GetComponentInParent<SpriteRenderer>().material = lastMoveSquareMat;
    }

    public void ResetSquareColor()
    {
        GetComponent<SpriteRenderer>().material = material;
    }
}
