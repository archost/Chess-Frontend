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

    public void UpdateBoardAfterAMove(Move move, int piecePromoteTo, bool undo = false)
    {
        GameObject fromSquare = squares[move.startSquare];
        GameObject toSquare = squares[move.targetSquare];

        SquareView fromSquareView = fromSquare.GetComponent<SquareView>();
        SquareView toSquareView = toSquare.GetComponent<SquareView>();

        GameObject enpassantSquare = null;
        SquareView enPassantSquareView = null;

        if (move.enPassantTargetPawnSquare != -1)
        {
            enpassantSquare = squares[move.enPassantTargetPawnSquare];
            enPassantSquareView = enpassantSquare.GetComponent<SquareView>();
        }

        if (!undo)
        {
            if (move.type == MoveType.EnPassant)
            {
                // Если это en passant, то вот эта ветка будет вместо взятия
                // Точно также на клетке пешки, которую берут на проход, выключаем фигуру
                enPassantSquareView.piece.gameObject.SetActive(false);
                // И обнуляем ну этой клетки ссылку на фигуру
                enPassantSquareView.piece = null;
            }
            if (toSquareView.piece != null)
            {
                // Если на целевой клетке есть фигура - выключить ее
                toSquareView.piece.gameObject.SetActive(false);
                // Обнулить ссылку на фигуру у целевой клетки
                toSquareView.piece = null;
            }
            if (fromSquareView.piece != null)
            {
                if (move.type == MoveType.Promote)
                {
                    // Если это превращение, то на целевой клетке нужно создать новую фигуру
                    // А не перемещать туда фигуру из стартовой клетки
                    GameObject instance = Instantiate(piecePrefabsData.GetPrefab(piecePromoteTo), toSquare.transform);
                    toSquareView.piece = instance.GetComponent<PieceView>();
                    instance.GetComponent<PieceView>().square = toSquareView;
                    pieces.Add(instance);

                    // А фигура на стартовой клетке выключается
                    // И PieceView стартовой клетки обнуляется
                    fromSquareView.piece.gameObject.SetActive(false);
                    fromSquareView.piece = null;
                }
                else
                {
                    // Установить текущей фигуре - родителя - целевую клетку
                    fromSquareView.piece.transform.SetParent(toSquare.transform, false);
                    // Целевой клетке присваивается PieceView стартовой
                    toSquareView.piece = fromSquareView.piece;
                    // Обновить значение клетки у фигуры
                    toSquareView.piece.square = toSquareView;

                    // PieceView стартовой клетки обнуляется
                    fromSquareView.piece = null;
                }
            }
        }
        else
        {
            if (move.type != MoveType.Promote)
            {
                if (fromSquareView.piece != null)
                {
                    // Сначала двинем обратно фигуру

                    // Установить текущей фигуре - родителя - целевую клетку
                    fromSquareView.piece.transform.SetParent(toSquare.transform, false);
                    // Целевой клетке присваивается PieceView стартовой
                    toSquareView.piece = fromSquareView.piece;
                    // Обновить значение клетки у фигуры
                    toSquareView.piece.square = toSquareView;

                    // PieceView стартовой клетки обнуляется
                    fromSquareView.piece = null;
                }
                if (move.capturedPiece != 0)
                {
                    // Это значит, что этим ходом нам нужно возродить эту capturedPiece
                    // То есть включить ее, и у клетки fromSquare установить ссылку на нее
                    // Правда ее сначала нужно найти в массиве фигур

                    if (move.type == MoveType.EnPassant)
                    {
                        // Если это был en passant, то включаем пешку на enpassantSquare
                        int capturedPieceIndex = FindPiece(move.capturedPiece, enPassantSquareView);

                        pieces[capturedPieceIndex].gameObject.SetActive(true);
                        enPassantSquareView.piece = pieces[capturedPieceIndex].GetComponent<PieceView>();
                    }
                    else
                    {
                        // Если это был обычный ход со взятием, то мы включаем фигуру там, откуда мы уходим
                        int capturedPieceIndex = FindPiece(move.capturedPiece, fromSquareView);

                        pieces[capturedPieceIndex].gameObject.SetActive(true);
                        fromSquareView.piece = pieces[capturedPieceIndex].GetComponent<PieceView>();
                    }
                }
            }
            else
            {
                // Отмена превращения
                // Находим пешку, которая до этого просто пропала
                // Мы не знаем, какой у нее цвет, потому что не храним эту информацию в Move
                // Поэтому поиск нужно выполнить дваджы
                int promotedPawnIndex = FindPiece(Piece.Pawn | Piece.White, toSquareView);
                if (promotedPawnIndex == -1)
                    promotedPawnIndex = FindPiece(Piece.Pawn | Piece.Black, toSquareView);
                pieces[promotedPawnIndex].SetActive(true);
                toSquareView.piece = pieces[promotedPawnIndex].GetComponent<PieceView>();

                // Нам нужно уничтожить объект фигуры, в которую пешка превратилась
                // И обнулить у fromSquare ссылку на фигуру
                pieces.Remove(fromSquareView.piece.gameObject); // Исправление бага - после удаления ссылка на фигуру все еще лежит в pieces
                Destroy(fromSquareView.piece.gameObject);
                fromSquareView.piece = null;

                // Также нужно иметь в виду, что превращение могло быть после взятия
                // В этом случае нам нужно:
                // Найти фигуру, которая была съедена
                // Включить ее
                // Установить fromSquare ссылку на нее
                if (move.capturedPiece != 0)
                {
                    int capturedPieceIndex = FindPiece(move.capturedPiece, fromSquareView);
                    pieces[capturedPieceIndex].SetActive(true);
                    fromSquareView.piece = pieces[capturedPieceIndex].GetComponent<PieceView>();
                }
            }
            
        }
    }

    private int FindPiece(int piece, SquareView square)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].GetComponent<PieceView>().pieceType == piece && pieces[i].GetComponent<PieceView>().square == square)
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
