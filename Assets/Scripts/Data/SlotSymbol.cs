using UnityEngine;

namespace SlotMachine.Data {
    
    public enum SymbolType {
        Cherry,
        Bell,
        Seven,
        Bar
    }
    [CreateAssetMenu(fileName = "SlotSymbol", menuName = "Slot Game/Slot Symbol ", order = 0)]
    public class SlotSymbol : ScriptableObject {
        [Header("Visuals")]
        public SymbolType symbolType; // Swapped from string to Enum
        public Sprite symbolSprite;

        [Header("Math & Payouts")]
        public int weight;          
        public int payoutValue;
    }
}