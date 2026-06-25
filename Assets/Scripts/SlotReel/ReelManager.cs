using System;
using System.Collections;
using SlotMachine.Data;
using UnityEngine;

namespace SlotMachine.SlotReel {
    public class ReelManager : MonoBehaviour {
        [Header("Reel References")]
        [Tooltip("Drag the 3 UI Reel GameObjects from your Canvas here (Left to Right).")]
        [SerializeField]
        ReelController[] reels;

        [Header("Animation Timings")]
        [Tooltip("How long to wait between stopping each reel (creates the slot machine tension).")]
        [SerializeField]
        float stopDelayBetweenReels = 0.4f;

        /// <summary>
        /// Bootstraps all the individual reels. Called by GameManager on start.
        /// </summary>
        public void InitializeReels(SymbolDatabase database) {
            foreach (var reel in reels) {
                reel.Initialize(database);
            }
        }

        /// <summary>
        /// Starts the spinning animation for all UI reels simultaneously.
        /// </summary>
        public void SpinAllReels() {
            foreach (var reel in reels) {
                reel.StartSpinning();
            }
        }

        /// <summary>
        /// Takes the final spin result and stops the reels sequentially.
        /// </summary>
        public void StopReels(SpinResult result, Action onAllReelsStopped) {
            StartCoroutine(StopReelsRoutine(result, onAllReelsStopped));
        }

        private IEnumerator StopReelsRoutine(SpinResult result, Action onAllReelsStopped) {
            int totalReelsStopped = 0;

            for (int i = 0; i < reels.Length; i++) {
                // Delay before stopping reels 2 and 3
                if (i > 0) {
                    yield return new WaitForSeconds(stopDelayBetweenReels);
                }

                // Command the specific reel to stop on its specific symbol from the math engine
                reels[i].StopOnSymbol(result.FinalSymbols[i], () => { totalReelsStopped++; });
            }

            // Wait here until every single UI container finishes bouncing
            yield return new WaitUntil(() => totalReelsStopped == reels.Length);

            // Notify GameManager the spin is done
            onAllReelsStopped?.Invoke();
        }
    }
}