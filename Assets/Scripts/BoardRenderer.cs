using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class BoardRenderer : MonoBehaviour
{
    public static BoardRenderer Instance { get; private set; }

    [SerializeField] private Material lightColor;
    [SerializeField] private Material darkColor;

    [SerializeField] private GameObject squarePrefab;
    [SerializeField] private PiecePrefabsData piecePrefabsData;

    [SerializeField] public Material selectedMat;
    [SerializeField] public Material lastMoveSquareMat;
    [SerializeField] public Material legalMoveMaterial;
    [SerializeField] public Material highlightMaterial;

    private GameObject[] squares;
    private List<GameObject> pieces;
    private GameObject[] whitePromotionUISquares;
    private GameObject[] blackPromotionUISquares;
    private GameObject whitePromotionMenu;
    private GameObject blackPromotionMenu;
    private int[] promotionPieces;
    [SerializeField] private GameObject dimPrefab;
    private GameObject dim;

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
        dim = Instantiate(dimPrefab, transform);
        squares = new GameObject[64];
        pieces = new List<GameObject>();
        whitePromotionUISquares = new GameObject[4];
        blackPromotionUISquares = new GameObject[4];
        promotionPieces = new int[4] { 
            Piece.Rook,
            Piece.Bishop, 
            Piece.Queen, 
            Piece.Knight
        };
        DrawBoard();
        DrawPieces();
        whitePromotionMenu = CreatePromotionMenu(Piece.White);
        blackPromotionMenu = CreatePromotionMenu(Piece.Black);
    }

    void Update()
    {

    }

    public void ToggleHighlightSquare(int index)
    {
        GameObject square = squares[index];
        if (square.GetComponent<SquareView>().isHighlighted)
        {
            SetSquareMaterial(index, square.GetComponent<SquareView>().lastMaterial);
            square.GetComponent<SquareView>().isHighlighted = false;
        }
        else
        {
            square.GetComponent<SquareView>().lastMaterial = square.GetComponent<SpriteRenderer>().material;
            square.GetComponent<SpriteRenderer>().material = highlightMaterial;
            square.GetComponent<SquareView>().isHighlighted = true;
        }
    }

    public void RemoveAllHighlighted()
    {
        for (int i = 0; i < 64; i++)
        {
            GameObject square = squares[i];
            if (square.GetComponent<SquareView>().isHighlighted)
            {
                ResetSquareColor(i);
                square.GetComponent<SquareView>().isHighlighted = false;
            }
        }
    }

    public void SetSquareMaterial(int index, Material mat)
    {
        squares[index].GetComponent<SpriteRenderer>().material = mat;
    }

    public void SelectSquare(int index)
    {
        squares[index].GetComponent<SpriteRenderer>().material = selectedMat;
    }

    public void UnselectSquare(int index)
    {
        squares[index].GetComponent<SpriteRenderer>().material = lastMoveSquareMat;
    }

    public void ResetSquareColor(int index)
    {
        squares[index].GetComponent<SpriteRenderer>().material = squares[index].GetComponent<SquareView>().material;
    }

    public void ResetAllSquaresColor()
    {
        foreach (var square in squares)
        {
            square.GetComponent<SpriteRenderer>().material = square.GetComponent<SquareView>().material;
        }
    }

    public void ShowMoveIsLegal(int index)
    {
        if (BoardManager.Instance.squares[index] != 0)
            squares[index].GetComponent<SquareView>().captureIndicator.SetActive(true);
        else
            squares[index].GetComponent<SquareView>().moveIndicator.SetActive(true);
    }

    public void ResetLegalMovesIndication()
    {
        foreach (var square in squares)
        {
            square.GetComponent<SquareView>().captureIndicator.SetActive(false);
            square.GetComponent<SquareView>().moveIndicator.SetActive(false);
        }
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
                instance.GetComponent<PieceView>().pieceType = currentPiece;
                pieces.Add(instance);
                
            }
        }
    }

    public void UpdateBoardAfterAMove(int fromIndex, int toIndex, int piecePromoteTo = 0, int capturedPiece = 0, bool enPassant = false)
    {
        GameObject fromSquare = squares[fromIndex];
        GameObject toSquare = squares[toIndex];

        SquareView fromSquareView = fromSquare.GetComponent<SquareView>();
        SquareView toSquareView = toSquare.GetComponent<SquareView>();
        if (piecePromoteTo != 0 && piecePromoteTo != -1)
        {
            Debug.Log("promotion");
            // Если на целевой клетке есть фигура - удалить ее
            if (toSquareView.piece != null)
            {
                // Destroy(toSquareView.piece.gameObject);
                toSquareView.piece.gameObject.SetActive(false);
            }
            // Destroy(fromSquareView.piece.gameObject);
            fromSquareView.piece.gameObject.SetActive(false);
            fromSquareView.piece = null;

            GameObject instance = Instantiate(piecePrefabsData.GetPrefab(piecePromoteTo), toSquare.transform);
            toSquareView.piece = instance.GetComponent<PieceView>();
            instance.GetComponent<PieceView>().square = toSquareView;
            pieces.Add(instance);

        }
        else if (piecePromoteTo == -1)
        {
            Debug.Log("Undo promotion");
            // Отмена превращения

            // Находим пешку
            // Проблема в том, что это единственный ход, при котором фигура после него исчезает
            // Так что мы не храним нигде информацию о том, что это за фигура
            // Поэтому мы не знаем, какой у нее цвет, можно выполнить поиск дважды?
            int promotedPawnIndex = FindPiece(Piece.Pawn | Piece.White, toSquareView);
            if (promotedPawnIndex == -1)
                promotedPawnIndex = FindPiece(Piece.Pawn | Piece.Black, toSquareView);
            // Включаем пешку и устанавливаем этой клетке ссылку на нее
            pieces[promotedPawnIndex].SetActive(true);
            toSquareView.piece = pieces[promotedPawnIndex].GetComponent<PieceView>();

            // Нам нужно уничтожить объект фигуры, в которую пешка превратилась
            // И обнулить у fromSquare ссылку на фигуру
            
            Destroy(fromSquareView.piece.gameObject);
            fromSquareView.piece = null;

            // Также нужно иметь в виду, что превращение могло быть после взятия
            // В этом случае нам нужно:
            // Найти фигуру, которая была съедена
            // Включить ее
            // Установить fromSquare ссылку на нее
            if (capturedPiece != 0)
            {
                int capturedPieceIndex = FindPiece(capturedPiece, fromSquareView);
                Debug.Log("Фигура, которую мы пытаемся восстановить: " +  capturedPieceIndex);
                pieces[capturedPieceIndex].SetActive(true);
                fromSquareView.piece = pieces[capturedPieceIndex].GetComponent<PieceView>();
            }
            
        }
        else
        {
            // Это значит, что это обратное взятие
            if (capturedPiece != 0)
            {
                Debug.Log("Undo take");
                // Найти в пуле фигур нужную фигуру на стартовой клетке
                int capturedPieceIndex = FindPiece(capturedPiece, fromSquareView);
                // У нее сохранилось значение поля square, она принадлежит ему, менять ничего не нужно
                // Нужно только установить значение fromSquareView - piece - эта фигура
                if (capturedPieceIndex != -1)
                {
                    pieces[capturedPieceIndex].SetActive(true);
                    if (enPassant)
                        fromSquareView.piece = pieces[capturedPieceIndex].GetComponent<PieceView>();
                }
                else
                    Debug.Log("It is -1 indeed");

                if (fromSquareView.piece != null && !enPassant)
                {
                    // Установить текущей фигуре - родителя - целевую клетку
                    fromSquareView.piece.transform.SetParent(toSquare.transform, false);

                    // Целевой клетке присваивается PieceView стартовой
                    toSquareView.piece = fromSquareView.piece;

                    // У стартовой клетки PieceView должен стать PieceView включенной фигуры
                    fromSquareView.piece = pieces[capturedPieceIndex].GetComponent<PieceView>();

                    // Обновить значение клетки у новой фигуры
                    toSquareView.piece.square = toSquareView;
                }
            }
            else
            {
                // Если на целевой клетке есть фигура - выключить ее
                if (toSquareView.piece != null)
                {
                    toSquareView.piece.gameObject.SetActive(false);
                    toSquareView.piece = null;
                }

                if (fromSquareView.piece != null)
                {
                    // Установить текущей фигуре - родителя - целевую клетку
                    fromSquareView.piece.transform.SetParent(toSquare.transform, false);

                    // Целевой клетке присваивается PieceView стартовой
                    toSquareView.piece = fromSquareView.piece;
                    // PieceView стартовой клетки обнуляется
                    fromSquareView.piece = null;

                    // Обновить значение клетки у новой фигуры
                    toSquareView.piece.square = toSquareView;
                }
            }
        }
    }

    private int FindPiece(int piece, SquareView square)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].GetComponent<PieceView>().pieceType == piece &&
                pieces[i].GetComponent<PieceView>().square == square)
            {
                return i;
            }
        }

        return -1;
    }

    private GameObject CreatePromotionMenu(int color)
    {
        GameObject menu = new GameObject();
        menu.transform.SetParent(transform, false);

        int squareIndex = 0;
        for (int rank = 0; rank < 2; rank++)
        {
            for (int file = 0; file < 2; file++)
            {
                Material squareMaterial = (file + rank) % 2 != 0 ? lightColor : darkColor;

                GameObject instance = Instantiate(squarePrefab, menu.transform);
                instance.transform.position = new Vector2(-0.5f + file, -0.5f + rank);
                instance.GetComponent<SpriteRenderer>().sortingOrder = 2;

                SquareView squareView = instance.GetComponent<SquareView>();

                squareView.position = new Vector2(file, rank);
                squareView.material = squareMaterial;
                squareView.squareIndex = squareIndex;
                if (color == Piece.White)
                    whitePromotionUISquares[squareIndex++] = instance;
                else
                    blackPromotionUISquares[squareIndex++] = instance;
            }
        }

        DrawPromotionPieces(color);
        dim.SetActive(false);
        menu.SetActive(false);

        return menu;
    }

    private void DrawPromotionPieces(int color)
    {
        for (int i = 0; i < 4; i++)
        {
            promotionPieces[i] |= color;
        }

        for (int i = 0; i < 4; i++)
        {
            int currentPiece = promotionPieces[i];
            if (currentPiece != 0)
            {
                if (color == Piece.White)
                {
                    GameObject instance = Instantiate(piecePrefabsData.GetPrefab(currentPiece), whitePromotionUISquares[i].transform);
                    instance.GetComponent<SpriteRenderer>().sortingOrder = 3;
                    whitePromotionUISquares[i].GetComponent<SquareView>().piece = instance.GetComponent<PieceView>();
                    instance.GetComponent<PieceView>().square = whitePromotionUISquares[i].GetComponent<SquareView>();
                }
                else
                {
                    GameObject instance = Instantiate(piecePrefabsData.GetPrefab(currentPiece), blackPromotionUISquares[i].transform);
                    instance.GetComponent<SpriteRenderer>().sortingOrder = 3;
                    blackPromotionUISquares[i].GetComponent<SquareView>().piece = instance.GetComponent<PieceView>();
                    instance.GetComponent<PieceView>().square = blackPromotionUISquares[i].GetComponent<SquareView>();
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            promotionPieces[i] &= 7;
        }
    }

    public void ShowPromotionMenu(int color)
    {
        dim.SetActive(true);
        if (color == Piece.White)
        {
            whitePromotionMenu.SetActive(true);
        }
        else
        {
            blackPromotionMenu.SetActive(true);
        }
    }

    public void HidePromotionMenu()
    {
        dim.SetActive(false);
        whitePromotionMenu.SetActive(false);
        blackPromotionMenu.SetActive(false);
    }
}
