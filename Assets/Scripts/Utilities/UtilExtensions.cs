using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utilities {
    
    /// <summary>
    /// A static helper class of misc utility functions and C# core extensions.
    /// </summary>
    public static class UtilExtensions {
        
        /// <summary>
        /// Get all possible bit combinations of a specified bit count size, with each bit being a boolean.<br/>
        /// Note: bits are ordered from the lowest bit to the highest bit, for example, GetBitCombos(3)
        /// returns a list of (0,0,0), (1,0,0), (0,1,0), (1,1,0), (0,0,1), (1,0,1), (0,1,1), (1,1,1), where
        /// 0 and 1 are expressed as false and true.
        /// </summary>
        public static IEnumerable<List<bool>> GetBitCombos(int bitCount) {
            // the maximum int value is 2^31 - 1, each int has 32 bits
            if (bitCount > 30) {
                throw new OverflowException("Bit count is out of range!");
            }

            var numberOfCombos = (int) Mathf.Pow(2, bitCount);
            var combos = new List<List<bool>>(numberOfCombos);

            for (var value = 0; value < numberOfCombos; value++) {
                var combo = new List<bool>(bitCount);

                for (var i = 0; i < bitCount; i++) {
                    combo.Add((value & (1 << i)) != 0);
                }

                combos.Add(combo);
            }

            return combos;
        }

        /// <summary>
        /// Get all possible combinations of a specified list of distinct elements (usually numbers or strings).<br/>
        /// For convenience, this extension method is implemented to yield each combination one at a time,
        /// which is intended to be consumed by a foreach loop.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> GetCombos<T>(this IEnumerable<T> list) {
            var elements = list.ToList();
            if (elements.Count == 1) {
                yield return new List<T>(); // always include the empty combo
                yield return elements;
            }
            else {
                var bitCombos = GetBitCombos(elements.Count);
                foreach (var bitCombo in bitCombos) {
                    yield return elements.Where((item, index) => bitCombo[index]).ToList();
                }
            }
        }
    }
}
