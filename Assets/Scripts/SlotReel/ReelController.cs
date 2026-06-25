using SlotMachine.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace SlotMachine.SlotReel {
    public class ReelController : MonoBehaviour {
        
        private struct SpinSequenceItem {
            public SymbolType Type;
            public bool IsTarget;
        }
        private enum ReelState { Idle, Spinning, Stopping }

        private ReelState _currentState = ReelState.Idle;
        private SymbolDatabase _database;

        [Header("UI Reel Settings")]
        [Tooltip("Speed in UI Pixels per second (e.g., 2000 - 3000)")]
        [SerializeField]
        float spinSpeed = 2500f; 

        [Tooltip("This is now AUTO-CALCULATED on start based on your Layout Group!")]
        [SerializeField] 
        float symbolSpacing = 250f; 

        [Header("Symbol Pool")]
        [Tooltip("The UI prefab for a single symbol on the reel.")]
        [SerializeField]
        ReelSymbol symbolPrefab;

        [Tooltip("The parent UI object that HAS the Grid Layout Group attached.")]
        [SerializeField]
        RectTransform symbolContainer;

        [Tooltip("Must be an ODD number (e.g., 5 or 7). This ensures there is a strict center symbol.")]
        [SerializeField]
        int poolSize = 5;

        [Header("Components")]
        [Tooltip("Reference to the animation component attached anywhere on this reel.")]
        [SerializeField]
        ReelAnimation reelAnimation;
        
        // State Tracking
        List<ReelSymbol> symbolPool = new List<ReelSymbol>();
        Queue<SpinSequenceItem> stoppingSequence = new Queue<SpinSequenceItem>();
        ReelSymbol targetSymbolInstance;
        Action onReelStopped;

        public void Initialize(SymbolDatabase database) {
            _database = database;

            // Force perfectly centered layout so symbols exist above AND below the center line
            symbolContainer.anchorMin = new Vector2(0.5f, 0.5f);
            symbolContainer.anchorMax = new Vector2(0.5f, 0.5f);
            symbolContainer.pivot = new Vector2(0.5f, 0.5f);
            symbolContainer.anchoredPosition = Vector2.zero;

            // Auto-Calculate the exact spacing from your Layout Group!
            GridLayoutGroup gridLayout = symbolContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null) {
                gridLayout.childAlignment = TextAnchor.MiddleCenter; 
                symbolSpacing = gridLayout.cellSize.y + gridLayout.spacing.y;
                Debug.Log($"Calculated Symbol Spacing: {symbolSpacing}");
            } else {
                VerticalLayoutGroup vLayout = symbolContainer.GetComponent<VerticalLayoutGroup>();
                if (vLayout != null) {
                    vLayout.childAlignment = TextAnchor.MiddleCenter;
                    RectTransform prefabRect = symbolPrefab.GetComponent<RectTransform>();
                    symbolSpacing = prefabRect.rect.height + vLayout.spacing;
                }
            }

            // Clear existing symbols from the UI container
            foreach (Transform child in symbolContainer) {
                Destroy(child.gameObject);
            }
            symbolPool.Clear();

            // Dynamically spawn the pool of symbols into the Layout Group
            for (int i = 0; i < poolSize; i++) {
                ReelSymbol newSymbol = Instantiate(symbolPrefab, symbolContainer);
                symbolPool.Add(newSymbol);
                AssignRandomSymbolOffScreen(newSymbol);
            }

            // Force the grid to calculate positions immediately
            LayoutRebuilder.ForceRebuildLayoutImmediate(symbolContainer);
        }

        private void Update() {
            if (_currentState == ReelState.Idle) return;
            MoveContainerDown();
        }

        public void StartSpinning() {
            _currentState = ReelState.Spinning;
            stoppingSequence.Clear();
            targetSymbolInstance = null;
        }

        public void StopOnSymbol(SymbolType targetType, Action onStoppedCallback) {
            onReelStopped = onStoppedCallback;
            stoppingSequence.Clear();

            // Mathematically find the exact center of our pool (For 5, it's 2. For 7, it's 3).
            int targetIndexInSequence = poolSize / 2;

            // We generate a full column sequence to ensure the target gets pushed to the exact middle
            for (int i = 0; i < poolSize; i++) {
                SpinSequenceItem item = new SpinSequenceItem();
                item.IsTarget = (i == targetIndexInSequence);
                
                // If it's the target, assign the winning symbol. Otherwise, random!
                item.Type = item.IsTarget ? targetType : GetRandomSymbolType();
                
                stoppingSequence.Enqueue(item);
            }

            _currentState = ReelState.Stopping;
        }

        private void MoveContainerDown() {
            symbolContainer.anchoredPosition += Vector2.down * (spinSpeed * Time.deltaTime);
            
            while (symbolContainer.anchoredPosition.y <= -symbolSpacing) {
                WrapBottomSymbolToTop();
            }

            // FIX APPLIED HERE: We now check "stoppingSequence.Count == 0"
            // This prevents it from stopping at Index 1 and forces it to wait for Index 2!
            if (_currentState == ReelState.Stopping && targetSymbolInstance != null && stoppingSequence.Count == 0) {
                RectTransform targetRect = (RectTransform)targetSymbolInstance.transform;
                float actualY = symbolContainer.anchoredPosition.y + targetRect.anchoredPosition.y;
                Debug.Log($"Actual Y for {gameObject.name}: {actualY} , container pos: {symbolContainer.anchoredPosition.y} ,and target rect pos : {targetRect.anchoredPosition.y} at index : {targetRect.GetSiblingIndex()}");
                actualY = 0;

                if (actualY <= 0f) {
                    SnapAndBounce(actualY);
                }
            }
        }

        private void WrapBottomSymbolToTop() {
            ReelSymbol bottomSymbol = symbolPool[symbolPool.Count - 1];
            bottomSymbol.transform.SetAsFirstSibling();
            
            symbolPool.RemoveAt(symbolPool.Count - 1);
            symbolPool.Insert(0, bottomSymbol);

            symbolContainer.anchoredPosition += new Vector2(0, symbolSpacing);
            LayoutRebuilder.ForceRebuildLayoutImmediate(symbolContainer);
            
            if (_currentState == ReelState.Stopping && stoppingSequence.Count > 0) {
                SpinSequenceItem nextItemInSequence = stoppingSequence.Dequeue();
                bottomSymbol.SetSymbol(_database.GetSymbolData(nextItemInSequence.Type));
                
                // Track it so we know exactly when to stop!
                if (nextItemInSequence.IsTarget) {
                    targetSymbolInstance = bottomSymbol;
                }
            }
            else {
                AssignRandomSymbolOffScreen(bottomSymbol);
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

        private void SnapAndBounce(float actualY) {
            _currentState = ReelState.Idle; 
            
            symbolContainer.anchoredPosition = new Vector2(0, actualY);

            if (reelAnimation != null) {
                // Explicitly pass symbolContainer as the RectTransform to animate
                reelAnimation.PlaySnapAndBounce(symbolContainer, symbolContainer.anchoredPosition, () => {
                    onReelStopped?.Invoke();
                    Debug.Log($"Reel stopped final pos : for gameobject : {gameObject.name} " + symbolContainer.anchoredPosition);
                });
            } else {
                onReelStopped?.Invoke();
            }
        }
    }
}