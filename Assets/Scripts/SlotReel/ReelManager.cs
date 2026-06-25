using System;
using System.Collections;
using SlotMachine.Data;
using UnityEngine;

namespace SlotMachine.SlotReel {
    public class ReelManager : MonoBehaviour {
        [Header("Reel References")]
        [Tooltip("Drag the 3 Reel GameObjects from your scene here (Left to Right).")]
        [SerializeField]
        private ReelController[] _reels;

        [Header("Animation Timings")]
        [Tooltip("How long to wait between stopping each reel (creates the classic slot machine tension).")]
        [SerializeField]
        private float _stopDelayBetweenReels = 0.4f;

        /// <summary>
        /// Bootstraps all the individual reels. Called by GameManager on Awake.
        /// </summary>
        public void InitializeReels(SymbolDatabase database) {
            foreach (var reel in _reels) {
                // Initializes each ReelController with the central database
                reel.Initialize(database);
            }
        }

        /// <summary>
        /// Starts the spinning animation for all reels simultaneously.
        /// </summary>
        public void SpinAllReels() {
            foreach (var reel in _reels) {
                reel.StartSpinning();
            }
        }

        /// <summary>
        /// Takes the final spin result and stops the reels one by one in a staggered sequence.
        /// </summary>
        public void StopReels(SpinResult result, Action onAllReelsStopped) {
            StartCoroutine(StopReelsRoutine(result, onAllReelsStopped));
        }

        private IEnumerator StopReelsRoutine(SpinResult result, Action onAllReelsStopped) {
            int totalReelsStopped = 0;

            for (int i = 0; i < _reels.Length; i++) {
                // Add a suspenseful delay before stopping the 2nd and 3rd reels
                if (i > 0) {
                    yield return new WaitForSeconds(_stopDelayBetweenReels);
                }

                // Command the specific reel to stop on its specific symbol from the SpinResult
                _reels[i].StopOnSymbol(result.FinalSymbols[i], () => { totalReelsStopped++; });
            }

            // Wait here until every single reel has finished its bounce animation
            yield return new WaitUntil(() => totalReelsStopped == _reels.Length);

            // Notify the GameManager that the visual spin sequence is completely over
            onAllReelsStopped?.Invoke();
        }
    }
}