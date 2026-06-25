using SlotMachine.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace SlotMachine.SlotReel {
    public class ReelController : MonoBehaviour {
        enum ReelState {
            Idle,
            Spinning,
            Stopping
        }

        private ReelState _currentState = ReelState.Idle;

        private SymbolDatabase _database;

        [Header("Reel Settings")]
        [SerializeField]
        float spinSpeed = 20f;

        [SerializeField] float symbolSpacing = 2.5f;

        [Tooltip("The Y position where a symbol is completely off-screen at the bottom and needs to wrap to the top.")]
        [SerializeField]
        float bottomThreshold = -5f;

        [Header("Symbol Pool")]
        [Tooltip("The prefab for a single symbol on the reel.")]
        [SerializeField]
        ReelSymbol symbolPrefab;

        [Tooltip("How many symbols to spawn for this reel's pool (usually 5).")]
        [SerializeField]
        int poolSize = 5;

        [Header("Components")]
        [Tooltip("Reference to the decoupled animation component attached to this GameObject.")]
        [SerializeField]
        ReelAnimation reelAnimation;
        
        [Header("Symbol parent")]
        [SerializeField] Transform symbolParent;

        // State Tracking
        List<ReelSymbol> symbolPool = new List<ReelSymbol>();
        Queue<SymbolType> stoppingSequence = new Queue<SymbolType>();
        ReelSymbol targetSymbolInstance;
        Action onReelStopped;

        /// <summary>
        /// Called by the ReelManager on startup to dynamically build the column.
        /// </summary>
        public void Initialize(SymbolDatabase database) {
            _database = database;

            // Clear existing symbols if re-initializing
            foreach (var sym in symbolPool) {
                if (sym != null) Destroy(sym.gameObject);
            }

            symbolPool.Clear();

            // We offset the start index by -1 so the first symbol spawns BELOW the center line,
            // the second spawns IN the center, and the third spawns ABOVE the center.
            int startIndexOffset = -1;

            // Dynamically spawn the pool of symbols
            for (int i = 0; i < poolSize; i++) {
                ReelSymbol newSymbol = Instantiate(symbolPrefab, symbolParent);
                
                // Calculate the Y position with the new offset
                float yPos = (i + startIndexOffset) * symbolSpacing;
                newSymbol.transform.localPosition = new Vector3(0, yPos, 0);

                symbolPool.Add(newSymbol);
                AssignRandomSymbolOffScreen(newSymbol);
            }
        }

        private void Update() {
            if (_currentState == ReelState.Idle) return;

            MoveSymbolsDown();
        }

        // --- PUBLIC API (Called by ReelManager) ---

        public void StartSpinning() {
            _currentState = ReelState.Spinning;
            stoppingSequence.Clear();
            targetSymbolInstance = null;
        }

        /// <summary>
        /// Tells the reel to stop on a specific target symbol.
        /// </summary>
        public void StopOnSymbol(SymbolType targetType, Action onStoppedCallback) {
            onReelStopped = onStoppedCallback;

            // Build the sequence of symbols that will roll in.
            // We want the target to be in the center, so we queue:
            // 1. A random symbol (this will land at the BOTTOM)
            // 2. The TARGET symbol (this will land in the CENTER)
            // 3. A random symbol (this will land at the TOP)

            stoppingSequence.Enqueue(GetRandomSymbolType()); // Bottom
            stoppingSequence.Enqueue(targetType); // Center Target
            stoppingSequence.Enqueue(GetRandomSymbolType()); // Top

            _currentState = ReelState.Stopping;
        }

        // --- INTERNAL LOGIC ---

        private void MoveSymbolsDown() {
            foreach (var symbol in symbolPool) {
                symbol.transform.localPosition += Vector3.down * (spinSpeed * Time.deltaTime);
                
                if (symbol.transform.localPosition.y <= bottomThreshold) {
                    WrapSymbolToTop(symbol);
                }
                
                if (_currentState == ReelState.Stopping && symbol == targetSymbolInstance) {
                    if (symbol.transform.localPosition.y <= 0f) {
                        SnapAndBounce();
                    }
                }
            }
        }

        private void WrapSymbolToTop(ReelSymbol symbol) {
            // Because our math dynamically calculates topY based on spacing and pool size, 
            // the offset we added in Initialize won't break the looping wrap logic!
            float topY = symbol.transform.localPosition.y + (symbolPool.Count * symbolSpacing);
            symbol.transform.localPosition = new Vector3(0, topY, 0);
            
            if (_currentState == ReelState.Stopping && stoppingSequence.Count > 0) {
                SymbolType nextTypeInSequence = stoppingSequence.Dequeue();
                symbol.SetSymbol(_database.GetSymbolData(nextTypeInSequence));
                
                if (stoppingSequence.Count == 1) {
                    targetSymbolInstance = symbol;
                }
            }
            else {
                AssignRandomSymbolOffScreen(symbol);
            }
        }

        private void AssignRandomSymbolOffScreen(ReelSymbol symbol) {
            if (_database == null) return;
            SymbolType randomType = GetRandomSymbolType();
            symbol.SetSymbol(_database.GetSymbolData(randomType));
        }

        private SymbolType GetRandomSymbolType() {
            var allSymbols = _database.GetAllSymbols();
            int randomIndex = Random.Range(0, allSymbols.Count);
            return allSymbols[randomIndex].symbolType;
        }

        // --- GAME FEEL (ANIMATION) ---

        private void SnapAndBounce() {
            _currentState = ReelState.Idle; // Stop Update() loop

            float overshoot = targetSymbolInstance.transform.localPosition.y;

            foreach (var symbol in symbolPool) {
                symbol.transform.localPosition -= new Vector3(0, overshoot, 0);
            }

            if (reelAnimation != null) {
                reelAnimation.PlaySnapAndBounce(transform.localPosition, () => {
                    onReelStopped?.Invoke();
                });
            }
            else {
                onReelStopped?.Invoke();
            }
        }
    }
}