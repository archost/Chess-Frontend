using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class BoardRenderer : MonoBehaviour
{
    [SerializeField] private Material lightColor;
    [SerializeField] private Material darkColor;

    [SerializeField] private GameObject squarePrefab;
    [SerializeField] private PiecePrefabsData piecePrefabsData;

    private GameObject[] squares;

    void Start()
    {
        squares = new GameObject[64];
        DrawBoard();
        DrawPieces();
    }

    void Update()
    {

    }

    private void DrawBoard()
    {
        int squareIndex = 0;
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                bool isLightSquare = (file + rank) % 2 != 0;
                Material squareMaterial = isLightSquare ? lightColor : darkColor;

                GameObject instance = Instantiate(squarePrefab, transform);
                instance.transform.position = new Vector2(-3.5f + file, -3.5f + rank);

                SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                renderer.material = squareMaterial;

                instance.GetComponent<SquareView>().position = new Vector2(file, rank);

                squares[squareIndex++] = instance;
            }
        }
    }

    private void DrawPieces()
    {
        for (int i = 0; i < 64; i++)
        {
            int currentPiece = BoardManager.Squares[i];
            if (currentPiece != 0)
            {
                GameObject instance = Instantiate(piecePrefabsData.GetPrefab(currentPiece), squares[i].transform);
                squares[i].GetComponent<SquareView>().piece = instance.GetComponent<PieceView>();
                instance.GetComponent<PieceView>().square = squares[i].GetComponent<SquareView>();
            }
        }
    }
}
