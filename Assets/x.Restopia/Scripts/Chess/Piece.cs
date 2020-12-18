using System;
using UnityEngine;

namespace x.Restopia.Scripts.Chess {
    
    public enum PieceType { King=900, Queen=90, Rook=50, Bishop=32, Knight=30, Pawn=10 }
    
    public class Piece {
        public PieceType Name { get; private set; }
        public int Rank { get; private set; }
        public int File { get; private set; }
        public string Color { get; }
        
        public int Score {
            get {
                var sign = String.Equals(Color, "White", StringComparison.OrdinalIgnoreCase);
                return (sign ? 1 : -1) * Convert.ToInt32(Name.ToString("d"));
            }
        }
        
        public Piece(int rank, int file, string color, PieceType name = PieceType.Pawn) {
            Name = name;
            Rank = rank;
            File = file;
            Color = color;
        }

        public void Move(int rank, int file) {
            // move to the target
            Rank = rank;
            File = file;
            
            // check pawn promotion
            if (String.Equals(Color, "White") && Name == PieceType.Pawn && rank == 8) {
                Name = PieceType.Queen;
            }
            else if (String.Equals(Color, "Black") && Name == PieceType.Pawn && rank == 1) {
                Name = PieceType.Queen;
            }
        }

        public void Capture(Piece otherPiece) {
            Debug.Log($"{Color} {Name} captured {otherPiece}");
        }
        
        public override string ToString() {
            return $"{Color} {Name} at {Map.ToStrPosition(Rank, File)} = {Score}";
        }
    }
    
    public static class PieceTest {
        // [RuntimeInitializeOnLoadMethod]
        public static void Main() {
            var pawn = new Piece(7, 1, "Black");
            Debug.Log(pawn.ToString());
            pawn.Move(5, 1);
            pawn.Move(4, 2);
            pawn.Move(3, 3);
            pawn.Move(2, 4);
            pawn.Move(1, 4);
            Debug.Log(pawn.ToString());
            
            var knight = new Piece(1, 2, "White", PieceType.Knight);
            Debug.Log(knight.ToString());
            knight.Move(3, 1);
            Debug.Log(knight.ToString());
        }
    }
}