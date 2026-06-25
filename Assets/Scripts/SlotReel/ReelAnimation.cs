using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace SlotMachine.SlotReel {
    public class ReelAnimation : MonoBehaviour {
        [Header("Bounce Settings")]
        [Tooltip("How far down the reel pulls before springing back. UI pixels are larger than World Units, so try -40 or -50.")]
        [SerializeField]
        private float bounceDepth = -40f;

        [Tooltip("How fast it hits the bottom.")]
        [SerializeField]
        private float bounceTime = 0.1f;

        [Tooltip("How fast it springs back to center.")]
        [SerializeField]
        private float recoveryTime = 0.15f;

        // We now pass the specific target we want to animate (the SymbolParent)
        public void PlaySnapAndBounce(RectTransform targetRect, Vector2 originalPosition, Action onComplete) {
            StartCoroutine(BounceRoutine(targetRect, originalPosition, onComplete));
        }

        private IEnumerator BounceRoutine(RectTransform targetRect, Vector2 startPos, Action onComplete) {
            // 1. The Dip (Physical Weight)
            float elapsed = 0f;
            Vector2 bottomPos = startPos + new Vector2(0, bounceDepth);

            while (elapsed < bounceTime) {
                targetRect.anchoredPosition = Vector2.Lerp(startPos, bottomPos, elapsed / bounceTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 2. The Spring Up (Recovery)
            elapsed = 0f;
            while (elapsed < recoveryTime) {
                float t = Mathf.SmoothStep(0, 1, elapsed / recoveryTime);
                targetRect.anchoredPosition = Vector2.Lerp(bottomPos, startPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Guarantee perfect alignment at the end
            targetRect.anchoredPosition = startPos;

            onComplete?.Invoke();
        }
    }
}