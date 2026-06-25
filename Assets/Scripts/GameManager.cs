using System;
using System.Threading.Tasks;
using SlotMachine.Data;
using SlotMachine.Extension;
using SlotMachine.SlotReel;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SlotMachine {
    
    public class GameManager : PersistentSingleton<GameManager> {

        public static Action<int> OnCoinChange;
        
        [SerializeField] SymbolDatabase database;
        
        [field:SerializeField] public int Coins { get; private set; } = 1000;
        
        [Space]
        [Header("Scene references")]
        [SerializeField] ReelManager reelManager;
        [SerializeField] GameCanvas gameCanvas;

        [SerializeField] float timeForResult = 2f;
        
        SlotMathEngine mathEngine;

        protected override void Awake() {
            database.Initialize();
            mathEngine = new SlotMathEngine(database);
            
            reelManager.InitializeReels(database);
        }

        void Start() {
            gameCanvas.ToggleHandle(false);
            gameCanvas.OnSpinComplete();
        }

        async public void  Spin() {
            reelManager.SpinAllReels();
            SpinResult result = mathEngine.GenerateSpin(3);
            await Awaitable.WaitForSecondsAsync(timeForResult);
            reelManager.StopReels(result, OnSpinComplete);
            Debug.Log($"Spin Result: {result}");

            Coins += result.TotalPayout;
            
            OnCoinChange?.Invoke(Coins);
        }

        void OnSpinComplete() {
            gameCanvas.OnSpinComplete();
        }

        public bool TryStartBetting(int amount) {
            if (amount <= Coins) {
                Coins -= amount;
                OnCoinChange?.Invoke(Coins);
                Spin();
                return true;
            }

            return false;
        }


        void OnDestroy() {
            mathEngine = null;
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            GameManager myScript = (GameManager)target;

            EditorGUILayout.Space(10); // Add a 10-pixel visual gap

            EditorGUILayout.HelpBox("Note: This button is for triggering whole spin logic without money and to test the whole flow", MessageType.Info);
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Warning : should be triggered only in game mode", MessageType.Warning);

            
            if (GUILayout.Button("Test Spin", GUILayout.Height(40))) {
                myScript.Spin();
            }
        }
    }
#endif
}