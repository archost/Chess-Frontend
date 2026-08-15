using UnityEngine;

public class SquareView : MonoBehaviour
{
    public Vector2 position;
    public PieceView piece;
    public int squareIndex;

    public Material material;
    public Color baseColor;
    public Color lastColor;

    public GameObject moveIndicator;
    public GameObject captureIndicator;

    public bool isHighlighted = false;

    void Start()
    {
        GetComponent<SpriteRenderer>().material = material;
        baseColor = material.GetColor("_BaseColor");
    }
}
