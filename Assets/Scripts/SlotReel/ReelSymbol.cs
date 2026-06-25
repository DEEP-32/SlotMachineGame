using SlotMachine.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace SlotMachine.SlotReel {
    
    [RequireComponent(typeof(SpriteRenderer))]
    
    public class ReelSymbol : MonoBehaviour {
        SpriteRenderer spriteRenderer;
        
        private void Awake() {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    
        // Public property to read what this symbol currently is
        public SymbolType CurrentType { get; private set; }

        /// <summary>
        /// Injects the ScriptableObject data into the visual prefab.
        /// </summary>
        public void SetSymbol(SlotSymbol data)
        {
            if (data == null) return;
        
            CurrentType = data.symbolType;
        
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = data.symbolSprite;
            }
        }
    }
}