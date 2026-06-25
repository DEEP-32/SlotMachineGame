using SlotMachine.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace SlotMachine.SlotReel {
    public class ReelController : MonoBehaviour {
        enum ReelState { Idle, Spinning, Stopping }

        ReelState currentState = ReelState.Idle;
        SymbolDatabase database;

        [Header("UI Reel Settings")]
        [Tooltip("Speed in UI Pixels per second (e.g., 2000 - 3000)")]
        [SerializeField]
        float spinSpeed = 2500f; 

        [Tooltip("MUST exactly match the Height + Spacing of your cell in the VerticalLayoutGroup.")]
        [SerializeField] 
        float symbolSpacing = 250f; 

        [Header("Symbol Pool")]
        [Tooltip("The UI prefab for a single symbol on the reel.")]
        [SerializeField]
        ReelSymbol symbolPrefab;

        [Tooltip("The parent UI object that HAS the Vertical Layout Group attached.")]
        [SerializeField]
        RectTransform symbolContainer;

        [Tooltip("Must be an ODD number (e.g., 5 or 7). This ensures there is a strict center symbol.")]
        [SerializeField]
        int poolSize = 5;

        [Header("Components")]
        [Tooltip("Reference to the animation component attached to the SymbolContainer.")]
        [SerializeField]
        ReelAnimation reelAnimation;

        // State Tracking
        List<ReelSymbol> symbolPool = new List<ReelSymbol>();
        Queue<SymbolType> stoppingSequence = new Queue<SymbolType>();
        ReelSymbol targetSymbolInstance;
        Action onReelStopped;

        public void Initialize(SymbolDatabase database) {
            this.database = database;

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
            if (currentState == ReelState.Idle) return;
            MoveContainerDown();
        }

        // --- PUBLIC API (Called by ReelManager) ---

        public void StartSpinning() {
            currentState = ReelState.Spinning;
            stoppingSequence.Clear();
            targetSymbolInstance = null;
        }

        public void StopOnSymbol(SymbolType targetType, Action onStoppedCallback) {
            onReelStopped = onStoppedCallback;

            // Queue: Bottom (Random), Center (Target), Top (Random)
            stoppingSequence.Enqueue(GetRandomSymbolType()); 
            stoppingSequence.Enqueue(targetType); 
            stoppingSequence.Enqueue(GetRandomSymbolType()); 

            currentState = ReelState.Stopping;
        }

        // --- INTERNAL LOGIC ---

        private void MoveContainerDown() {
            // 1. Move the entire container downwards in UI space
            symbolContainer.anchoredPosition += Vector2.down * (spinSpeed * Time.deltaTime);
            
            // 2. While loop prevents separation glitches if spin speed is very high
            while (symbolContainer.anchoredPosition.y <= -symbolSpacing) {
                WrapBottomSymbolToTop();
            }

            // 3. Stop logic checking physical UI coordinates
            if (currentState == ReelState.Stopping && targetSymbolInstance != null) {
                RectTransform targetRect = (RectTransform)targetSymbolInstance.transform;
                
                // Actual Y is Container offset + Symbol's local Y inside the layout group
                float actualY = symbolContainer.anchoredPosition.y + targetRect.anchoredPosition.y;

                if (actualY <= 0f) {
                    SnapAndBounce(actualY);
                }
            }
        }

        private void WrapBottomSymbolToTop() {
            ReelSymbol bottomSymbol = symbolPool[symbolPool.Count - 1];

            // Move the bottom symbol to the top of the Layout Group hierarchy
            bottomSymbol.transform.SetAsFirstSibling();
            
            symbolPool.RemoveAt(symbolPool.Count - 1);
            symbolPool.Insert(0, bottomSymbol);

            // Shift container position back up to counter the layout change seamlessly
            symbolContainer.anchoredPosition += new Vector2(0, symbolSpacing);
            LayoutRebuilder.ForceRebuildLayoutImmediate(symbolContainer);
            
            // Inject next sequence symbol
            if (currentState == ReelState.Stopping && stoppingSequence.Count > 0) {
                SymbolType nextTypeInSequence = stoppingSequence.Dequeue();
                bottomSymbol.SetSymbol(database.GetSymbolData(nextTypeInSequence));
                
                if (stoppingSequence.Count == 1) {
                    targetSymbolInstance = bottomSymbol;
                }
            }
            else {
                AssignRandomSymbolOffScreen(bottomSymbol);
            }
        }

        private void AssignRandomSymbolOffScreen(ReelSymbol symbol) {
            if (database == null) return;
            SymbolType randomType = GetRandomSymbolType();
            symbol.SetSymbol(database.GetSymbolData(randomType));
        }

        private SymbolType GetRandomSymbolType() {
            var allSymbols = database.GetAllSymbols();
            int randomIndex = Random.Range(0, allSymbols.Count);
            return allSymbols[randomIndex].symbolType;
        }

        private void SnapAndBounce(float actualY) {
            currentState = ReelState.Idle; 

            // Snap the container perfectly to grid center to fix the overshoot
            symbolContainer.anchoredPosition -= new Vector2(0, actualY);

            if (reelAnimation != null) {
                reelAnimation.PlaySnapAndBounce(symbolContainer.anchoredPosition, () => {
                    onReelStopped?.Invoke();
                });
            } else {
                onReelStopped?.Invoke();
            }
        }
    }
}