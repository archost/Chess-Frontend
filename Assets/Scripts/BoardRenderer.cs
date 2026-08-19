using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
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
    private GameObject[] whitePromotionUISquares;
    private GameObject[] blackPromotionUISquares;
    private GameObject whitePromotionMenu;
    private GameObject blackPromotionMenu;
    private int[] promotionPieces;
    [SerializeField] private GameObject dimPrefab;
    private GameObject dim;

    private const float MOVE_DURATION = 0.1f;

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
        LeanTween.reset();
        dim = Instantiate(dimPrefab, transform);
        squares = new GameObject[64];
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
        SquareView squareView = squares[index].GetComponent<SquareView>();
        if (squareView.isHighlighted)
        {
            squares[index].GetComponent<SpriteRenderer>().material.SetColor("_BaseColor", squareView.lastColor);
            squareView.isHighlighted = false;
        }
        else
        {
            squareView.lastColor = squares[index].GetComponent<SpriteRenderer>().material.GetColor("_BaseColor");
            squares[index].GetComponent<SpriteRenderer>().material.SetColor("_BaseColor", Color.Lerp(squareView.baseColor, Color.red, 0.5f));
            squareView.isHighlighted = true;
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

    public void SelectSquare(int index)
    {
        Color baseColor = squares[index].GetComponent<SquareView>().baseColor;
        squares[index].GetComponent<SpriteRenderer>().material.SetColor("_BaseColor", Color.Lerp(baseColor, Color.yellow, 0.5f));
    }

    public void UnselectSquare(int index)
    {
        Color baseColor = squares[index].GetComponent<SquareView>().baseColor;
        squares[index].GetComponent<SpriteRenderer>().material.SetColor("_BaseColor", Color.Lerp(baseColor, Color.orange, 0.3f));
    }

    public void ResetSquareColor(int index)
    {
        Color baseColor = squares[index].GetComponent<SquareView>().baseColor;
        squares[index].GetComponent<SpriteRenderer>().material.SetColor("_BaseColor", baseColor);
    }

    public void ResetAllSquaresColor()
    {
        foreach (var square in squares)
        {
            Color baseColor = square.GetComponent<SquareView>().baseColor;
            square.GetComponent<SpriteRenderer>().material.SetColor("_BaseColor", baseColor);
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
                
            }
        }
    }

    public void VizualizeMove(Move move)
    {
        GameObject fromSquare = squares[move.startSquare];
        GameObject toSquare = squares[move.targetSquare];

        SquareView fromSquareView = fromSquare.GetComponent<SquareView>();
        SquareView toSquareView = toSquare.GetComponent<SquareView>();

        GameObject enpassantSquare = null;
        SquareView enPassantSquareView = null;

        int piecePromoteTo = move.promoteTo;

        if (move.enPassantTargetPawnSquare != -1)
        {
            enpassantSquare = squares[move.enPassantTargetPawnSquare];
            enPassantSquareView = enpassantSquare.GetComponent<SquareView>();
        }

        if (move.type == MoveType.EnPassant)
        {
            PieceView movingPiece = fromSquareView.piece;

            // Если это en passant, то вот эта ветка будет вместо взятия
            // Точно также на клетке пешки, которую берут на проход, выключаем фигуру
            enPassantSquareView.piece.gameObject.SetActive(false);
            // И обнуляем ну этой клетки ссылку на фигуру
            enPassantSquareView.piece = null;

            // Целевой клетке присваивается PieceView стартовой
            toSquareView.piece = fromSquareView.piece;
            // Обновить значение клетки у фигуры
            toSquareView.piece.square = toSquareView;

            // PieceView стартовой клетки обнуляется
            fromSquareView.piece = null;

            toSquareView.piece.transform.SetParent(toSquare.transform);

            LeanTween.move(movingPiece.gameObject, toSquareView.transform.position, MOVE_DURATION)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    AudioManager.Instance.PlayMoveSound(move.type);
                });
        }
        else if (move.type == MoveType.Promote)
        {
            PieceView movingPiece = fromSquareView.piece;

            if (toSquareView.piece != null)
            {
                // Если на целевой клетке есть фигура - выключить ее
                toSquareView.piece.gameObject.SetActive(false);
            }

            // Пешке присваиваем ссылку на целевую клетку
            fromSquareView.piece.square = toSquareView;

            // Перемещаем пешку на целевую клетку, не меняя при этом ее координат
            fromSquareView.piece.transform.SetParent(toSquare.transform);

            // PieceView стартовой клетки обнуляется
            fromSquareView.piece = null;

            GameObject instance = Instantiate(piecePrefabsData.GetPrefab(piecePromoteTo), toSquare.transform);
            toSquareView.piece = instance.GetComponent<PieceView>();
            instance.GetComponent<PieceView>().square = toSquareView;
            // Выключаем на время анимации фигуру, в которую пешка превратилась
            instance.SetActive(false);

            LeanTween.move(movingPiece.gameObject, toSquareView.transform.position, MOVE_DURATION)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    movingPiece.gameObject.SetActive(false);
                    instance.SetActive(true);
                    AudioManager.Instance.PlayMoveSound(move.type);
                });
        }
        else
        {
            PieceView movingPiece = fromSquareView.piece;

            if (toSquareView.piece != null)
            {
                // Если на целевой клетке есть фигура - выключить ее
                toSquareView.piece.gameObject.SetActive(false);
                // Обнулить ссылку на фигуру у целевой клетки
                toSquareView.piece = null;
            }

            // Целевой клетке присваивается PieceView стартовой
            toSquareView.piece = fromSquareView.piece;
            // Обновить значение клетки у фигуры
            toSquareView.piece.square = toSquareView;

            // PieceView стартовой клетки обнуляется
            fromSquareView.piece = null;

            toSquareView.piece.transform.SetParent(toSquare.transform);

            LeanTween.move(movingPiece.gameObject, toSquareView.transform.position, MOVE_DURATION)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    AudioManager.Instance.PlayMoveSound(move.type);
                });
        }
    }

    public void UndoVizualizeMove(Move move)
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

        if (move.type == MoveType.Promote)
        {
            // Нам нужно уничтожить объект фигуры, в которую пешка превратилась
            // И обнулить у fromSquare ссылку на фигуру
            Destroy(fromSquareView.piece.gameObject);
            fromSquareView.piece = null;

            // Мы восстановили пешку
            RestorePiece(fromSquareView, move.promotedPawn);
            // Но после этого она привязана к fromSquareView
            // А нам нужно привязать ее к toSquareView

            // Целевой клетке присваивается PieceView стартовой
            toSquareView.piece = fromSquareView.piece;
            fromSquareView.piece = null;
            // Обновить значение клетки у фигуры
            toSquareView.piece.square = toSquareView;

            PieceView movingPiece = toSquareView.piece;

            // Также нужно иметь в виду, что превращение могло быть после взятия
            if (move.capturedPiece != 0)
            {
                RestorePiece(fromSquareView, move.capturedPiece);
            }

            movingPiece.transform.SetParent(toSquare.transform);

            LeanTween.move(movingPiece.gameObject, toSquareView.transform.position, MOVE_DURATION)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    AudioManager.Instance.PlayMoveSound(move.type);
                });
        }
        else
        {
            PieceView movingPiece = fromSquareView.piece;

            // Целевой клетке присваивается PieceView стартовой
            toSquareView.piece = fromSquareView.piece;
            // Обновить значение клетки у фигуры
            toSquareView.piece.square = toSquareView;

            // PieceView стартовой клетки обнуляется
            fromSquareView.piece = null;

            if (move.capturedPiece != 0)
            {
                if (move.type == MoveType.EnPassant)
                    RestorePiece(enPassantSquareView, move.capturedPiece);
                else
                    RestorePiece(fromSquareView, move.capturedPiece);
            }

            toSquareView.piece.transform.SetParent(toSquare.transform);

            LeanTween.move(movingPiece.gameObject, toSquareView.transform.position, MOVE_DURATION)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    AudioManager.Instance.PlayMoveSound(move.type);
                });
        }
    }

    private void RestorePiece(SquareView square, int pieceType)
    {
        PieceView[] pieces = square.transform.GetComponentsInChildren<PieceView>(true);
        foreach (var piece in pieces)
        {
            if (piece.pieceType == pieceType)
            {
                piece.gameObject.SetActive(true);
                piece.transform.localPosition = Vector3.zero;
                square.piece = piece;
                return;
            }
        }
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
