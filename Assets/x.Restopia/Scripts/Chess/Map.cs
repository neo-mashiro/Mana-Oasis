using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace x.Restopia.Scripts.Chess {
    // the helper class that maps a piece's (rank, file) to the string position on board
    // e.g. (4, 6) <=> "4F"
    public static class Map {
        private static readonly Dictionary<int, string> Files = new Dictionary<int, string> {
            [1] = "A", [2] = "B", [3] = "C", [4] = "D",
            [5] = "E", [6] = "F", [7] = "G", [8] = "H"
        };

        private static string GetFile(int index) {
            return Files.TryGetValue(index, out var file) ? file : null;
        }

        private static int GetIndex(string file) {
            return Files.FirstOrDefault(x => x.Value == file).Key;
        }

        public static (int rank, int file) ToIntPosition(string strPosition) {
            var rank = Convert.ToInt32(strPosition.Substring(0, 1));
            var file = GetIndex(strPosition.Substring(1, 1));

            if (!Enumerable.Range(1, 8).Contains(rank) ||
                !Enumerable.Range(1, 8).Contains(file)) {
                throw new IndexOutOfRangeException("Index out of bound!");
            }
            
            return (rank, file);
        }

        public static string ToStrPosition(int rank, int file) {
            if (!Enumerable.Range(1, 8).Contains(rank) ||
                !Enumerable.Range(1, 8).Contains(file)) {
                throw new IndexOutOfRangeException("Index out of bound!");
            }
            
            return rank + GetFile(file);
        }
    }
    
    public static class MapTest {
        // [RuntimeInitializeOnLoadMethod]
        public static void Main() {
            Debug.Log(Map.ToIntPosition("1A"));
            Debug.Log(Map.ToIntPosition("3F"));
            Debug.Log(Map.ToIntPosition("8E"));
            Debug.Log(Map.ToIntPosition("9A"));  // exception
            Debug.Log(Map.ToIntPosition("5Z"));  // exception
            
            Debug.Log(Map.ToStrPosition(1, 2));
            Debug.Log(Map.ToStrPosition(3, 7));
            Debug.Log(Map.ToStrPosition(8, 5));
            Debug.Log(Map.ToStrPosition(9, 1));  // exception
            Debug.Log(Map.ToStrPosition(5, 0));  // exception
        }
    }
}