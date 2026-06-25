using System;
using System.Collections;
using UnityEngine;
namespace SlotMachine.SlotReel {
    public class ReelAnimation : MonoBehaviour
    {
        [Header("Bounce Settings")]
        [Tooltip("How far down the reel pulls before springing back.")]
        [SerializeField] private float _bounceDepth = -0.4f;
        [Tooltip("How fast it hits the bottom.")]
        [SerializeField] private float _bounceTime = 0.1f;
        [Tooltip("How fast it springs back to center.")]
        [SerializeField] private float _recoveryTime = 0.15f;

        /// <summary>
        /// Plays the physical dip and spring animation, then fires a callback.
        /// </summary>
        public void PlaySnapAndBounce(Vector3 originalPosition, Action onComplete)
        {
            StartCoroutine(BounceRoutine(originalPosition, onComplete));
        }

        private IEnumerator BounceRoutine(Vector3 startPos, Action onComplete)
        {
            // 1. The Dip (Overshoot)
            float elapsed = 0f;
            Vector3 bottomPos = startPos + new Vector3(0, _bounceDepth, 0);

            while (elapsed < _bounceTime)
            {
                transform.localPosition = Vector3.Lerp(startPos, bottomPos, elapsed / _bounceTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 2. The Spring Up (Recovery)
            elapsed = 0f;
            while (elapsed < _recoveryTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / _recoveryTime);
                transform.localPosition = Vector3.Lerp(bottomPos, startPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Guarantee perfect alignment at the end
            transform.localPosition = startPos;
        
            // Notify the controller that the animation is completely done
            onComplete?.Invoke();
        }
    }
}