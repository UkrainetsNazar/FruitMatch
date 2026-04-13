using Infrastructure.Audio;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Presentation.UI
{
    public class SingleGameUI : BaseGameUI
    {
        [SerializeField] protected GameObject singleGamePanel;

        protected override void Start()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                enabled = false;
                return;
            }

            base.Start();
            if (singleGamePanel != null) singleGamePanel.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);

            _gameState.OnGameFinished += ShowResult;

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
                _gameState.OnGameFinished -= ShowResult;
        }

        protected override void RefreshUI()
        {
            var me = _gameState.GetPlayerData("Local");
            playerScore.text = $"{me.Score}";
            playerMoves.text = $"Moves: {me.MovesLeft}";
        }

        private void ShowResult(int finalScore)
        {
            if (finalPlayerScore != null)
                finalPlayerScore.text = $"Score: {finalScore}";

            if (resultPanel != null)
                resultPanel.SetActive(true);
        }
    }
}