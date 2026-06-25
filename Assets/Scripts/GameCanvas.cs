using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine {
    public class GameCanvas : MonoBehaviour {
        [SerializeField] Image normalHandle;
        [SerializeField] Image activeHandle;

        [SerializeField] List<Button> bettingButtons;
        [SerializeField] List<int> amountToBet;

        [SerializeField] GameObject buttonParent;
        
        [SerializeField] TextMeshProUGUI coinText;
        
        

        void OnEnable() {
            for (var i = 0; i < bettingButtons.Count; i++) {
                var currentButton = bettingButtons[i];
                var currentAmount = amountToBet[i];
                currentButton.onClick.AddListener(() => OnBettingButtonClick(currentAmount));
                currentButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Bet <color=#FFD700>{currentAmount}G</color>";
            }
            
            GameManager.OnCoinChange += OnCoinChange; 
        }

        void OnCoinChange(int amount) {
            coinText.text = amount.ToString();
        }

        void Start() {
            coinText.text = GameManager.Instance.Coins.ToString();
        }

        void OnDisable() {
            for (var i = 0; i < bettingButtons.Count; i++) {
                var currentButton = bettingButtons[i];
                currentButton.onClick.RemoveAllListeners();
            }
            GameManager.OnCoinChange -= OnCoinChange;
        }

        void OnBettingButtonClick(int amount) {
            var canStart = GameManager.Instance.TryStartBetting(amount);
            if (canStart) {
                StartCoroutine(HandleCoroutine());
                buttonParent.SetActive(false);
            }

            else {
                //DO shake animation
            }
        }

        IEnumerator HandleCoroutine() {
            ToggleHandle(true);
            yield return new WaitForSeconds(1f);
            ToggleHandle(false);
        }


        public void ToggleHandle(bool isActive) {
            if (!isActive) {
                normalHandle.enabled = true;
                activeHandle.enabled = false;
            }
            else {
                normalHandle.enabled = false;
                activeHandle.enabled = true;
            }
        }

        public void OnSpinComplete() {
            buttonParent.SetActive(true);
        }
    }
}