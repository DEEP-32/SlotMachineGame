using SlotMachine.Data;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SlotMachine.SlotReel {
    [RequireComponent(typeof(Image))]
    public class ReelSymbol : MonoBehaviour {
        [SerializeField] Image symbolImage;


        public SymbolType CurrentType { get; private set; }

        /// <summary>
        /// Injects the ScriptableObject data into the UI prefab.
        /// </summary>
        public void SetSymbol(SlotSymbol data) {
            if (data == null) return;

            CurrentType = data.symbolType;

            if (symbolImage != null) {
                symbolImage.sprite = data.symbolSprite;
            }
        }
    }
}