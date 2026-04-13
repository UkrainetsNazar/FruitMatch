using Core.Domain;
using Core.Interfaces;
using Infrastructure.Audio;
using Infrastructure.Network;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Ui
{
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text header;
        [SerializeField] private Button returnButton, continueButton;
        [SerializeField] private GameObject pausePanel;

        [InjectOptional] private NetworkGameManager _network;
        [Inject] private IGameStateService _gameState;

        private bool _isGameFinished;

        void Start()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);

            returnButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                if (NetworkManager.Singleton != null)
                    NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Menu");
            });

            continueButton.onClick.AddListener(TogglePause);

            if (_network != null) _network.OnOpponentDisconnected += OnOpponentDisconnected;
            _gameState.OnGameFinished += _ => _isGameFinished = true;
        }

        void OnDestroy()
        {
            if (_network != null) _network.OnOpponentDisconnected -= OnOpponentDisconnected;
            if (_gameState != null) _gameState.OnGameFinished -= _ => _isGameFinished = true;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        private void OnOpponentDisconnected()
        {
            if (_isGameFinished) return;

            header.text = "Opponent left the game";
            continueButton.gameObject.SetActive(false);
            pausePanel.SetActive(true);
        }

        private void TogglePause()
        {
            AudioManager.PlayButtonClick();
            if (pausePanel == null) return;
            header.text = "Pause";
            continueButton.gameObject.SetActive(true);
            pausePanel.SetActive(!pausePanel.activeSelf);
        }
    }
}