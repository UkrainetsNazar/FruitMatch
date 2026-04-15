using Infrastructure.Audio;
using Presentation.Animations;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Presentation.UI
{
    public class SingleGameUI : BaseGameUI
    {
        [SerializeField] protected PanelAnimator singleGamePanel;
        [SerializeField] protected TMP_Text playerMultiplier;

        protected override void Start()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                enabled = false;
                return;
            }

            base.Start();
            if (singleGamePanel != null) singleGamePanel.Show();
            if (resultPanel != null) resultPanel.Hide();

            _gameState.OnGameFinished += ShowResult;
            _gameState.OnComboAchieved += OnComboAchieved;

            _returnButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                SceneManager.LoadScene("Menu");
            });

            RefreshUI();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_gameState != null)
            {
                _gameState.OnGameFinished -= ShowResult;
                _gameState.OnComboAchieved -= OnComboAchieved;
            }
        }

        protected override void RefreshUI()
        {
            var me = _gameState.GetPlayerData("Local");
            playerScore.text = $"{me.Score}";
            playerMoves.text = $"Moves: {me.MovesLeft}";
        }

        private void ShowResult(int finalScore)
        {
            AudioManager.PlayWinGame();
            if (finalPlayerScore != null)
                finalPlayerScore.text = $"{finalScore}";

            if (resultPanel != null) resultPanel.Show();
        }

        private void OnComboAchieved(string playerId, int combo)
        {
            if (playerId != "Local") return;
            ShowMultiplierOn(playerMultiplier, combo);
        }
    }
}