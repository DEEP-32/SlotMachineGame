using UnityEngine;
using System.IO;

namespace SlotMachine {
    // This class holds the actual data variables. 
    // Separating it makes JSON serialization much cleaner.
    [System.Serializable]
    public class SaveData {
        public int coins = 1000;
    }

    [System.Serializable]
    public class SaveHandler {
        [Tooltip("If true, the game will ignore existing save files and start with default values. Useful for testing.")]
        public bool ignoreSaveData = false;

        public SaveData Data = new SaveData();

        private string GetSavePath() {
            return Path.Combine(Application.persistentDataPath, "slotgame_save.json");
        }
        
        public void Initialize() {
            if (ignoreSaveData) {
                Debug.Log("<color=yellow>SaveHandler: 'Ignore Save Data' is TRUE. Starting with default values.</color>");
                Data = new SaveData(); // Fresh data
                return;
            }

            Load();
        }

        
        public void Save() {
            string json = JsonUtility.ToJson(Data, true);

            File.WriteAllText(GetSavePath(), json);
            Debug.Log($"<color=green>Game Saved! Coins: {Data.coins}</color>");
        }

       
        private void Load() {
            string path = GetSavePath();

            if (File.Exists(path)) {
                string json = File.ReadAllText(path);
                Data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("<color=cyan>Save data loaded successfully.</color>");
            }
            else {
                Debug.Log("No existing save found. Creating new default save.");
                Data = new SaveData();
            }
        }


        public void AddCoins(int amount) {
            Data.coins += amount;
            Save();
        }

        public bool TrySpendCoins(int amount) {
            if (Data.coins >= amount) {
                Data.coins -= amount;
                Save(); 
                return true;
            }

            return false;
        }
    }
}