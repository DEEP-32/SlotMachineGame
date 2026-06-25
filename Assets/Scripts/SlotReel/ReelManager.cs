using System;
using System.Collections;
using SlotMachine.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace SlotMachine.SlotReel {
    public class ReelManager : MonoBehaviour {
        [FormerlySerializedAs("_reels")]
        [Header("Reel References")]
        [Tooltip("Drag the 3 Reel GameObjects from your scene here (Left to Right).")]
        [SerializeField]
        ReelController[] reels;

        [FormerlySerializedAs("_stopDelayBetweenReels")]
        [Header("Animation Timings")]
        [Tooltip("How long to wait between stopping each reel (creates the classic slot machine tension).")]
        [SerializeField]
        float stopDelayBetweenReels = 0.4f;

        /// <summary>
        /// Bootstraps all the individual reels. Called by GameManager on Awake.
        /// </summary>
        public void InitializeReels(SymbolDatabase database) {
            foreach (var reel in reels) {
                // Initializes each ReelController with the central database
                reel.Initialize(database);
            }
        }

        /// <summary>
        /// Starts the spinning animation for all reels simultaneously.
        /// </summary>
        public void SpinAllReels() {
            foreach (var reel in reels) {
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

            for (int i = 0; i < reels.Length; i++) {
                // Add a suspenseful delay before stopping the 2nd and 3rd reels
                if (i > 0) {
                    yield return new WaitForSeconds(stopDelayBetweenReels);
                }

                // Command the specific reel to stop on its specific symbol from the SpinResult
                reels[i].StopOnSymbol(result.FinalSymbols[i], () => { totalReelsStopped++; });
            }

            // Wait here until every single reel has finished its bounce animation
            yield return new WaitUntil(() => totalReelsStopped == reels.Length);

            // Notify the GameManager that the visual spin sequence is completely over
            onAllReelsStopped?.Invoke();
        }
    }
}