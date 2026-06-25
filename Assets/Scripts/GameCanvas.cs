using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine {
    public class GameCanvas : MonoBehaviour {
        [SerializeField] Image normalHandle;
        [SerializeField] Image activeHandle;

        public void ToggleHandle(bool isActive) {
            if (!isActive) {
                normalHandle.enabled = true;
                activeHandle.enabled = false;
            }
            else {
                normalHandle.enabled = false;
                activeHandle.enabled = true;
            }
        }
    }
}