using System;
using System.Collections;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;

public class BoardRenderer : MonoBehaviour
{
    public static BoardRenderer Instance { get; private set; }

    [SerializeField] private Material lightColor;
    [SerializeField] private Material darkColor;

    [SerializeField] private GameObject squarePrefab;
    [SerializeField] private PiecePrefabsData piecePrefabsData;

    private GameObject[] squares;

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

    void Start()
    {
        squares = new GameObject[64];
        DrawBoard();
        DrawPieces();
    }

    void Update()
    {

    }
    

    public SquareView getSquare(int index)
    {
        return squares[index].GetComponent<SquareView>();
    }

    private void DrawBoard()
    {
        int squareIndex = 0;
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                Material squareMaterial = (file + rank) % 2 != 0 ? lightColor : darkColor;

                GameObject instance = Instantiate(squarePrefab, transform);
                instance.transform.position = new Vector2(-3.5f + file, -3.5f + rank);

                SquareView squareView = instance.GetComponent<SquareView>();

                squareView.position = new Vector2(file, rank);
                squareView.material = squareMaterial;
                squareView.squareIndex = squareIndex;

                squares[squareIndex++] = instance;
            }
        }
    }

    private void DrawPieces()
    {
        for (int i = 0; i < 64; i++)
        {
            int currentPiece = BoardManager.Instance.squares[i];
            if (currentPiece != 0)
            {
                GameObject instance = Instantiate(piecePrefabsData.GetPrefab(currentPiece), squares[i].transform);
                squares[i].GetComponent<SquareView>().piece = instance.GetComponent<PieceView>();
                instance.GetComponent<PieceView>().square = squares[i].GetComponent<SquareView>();
            }
        }
    }

    public void UpdateBoardAfterAMove(int fromIndex, int toIndex)
    {
        // ѕока сделаем обычный ход, из одной клетки в другую

        GameObject fromSquare = squares[fromIndex];
        GameObject toSquare = squares[toIndex];

        SquareView fromSquareView = fromSquare.GetComponent<SquareView>();
        SquareView toSquareView = toSquare.GetComponent<SquareView>();

        // ≈сли на целевой клетке есть фигура - удалить ее
        if (toSquareView.piece != null)
        {
            Destroy(toSquareView.piece.gameObject);
        }

        // ”становить текущей фигуре родител€ - целевую клетку
        fromSquareView.piece.transform.SetParent(toSquare.transform, false);

        // ќбновить значени€ фигур у клеток
        toSquareView.piece = fromSquareView.piece;
        fromSquareView.piece = null;

        // ќбновить значение клетки у новой фигуры
        toSquareView.piece.square = toSquareView;
    }
}
