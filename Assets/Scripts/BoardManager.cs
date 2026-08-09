using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public int[] squares;
    public string fen = "";
    // rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
    // r3k2r/2b4q/8/8/8/8/2B4Q/R3K2R w KQkq - 0 1
    // 7k/3N2qp/b5r1/2p1Q1N1/Pp4PK/7P/1P3p2/6r1 w - - 7 4

    public int[] whitePromotionDeck;
    public int[] blackPromotionDeck;

    public int colorToMove = 8;

    public int enPassantTargetSquare = -1;

    public bool canWhiteCastleKingside = true;
    public bool canWhiteCastleQueenside = true;
    public bool canBlackCastleKingside = true;
    public bool canBlackCastleQueenside = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            squares = new int[64];
            FenToBoard(); 
            InitializePromotionDecks();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePromotionDecks()
    {
        whitePromotionDeck = new int[4]{ 
            Piece.Rook | Piece.White,
            Piece.Bishop | Piece.White,
            Piece.Queen | Piece.White,
            Piece.Knight | Piece.White
        };
        blackPromotionDeck = new int[4]{
            Piece.Rook | Piece.Black,
            Piece.Bishop | Piece.Black,
            Piece.Queen | Piece.Black,
            Piece.Knight | Piece.Black
        };

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

    public void ExecuteMove(Move move, int piecePromoteTo = Piece.None)
    {
        enPassantTargetSquare = -1;

        Move rookCastlingMove = new Move();

        int fromIndex = move.startSquare;
        int toIndex = move.targetSquare;

        int pieceToMove = squares[fromIndex];
        int pieceToCapture = squares[toIndex];

        // Итак, нам нужно понять, когда снимать флаги рокировок
        // Рокироваться нельзя вообще, если король делал любой ход (в том числе рокировку) - это понятно и легко
        // Рокироваться нельзя в определенную сторону, если:
        // - Соответствующая ладья двигалась до этого
        // - Соответствующая ладья была съедена
        // То есть нам понадобится 4 проверки на движение
        // + 4 проверки на то, что ладья была съедена
        // Чето такое себе...
        if (Piece.GetType(pieceToMove) == Piece.Rook)
        {
            if (fromIndex == 0)
                canWhiteCastleQueenside = false;
            if (fromIndex == 7)
                canWhiteCastleKingside = false;
            if (fromIndex == 56)
                canBlackCastleQueenside = false;
            if (fromIndex == 63)
                canBlackCastleKingside = false;
        }
        if (Piece.GetType(pieceToCapture) == Piece.Rook)
        {
            if (toIndex == 0)
                canWhiteCastleQueenside = false;
            if (toIndex == 7)
                canWhiteCastleKingside = false;
            if (toIndex == 56)
                canBlackCastleQueenside = false;
            if (toIndex == 63)
                canBlackCastleKingside = false;
        }

        // Мы здесь ничего не определяем, тип хода определяет MoveGenerator
        // А точнее создает ход с уже определенным типом
        // Здесь мы просто выполняем ход соответствующим образом

        // Если это обычный ход, значит нам просто нужно поставить фигуру на целевой индекс
        // И стереть ее со стартового индекса
        if (move.type == MoveType.Move)
        {
            squares[toIndex] = squares[fromIndex];
            squares[fromIndex] = 0;
        }

        // Для отмены не требуется специальная логика

        // Если это превращение, то мы должны получить ДОПОЛНИТЕЛЬНУЮ ИНФОРМАЦИЮ о том, в какую фигуру превращать
        // А затем выполнить ход пешкой и заменить ее на эту фигуру
        // Для отмены превращения нужна отдельная логика
        // Превращение - отдельный ход, а не дополнение к ходу "ход"
        if (move.type == MoveType.Promote)
        {
            // Откуда мы получим информацию о том, в какую фигуру превращать, где это должно определяться?
            // Когда мы делаем второй клик, GameController должен вызвать какой-то метод у BoardRenderer
            squares[toIndex] = piecePromoteTo;
            squares[fromIndex] = 0;
        }

        // Если это взятие, то мы делаем то же самое
        // Однако, нам необходимо сохранить фигуру, которую мы взяли, чтобы мы могли отменить ход
        // Для отмены нужна отдельная логика
        // Это еще значит, что в Renderer нам нужно просто выключать фигуры, а не уничтожать их при съедении
        // Чтобы при отмене мы могли просто включить фигуру обратно
        if (move.type == MoveType.Take)
        {
            squares[toIndex] = squares[fromIndex];
            squares[fromIndex] = 0;
        }

        // Если это рокировка, то мы должны определить, это 0-0 или 0-0-0 и кто рокируется
        // А затем выполнить определенную логику рокировки
        // Для отмены рокировки нужна отдельная логика
        if (move.type == MoveType.Castle)
        {
            // Определяем, какая это рокировка
            switch (move.targetSquare)
            {
                case 2:     // W 0-0-0
                    rookCastlingMove = new Move(0, 3, MoveType.Move);
                    break;
                case 6:     // W 0-0
                    rookCastlingMove = new Move(7, 5, MoveType.Move);
                    break;
                case 58:    // B 0-0-0
                    rookCastlingMove = new Move(56, 59, MoveType.Move);
                    break;
                case 62:    // B 0-0
                    rookCastlingMove = new Move(63, 61, MoveType.Move);
                    break;
                default:
                    break;
            }

            squares[toIndex] = squares[fromIndex];
            squares[fromIndex] = 0;

            squares[rookCastlingMove.targetSquare] = squares[rookCastlingMove.startSquare];
            squares[rookCastlingMove.startSquare] = 0;
        }

        // Если это en passant, то мы должны выполнить определенную логику
        // en passant говорит, что кроме движения пешки, мы еще и срубаем другую
        // Для отмены нужна отдельная логика
        if ((move.type == MoveType.EnPassant))
        {
            squares[toIndex] = squares[fromIndex];
            squares[fromIndex] = 0;

            if (Piece.GetColor(pieceToMove) == Piece.White)
                squares[toIndex - 8] = 0;
            else
                squares[toIndex + 8] = 0;
        }

        // Нам тут где-то еще нужно проверять, был ли сделан ход пешкой на две клетки
        // Чтобы выставить enPassantTargetSquare
        // У нас есть fromIndex и toIndex, по сути нужно просто посмотреть разницу rank этих клеток
        if (Piece.GetType(pieceToMove) == Piece.Pawn && 
            Math.Abs(fromIndex / 8 - toIndex / 8) == 2)
        {
            if (Piece.GetColor(pieceToMove) == Piece.White)
            {
                // Если это белая пешка, то клетка en passant будет на 8 индексов ниже
                enPassantTargetSquare = toIndex - 8;
            }
            else
            {
                // Если черная - то на 8 индексов выше
                enPassantTargetSquare = toIndex + 8;
            }
        }

        // А еще, скорее всего в начале функции, сбрасывать enPassantTargetSquare
        // Потому что вне зависимости от того, была ли взята пешка на проходе на предыдущем ходе или нет, больше этого сделать будет уже нельзя
        

        // Вот тут проверяем, если был совершен ход королем, то мы выключаем рокировки у этого цвета
        if (Piece.GetType(pieceToMove) == Piece.King)
        {
            if (Piece.GetColor(pieceToMove) == Piece.White)
            {
                canWhiteCastleKingside = false;
                canWhiteCastleQueenside = false;
            }
            else if (Piece.GetColor(pieceToMove) == Piece.Black)
            {
                canBlackCastleKingside = false;
                canBlackCastleQueenside = false;
            }
        }

        // Обновляем чей ход
        colorToMove = colorToMove ^ 24;

        // Вызвать метод обновления визуала у BoardRenderer
        if (move.type == MoveType.Move || move.type == MoveType.Take)
        {
            BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex);
        }

        // Если это рокировка, то BoardRenderer должен сделать "2 хода"
        if (move.type == MoveType.Castle)
        {
            BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex);
            BoardRenderer.Instance.UpdateBoardAfterAMove(rookCastlingMove.startSquare, rookCastlingMove.targetSquare);
        }
        
        // Если это enPassant, то кроме хода пешкой, съеденная должна ПРОПАСТЬ куда-то
        if (move.type == MoveType.EnPassant)
        {
            Debug.Log("EN PASSANT");
            if (Piece.GetColor(pieceToMove) == Piece.White)
            {
                // Если en passant производит белая пешка, то нужно сходить пустой клеткой на index - 8
                // Пустая клетка сейчас ТОЧНО та, из которой ушла пешка, взявшая на проход
                BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex);
                BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex - 8);
            }
            else if (Piece.GetColor(pieceToMove) == Piece.Black)
            {
                // Черная аналогично
                BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex);
                BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex + 8);
            }
        }

        if (move.type == MoveType.Promote)
        {
            BoardRenderer.Instance.UpdateBoardAfterAMove(fromIndex, toIndex, piecePromoteTo);
        }

    }
}
