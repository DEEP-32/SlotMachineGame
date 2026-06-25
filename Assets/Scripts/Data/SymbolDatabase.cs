using System.Collections.Generic;
using UnityEngine;

namespace SlotMachine.Data {
    [CreateAssetMenu(fileName = "SymbolDatabase", menuName = "Slot Game/Symbol Database")]
    public class SymbolDatabase : ScriptableObject {
        [Tooltip("Drag all your SlotSymbol assets here in the Inspector.")]
        [SerializeField]
        private List<SlotSymbol> _allSymbols;

        // Internal dictionary for rapid lookups
        private Dictionary<SymbolType, SlotSymbol> _symbolDict;

        /// <summary>
        /// Builds the dictionary. Call this once at the very start of your game.
        /// </summary>
        public void Initialize() {
            _symbolDict = new Dictionary<SymbolType, SlotSymbol>();

            foreach (var symbol in _allSymbols) {
                if (!_symbolDict.ContainsKey(symbol.symbolType)) {
                    _symbolDict.Add(symbol.symbolType, symbol);
                }
                else {
                    Debug.LogError($"Database Error: Duplicate symbol type '{symbol.symbolType}' found!");
                }
            }
        }

        /// <summary>
        /// Fetch a symbol's full data using only its Enum key.
        /// </summary>
        public SlotSymbol GetSymbolData(SymbolType type) {
            if (_symbolDict == null) {
                Debug.LogWarning("Database was not initialized! Initializing now...");
                Initialize();
            }

            if (_symbolDict.TryGetValue(type, out SlotSymbol symbolData)) {
                return symbolData;
            }

            Debug.LogError($"Symbol {type} does not exist in the Database!");
            return null;
        }

        // Expose the raw list so the Math Engine can calculate total weights
        public List<SlotSymbol> GetAllSymbols() => _allSymbols;
    }
}