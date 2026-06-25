using SlotMachine.Data;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace SlotMachine {
    public class SpinResult {
        // Now it only holds the lightweight Enums
        public SymbolType[] FinalSymbols;
        public bool IsWin;
        public int TotalPayout;

        public SpinResult(int columns) {
            FinalSymbols = new SymbolType[columns];
            IsWin = false;
            TotalPayout = 0;
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SlotMathEngine {
        private SymbolDatabase database;
        private int totalWeight;

        // We now pass the entire database into the engine
        public SlotMathEngine(SymbolDatabase database) {
            this.database = database;
            CalculateTotalWeight();
        }

        private void CalculateTotalWeight() {
            totalWeight = 0;
            foreach (var symbol in database.GetAllSymbols()) {
                totalWeight += symbol.weight;
            }
        }

        /// <summary>
        /// Generates a random outcome based on symbol weights, returning an array of Enums.
        /// </summary>
        public SpinResult GenerateSpin(int numberOfColumns) {
            SpinResult result = new SpinResult(numberOfColumns);

            // Generate a weighted random Enum for each column
            for (int i = 0; i < numberOfColumns; i++) {
                result.FinalSymbols[i] = GetRandomSymbolType();
            }

            // Evaluate the generated Enums for a win
            EvaluateWin(result);

            return result;
        }

        // Mathematical implementation of Weighted Random Selection returning an Enum
        private SymbolType GetRandomSymbolType() {
            int randomValue = Random.Range(0, totalWeight);
            int currentWeightSum = 0;

            foreach (var symbol in database.GetAllSymbols()) {
                currentWeightSum += symbol.weight;

                if (randomValue < currentWeightSum) {
                    return symbol.symbolType; // Return the Enum key
                }
            }

            // Fallback (should never be reached if weights are > 0)
            return database.GetAllSymbols()[0].symbolType;
        }

        // Evaluates if all generated Enums match
        private void EvaluateWin(SpinResult result) {
            if (result.FinalSymbols.Length == 0) return;

            // Grab the enum from the first column
            SymbolType firstSymbolType = result.FinalSymbols[0];
            bool allMatch = true;

            // Compare all other columns to the first column's enum
            for (int i = 1; i < result.FinalSymbols.Length; i++) {
                if (result.FinalSymbols[i] != firstSymbolType) {
                    allMatch = false;
                    break;
                }
            }

            result.IsWin = allMatch;

            if (allMatch) {
                // If they won, use the Database to look up the payout value for that Enum
                SlotSymbol winningData = database.GetSymbolData(firstSymbolType);
                result.TotalPayout = winningData.payoutValue;

                Debug.Log($"Win! Payout: {result.TotalPayout} for matching {firstSymbolType}");
            }
        }
    }
}