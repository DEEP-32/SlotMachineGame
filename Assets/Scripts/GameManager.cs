using System;
using SlotMachine.Data;
using SlotMachine.Extension;
using SlotMachine.SlotReel;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SlotMachine {

    enum GameStates {
        Start,
        Betting,
        Spinning,
        Winning,
        Losing
    }
    
    public class GameManager : PersistentSingleton<GameManager> {
        [SerializeField] SymbolDatabase database;
        [SerializeField] SaveHandler saveHandler;
        
        [Space]
        [Header("Scene references")]
        [SerializeField] ReelManager reelManager;
        
        SlotMathEngine mathEngine;

        protected override void Awake() {
            database.Initialize();
            mathEngine = new SlotMathEngine(database);
            
            //load save data or just set to default depending on ignoreSaveData
            saveHandler.Initialize();
            
            reelManager.InitializeReels(database);
        }

        public void Spin() {
            SpinResult result = mathEngine.GenerateSpin(3);
            Debug.Log($"Spin Result: {result}");
        }


        void OnDestroy() {
            saveHandler.Save();

            mathEngine = null;
            saveHandler = null;
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            GameManager myScript = (GameManager)target;

            EditorGUILayout.Space(10); // Add a 10-pixel visual gap

            EditorGUILayout.HelpBox("Note: This button is for testing the math engine and outcome generation in the console. It does NOT trigger visual reel animations.", MessageType.Info);

            
            if (GUILayout.Button("Test Spin", GUILayout.Height(40))) {
                myScript.Spin();
            }
        }
    }
#endif
}