using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace x.Restopia.Scripts.Chess {

    public class ChessBoard : IEnumerable {
        private readonly PieceType[] _startPosition = {
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
        };

        private Piece[,] Cells { get; set; }
        private int Score { get; set; } = 0;
        public bool GameOver { get; private set; }
        public string Winner { get; private set; }
        public int TotalMoves { get; private set; } = 0;

        // constructor
        public ChessBoard() { Reset(); }
        
        public void Reset() {
            Debug.Log("Initializing chessboard ......");
            Cells = new Piece[8, 8];

            // populate cells with instantiated pieces
            for (var col = 0; col < 8; col++) {
                Cells[6, col] = new Piece(7, col + 1, "Black");  // black pawns
                Cells[1, col] = new Piece(2, col + 1, "White");  // white pawns
                Cells[7, col] = new Piece(8, col + 1, "Black", _startPosition[col]);
                Cells[0, col] = new Piece(1, col + 1, "White", _startPosition[col]);
            }
            
            Score = 0;
            TotalMoves = 0;
            GameOver = false;
            Winner = "";
        }

        // indexer (string)
        public Piece this[string cell] {
            get {
                try {
                    var (rank, file) = Map.ToIntPosition(cell);
                    return Cells[rank - 1, file - 1];  // array index is 0-based
                }
                catch (IndexOutOfRangeException e) {
                    Debug.Log($"Invalid cell entry \"{cell}\": {e.Message}");
                    return null;
                }
            }
            private set {
                try {
                    var (rank, file) = Map.ToIntPosition(cell);
                    Cells[rank - 1, file - 1] = value;
                }
                catch (IndexOutOfRangeException e) {
                    Debug.Log($"Invalid cell entry \"{cell}\": {e.Message}");
                }
            }
        }
        
        // indexer (rank, file)
        public Piece this[int rank, int file] {
            get {
                try { return Cells[rank - 1, file - 1]; }
                catch (IndexOutOfRangeException) {
                    return null;
                }
            }
            set {
                try { Cells[rank - 1, file - 1] = value; }
                catch (IndexOutOfRangeException e) {
                    Debug.Log($"Invalid cell entry ({rank}, {file}): {e.Message}");
                }
            }
        }
        
        // enumerator
        public IEnumerator GetEnumerator() {
            for (var row = 0; row < Cells.GetLength(0); row++) {
                for (var col = 0; col < Cells.GetLength(1); col++) { 
                    yield return Cells[row, col];
                }
            }
        }

        // find all legal moves for a given cell (e.g. "3B", "7H") based on the board context
        public List<string> GetLegalMoves(string cell) {
            var p = this[cell];
            
            if (p == null) {  // empty cell
                return new List<string>();
            }

            var legalCells = new List<string>();  // string representation, easier to link to the game object
            var legalMoves = new List<(int rank, int file)>();  // int representation, easier to manipulate in-class
            
            switch (p.Name) {
                case PieceType.King:
                    (int rank, int file)[] neighbors = {
                        (p.Rank - 1, p.File - 1), (p.Rank - 1, p.File), (p.Rank - 1, p.File + 1), (p.Rank, p.File - 1),
                        (p.Rank + 1, p.File - 1), (p.Rank + 1, p.File), (p.Rank + 1, p.File + 1), (p.Rank, p.File + 1)
                    };
                    
                    legalMoves.AddRange(neighbors.Where(move =>
                        Enumerable.Range(1, 8).Contains(move.rank) && 
                        Enumerable.Range(1, 8).Contains(move.file) &&
                        (this[move.rank, move.file] is null || this[move.rank, move.file].Color != p.Color))
                    );
                    break;
                
                case PieceType.Bishop:
                    legalMoves.AddRange(PathFinder.FindDiagonal(this, p));
                    break;
                
                case PieceType.Knight:
                    (int rank, int file)[] possibleMoves = {
                        (p.Rank - 2, p.File + 1), (p.Rank + 2, p.File + 1), (p.Rank + 1, p.File - 2), (p.Rank + 1, p.File + 2),
                        (p.Rank - 2, p.File - 1), (p.Rank + 2, p.File - 1), (p.Rank - 1, p.File - 2), (p.Rank - 1, p.File + 2)
                    };

                    legalMoves.AddRange(
                        possibleMoves.Where(move =>
                            Enumerable.Range(1, 8).Contains(move.rank) && 
                            Enumerable.Range(1, 8).Contains(move.file) &&
                            (this[move.rank, move.file] is null || this[move.rank, move.file].Color != p.Color)));
                    break;
                
                case PieceType.Rook:
                    legalMoves.AddRange(PathFinder.FindOrthogonal(this, p));
                    break;
                
                case PieceType.Queen:
                    legalMoves.AddRange(PathFinder.FindDiagonal(this, p));
                    legalMoves.AddRange(PathFinder.FindOrthogonal(this, p));
                    break;
                
                case PieceType.Pawn:
                    if (String.Equals(p.Color, "White")) {
                        if (this[p.Rank + 1, p.File] == null) {
                            legalMoves.Add((p.Rank + 1, p.File));  // forward 1 cell
                            
                            if (this[p.Rank + 2, p.File] == null && p.Rank == 2) {
                                legalMoves.Add((p.Rank + 2, p.File));  // initial move can forward 2 cells
                            }
                        }

                        legalMoves.AddRange(
                            from file in new[] {p.File - 1, p.File + 1}
                            where this[p.Rank + 1, file] != null &&
                                  this[p.Rank + 1, file].Color != p.Color
                            select (p.Rank + 1, file));
                    }
                    
                    else if (String.Equals(p.Color, "Black")) {
                        if (this[p.Rank - 1, p.File] == null) {
                            legalMoves.Add((p.Rank - 1, p.File));  // backward 1 cell
                            
                            if (this[p.Rank - 2, p.File] == null && p.Rank == 7) {
                                legalMoves.Add((p.Rank - 2, p.File));  // initial move can backward 2 cells
                            }
                        }

                        legalMoves.AddRange(
                            from file in new[] {p.File - 1, p.File + 1}
                            where this[p.Rank - 1, file] != null &&
                                  this[p.Rank - 1, file].Color != p.Color
                            select (p.Rank - 1, file));
                    }
                    
                    break;
                
                default:
                    // unlike throwing exceptions, this won't crash gameplay
                    Debug.LogException(new ArgumentOutOfRangeException());
                    break;
            }

            // finally, check boundaries, convert to string representation and output
            bool BoundaryCheck((int rank, int file) move)
                => Enumerable.Range(1, 8).Contains(move.rank) &&
                   Enumerable.Range(1, 8).Contains(move.file);

            foreach (var move in legalMoves.Where(BoundaryCheck)) {
                var (rank, file) = move;
                legalCells.Add(Map.ToStrPosition(rank, file));
            }

            return legalCells;
        }

        /* before making the move, we can do a last sanity check to ensure that it's safe,
         * but this is very expensive when the Minimax search is deep even with alpha-beta
         * pruning, for this reason, the validation code block is commented out.
         *
         * the caller on the UI side (such as Unity) must do this check for us, it's easy
         * to do so by calling GetLegalMoves() first to get all legal moves. if the player
         * attempts to make an illegal move, Move() must not be invoked. the Minimax AI is
         * guaranteed to always simulate a legal move by checking first, so as long as the
         * UI does not misbehave with flawed method calls, data will not be corrupted.
        */
        public void Move(string source, string target) {
            // if (!GetLegalMoves(source).Contains(target)) {
            //     Debug.LogWarning($"Invalid move from {source} to {target}!");
            //     return;
            // }

            var sourcePiece = this[source];
            var targetPiece = this[target];

            if (sourcePiece == null) {
                return;
            }
            
            // if the target cell is not empty, capture (destroy) the enemy piece in that cell
            if (targetPiece != null) {
                sourcePiece.Capture(targetPiece);
                this[target] = null;
                Score -= targetPiece.Score;

                // since this is a 3D game, the checkmate method is not called until the king
                // is knocked down (death animation), which differs from the normal checkmate.
                if (targetPiece.Name == PieceType.King) {
                    Checkmate(targetPiece);
                }
            }

            // now the cell is clean so we can safely move and update
            this[target] = sourcePiece;
            this[source] = null;
            var (rank, file) = Map.ToIntPosition(target);
            sourcePiece.Move(rank, file);

            TotalMoves++;
        }

        public void Resign(string player) {
            Debug.Log($"{player} surrendered, Game Over!");
            Winner = player == "Black" ? "White": "Black";
            GameOver = true;
        }
        
        private void Checkmate(Piece king) {
            Debug.Log($"{king.Color} King is dead, Game Over!");
            Winner = king.Color == "Black" ? "White": "Black";
            GameOver = true;
        }

        // once we made a move, AI decides his move and return the move so that UI knows what to animate next
        public (string source, string target) MinimaxAI() {
            return ("7G", "5G");
        }
    }
    
    public static class ChessBoardTest {
        // uncomment the line below to run the test code in Unity
        // [RuntimeInitializeOnLoadMethod]
        public static void Main() {
            var board = new ChessBoard();

            foreach (var cell in board) {
                Debug.Log(cell);
            }
            
            try {
                Debug.Log(board[7, 6]);
                Debug.Log(board[4, 5]);
                Debug.Log(board["1D"]);
                Debug.Log(board["9C"]);
            }
            catch (IndexOutOfRangeException e) {
                Debug.Log(e.Message);
            }

            var message = new StringBuilder();
            
            foreach (var cell in new string[] {"8B", "2D", "1E", "1G"}) {
                message.Append($"Available moves for {cell}: ");
                
                var moves = board.GetLegalMoves(cell);
                var moveString = moves.Any() ? String.Join(", ", moves) : "no legal moves!";
                
                message.Append(moveString);
                Debug.Log(message);
                message.Clear();
            }
            
            board.Move("2C", "4C");
            board.Move("8B", "6C");
            
            try {
                board.Move("2C", "4C");
                board.Move("1H", "3X");
            }
            catch (IndexOutOfRangeException e) {
                Debug.Log(e.Message);
            }
            
            foreach (var cell in new string[] {"7C", "4B", "1C", "8A", "1D"}) {
                message.Append($"Available moves for {cell}: ");
                
                var moves = board.GetLegalMoves(cell);
                var moveString = moves.Any() ? String.Join(", ", moves) : "no legal moves!";
                
                message.Append(moveString);
                Debug.Log(message);
                message.Clear();
            }
            
            board.Move("2D", "4D");
            board.Move("7E", "5E");
            
            foreach (var cell in new string[] {"6C", "8B", "1C", "1E", "1D", "4D", "7E"}) {
                message.Append($"Available moves for {cell}: ");
                
                var moves = board.GetLegalMoves(cell);
                var moveString = moves.Any() ? String.Join(", ", moves) : "no legal moves!";
                
                message.Append(moveString);
                Debug.Log(message);
                message.Clear();
            }
            
            // board.Move("6C", "1E");
            
            board.Resign("White");
            board.Reset();
        }
    }
}