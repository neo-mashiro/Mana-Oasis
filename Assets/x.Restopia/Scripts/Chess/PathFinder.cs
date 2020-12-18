using System;
using System.Collections.Generic;

namespace x.Restopia.Scripts.Chess {
    // the helper class that finds path for a piece given any chessboard context
    public static class PathFinder {
        // to check flags by bit, each enum member must have a different bit set to 1
        // that is, the value of each enum member needs to be a power of 2 (since data is stored in binary format)
        [Flags] private enum Direction {
            Up = 1, Down = 2, Left = 4, Right = 8,
            Southwest = 16, Southeast = 32, Northwest = 64, Northeast = 128
        }

        // apply bitwise "or" operator | on multiple directions to create a directions category
        // e.g. RankUp = 0b_0000_0001 | 0b_0100_0000 | 0b_1000_0000 = 0b_1100_0001
        private const Direction RankUp = Direction.Up | Direction.Northwest | Direction.Northeast;
        private const Direction FileUp = Direction.Right | Direction.Southeast | Direction.Northeast;
        private const Direction RankDown = Direction.Down | Direction.Southwest | Direction.Southeast;
        private const Direction FileDown = Direction.Left | Direction.Northwest | Direction.Southwest;
        
        // given a piece, find its orthogonal distances to the chessboard border
        private static (int l, int r, int u, int d) OrthogonalDistance(Piece p) {
            var l = p.File - 1;  // left
            var r = 8 - p.File;  // right
            var u = 8 - p.Rank;  // up
            var d = p.Rank - 1;  // down
            
            return (l, r, u, d);
        }
        
        // given a piece, find its diagonal distances to the chessboard border
        // note: distance = the number of diagonal cells away from the border (not Euclidean)
        private static (int sw, int ne, int nw, int se) DiagonalDistance(Piece p) {
            var (l, r, u, d) = OrthogonalDistance(p);
            
            var sw = Math.Min(l, d);  // southwest
            var ne = Math.Min(r, u);  // northeast
            var nw = Math.Min(u, l);  // northwest
            var se = Math.Min(d, r);  // southeast
            
            return (sw, ne, nw, se);
        }
        
        // retrieve all reachable moves on the line along the given direction
        private static void FindPath(ChessBoard board, Piece p, int depth,
            Direction dir, ref List<(int rank, int file)> moves) {
            
            (int, int) Walk(int step) => (
                p.Rank + (RankUp.HasFlag(dir) ? 1 : 0) * step - (RankDown.HasFlag(dir) ? 1 : 0) * step,
                p.File + (FileUp.HasFlag(dir) ? 1 : 0) * step - (FileDown.HasFlag(dir) ? 1 : 0) * step);
            
            for (var i = 1; i <= depth; i++) {
                var (rank, file) = Walk(i);
                
                if (board[rank, file] == null) {
                    moves.Add((rank, file));  // move to an empty cell
                }
                else {
                    if (board[rank, file].Color != p.Color) {
                        moves.Add((rank, file));  // capture enemy in the cell
                    }
                    break;  // cells beyond this one are not reachable
                }
            }
        }

        // find the list of reachable cells orthogonal to the given piece
        public static IEnumerable<(int rank, int file)> FindOrthogonal(ChessBoard board, Piece p) {
            var (l, r, u, d) = OrthogonalDistance(p);
            var feasibleMoves = new List<(int rank, int file)>();

            FindPath(board, p, l, Direction.Left, ref feasibleMoves);
            FindPath(board, p, r, Direction.Right, ref feasibleMoves);
            FindPath(board, p, u, Direction.Up, ref feasibleMoves);
            FindPath(board, p, d, Direction.Down, ref feasibleMoves);

            return feasibleMoves;
        }

        // find the list of reachable cells diagonal to the given piece
        public static IEnumerable<(int rank, int file)> FindDiagonal(ChessBoard board, Piece p) {
            var (sw, ne, nw, se) = DiagonalDistance(p);
            var feasibleMoves = new List<(int rank, int file)>();

            FindPath(board, p, sw, Direction.Southwest, ref feasibleMoves);
            FindPath(board, p, ne, Direction.Northeast, ref feasibleMoves);
            FindPath(board, p, nw, Direction.Northwest, ref feasibleMoves);
            FindPath(board, p, se, Direction.Southeast, ref feasibleMoves);

            return feasibleMoves;
        }
    }
}