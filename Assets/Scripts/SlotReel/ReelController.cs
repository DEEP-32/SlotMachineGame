using SlotMachine.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SlotMachine.SlotReel {
    public class ReelController : MonoBehaviour {
        private enum ReelState {
            Idle,
            Spinning,
            Stopping
        }

        private ReelState _currentState = ReelState.Idle;

        private SymbolDatabase _database;

        [Header("Reel Settings")]
        [SerializeField]
        private float _spinSpeed = 20f;

        [SerializeField] private float _symbolSpacing = 2.5f;

        [Tooltip("The Y position where a symbol is completely off-screen at the bottom and needs to wrap to the top.")]
        [SerializeField]
        private float _bottomThreshold = -5f;

        [Header("Symbol Pool")]
        [Tooltip("The prefab for a single symbol on the reel.")]
        [SerializeField]
        private ReelSymbol _symbolPrefab;

        [Tooltip("How many symbols to spawn for this reel's pool (usually 5).")]
        [SerializeField]
        private int _poolSize = 5;

        [Header("Components")]
        [Tooltip("Reference to the decoupled animation component attached to this GameObject.")]
        [SerializeField]
        private ReelAnimation _reelAnimation;

        // State Tracking
        private List<ReelSymbol> _symbolPool = new List<ReelSymbol>();
        private Queue<SymbolType> _stoppingSequence = new Queue<SymbolType>();
        private ReelSymbol _targetSymbolInstance;
        private Action _onReelStopped;

        /// <summary>
        /// Called by the ReelManager on startup to dynamically build the column.
        /// </summary>
        public void Initialize(SymbolDatabase database) {
            _database = database;

            // Clear existing symbols if re-initializing
            foreach (var sym in _symbolPool) {
                if (sym != null) Destroy(sym.gameObject);
            }

            _symbolPool.Clear();

            // Dynamically spawn the pool of symbols
            for (int i = 0; i < _poolSize; i++) {
                ReelSymbol newSymbol = Instantiate(_symbolPrefab, transform);
                newSymbol.transform.localPosition = new Vector3(0, i * _symbolSpacing, 0);

                _symbolPool.Add(newSymbol);
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
            _stoppingSequence.Clear();
            _targetSymbolInstance = null;
        }

        /// <summary>
        /// Tells the reel to stop on a specific target symbol.
        /// </summary>
        public void StopOnSymbol(SymbolType targetType, Action onStoppedCallback) {
            _onReelStopped = onStoppedCallback;

            // Build the sequence of symbols that will roll in.
            // We want the target to be in the center, so we queue:
            // 1. A random symbol (this will land at the BOTTOM)
            // 2. The TARGET symbol (this will land in the CENTER)
            // 3. A random symbol (this will land at the TOP)

            _stoppingSequence.Enqueue(GetRandomSymbolType()); // Bottom
            _stoppingSequence.Enqueue(targetType); // Center Target
            _stoppingSequence.Enqueue(GetRandomSymbolType()); // Top

            _currentState = ReelState.Stopping;
        }

        // --- INTERNAL LOGIC ---

        private void MoveSymbolsDown() {
            foreach (var symbol in _symbolPool) {
                // Move downwards
                symbol.transform.localPosition += Vector3.down * (_spinSpeed * Time.deltaTime);

                // Check if symbol has fallen completely off-screen at the bottom
                if (symbol.transform.localPosition.y <= _bottomThreshold) {
                    WrapSymbolToTop(symbol);
                }

                // If we are stopping, watch the target symbol closely!
                if (_currentState == ReelState.Stopping && symbol == _targetSymbolInstance) {
                    // Once the target symbol hits the exact center (Y <= 0), SNAP AND STOP!
                    if (symbol.transform.localPosition.y <= 0f) {
                        SnapAndBounce();
                    }
                }
            }
        }

        private void WrapSymbolToTop(ReelSymbol symbol) {
            // Teleport to the top of the column (completely off-screen)
            float topY = symbol.transform.localPosition.y + (_symbolPool.Count * _symbolSpacing);
            symbol.transform.localPosition = new Vector3(0, topY, 0);

            // If we are in the stopping phase, pull from our predefined sequence
            if (_currentState == ReelState.Stopping && _stoppingSequence.Count > 0) {
                SymbolType nextTypeInSequence = _stoppingSequence.Dequeue();
                symbol.SetSymbol(_database.GetSymbolData(nextTypeInSequence));

                // If this was our center target, flag it so Update() knows to stop on it
                if (_stoppingSequence.Count == 1) {
                    _targetSymbolInstance = symbol;
                }
            }
            else {
                // Normal spinning behavior: just grab a random symbol
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

            // 1. Calculate how far past zero the symbol went, and snap the whole reel back to perfect grid alignment
            float overshoot = _targetSymbolInstance.transform.localPosition.y;

            foreach (var symbol in _symbolPool) {
                symbol.transform.localPosition -= new Vector3(0, overshoot, 0);
            }

            // 2. Play the physical bounce animation via our separate component
            if (_reelAnimation != null) {
                _reelAnimation.PlaySnapAndBounce(transform.localPosition, () => {
                    // 3. Tell the ReelManager this reel is completely done!
                    _onReelStopped?.Invoke();
                });
            }
            else {
                // Fallback just in case the component is missing
                _onReelStopped?.Invoke();
            }
        }
    }
}