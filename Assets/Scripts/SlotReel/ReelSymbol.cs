using SlotMachine.Data;
using UnityEngine;

namespace SlotMachine.SlotReel {
    public class ReelSymbol : MonoBehaviour {
        [SerializeField] private SpriteRenderer _spriteRenderer;
    
        // Public property to read what this symbol currently is
        public SymbolType CurrentType { get; private set; }

        /// <summary>
        /// Injects the ScriptableObject data into the visual prefab.
        /// </summary>
        public void SetSymbol(SlotSymbol data)
        {
            if (data == null) return;
        
            CurrentType = data.symbolType;
        
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = data.symbolSprite;
            }
        }
    }
}