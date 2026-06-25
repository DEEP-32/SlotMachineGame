using System;
using System.Collections;
using UnityEngine;

namespace SlotMachine.SlotReel {
    public class ReelAnimation : MonoBehaviour {
        [Header("Bounce Settings")]
        [Tooltip("How far down the reel pulls before springing back. UI pixels are larger than World Units, so try -40 or -50.")]
        [SerializeField]
        private float _bounceDepth = -40f;

        [Tooltip("How fast it hits the bottom.")]
        [SerializeField]
        private float _bounceTime = 0.1f;

        [Tooltip("How fast it springs back to center.")]
        [SerializeField]
        private float _recoveryTime = 0.15f;

        // Note: For this UI setup, ReelAnimation must be attached to the SymbolContainer object
        private RectTransform _rectTransform;

        private void Awake() {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void PlaySnapAndBounce(Vector2 originalPosition, Action onComplete) {
            StartCoroutine(BounceRoutine(originalPosition, onComplete));
        }

        private IEnumerator BounceRoutine(Vector2 startPos, Action onComplete) {
            // 1. The Dip (Physical Weight)
            float elapsed = 0f;
            Vector2 bottomPos = startPos + new Vector2(0, _bounceDepth);

            while (elapsed < _bounceTime) {
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, bottomPos, elapsed / _bounceTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 2. The Spring Up (Recovery)
            elapsed = 0f;
            while (elapsed < _recoveryTime) {
                float t = Mathf.SmoothStep(0, 1, elapsed / _recoveryTime);
                _rectTransform.anchoredPosition = Vector2.Lerp(bottomPos, startPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Guarantee perfect alignment at the end
            _rectTransform.anchoredPosition = startPos;

            onComplete?.Invoke();
        }
    }
}