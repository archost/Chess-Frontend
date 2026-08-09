using UnityEngine;

public class SquareView : MonoBehaviour
{
    public Vector2 position;
    public PieceView piece;
    public Material material;
    public int squareIndex;

    public GameObject moveIndicator;
    public GameObject captureIndicator;

    public bool isHighlighted = false;

    public Material lastMaterial;

    void Start()
    {
        GetComponent<SpriteRenderer>().material = material;
    }
}
