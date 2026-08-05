using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public int[] squares;
    public string fen = "7k/3N2qp/b5r1/2p1Q1N1/Pp4PK/7P/1P3p2/6r1 w - - 7 4";
    public int colorToMove = 8;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            squares = new int[64];
            FenToBoard();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void FenToBoard()
    {
        var piecesValues = new Dictionary<char, int>()
        {
            { 'p', Piece.Pawn },
            { 'n', Piece.Knight },
            { 'b', Piece.Bishop },
            { 'r', Piece.Rook },
            { 'q', Piece.Queen },
            { 'k', Piece.King }
        };

        string fenBoard = fen.Split(' ')[0];
        int currentPiece;
        int file = 0, rank = 7;

        foreach (char symbol in fenBoard)
        {
            currentPiece = 0;
            if (symbol == '/')
            {
                file = 0;
                rank--;
            }
            else
            {
                if (char.IsDigit(symbol))
                {
                    file += (int)char.GetNumericValue(symbol);
                }
                else
                {
                    currentPiece |= (char.IsUpper(symbol)) ? Piece.White : Piece.Black;
                    currentPiece |= piecesValues[char.ToLower(symbol)];
                    squares[file + 8 * rank] = currentPiece;
                    file++;
                }
            }

        }
    }

    public void ExecuteMove(int fromIndex, int toIndex)
    {
        // TODO: определить, это обычный ход, или 
        // // Ракировка 
        // // EN PASSANT!!!
        // // Promotion

        // Пока предположим, что это просто обычный ход
        // И пока без разницы, это взятие или нет
        squares[toIndex] = squares[fromIndex];
        squares[fromIndex] = 0;

        // Обновляем чей ход
        colorToMove = colorToMove ^ 24;
        
        // Вызвать метод обновления визуала у BoardRenderer
        BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex);
    }
}
