using Core.Interfaces;
using Infrastructure.Audio;
using Infrastructure.Network;
using Presentation.Animations;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Presentation.Ui
{
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text header;
        [SerializeField] private ButtonAnimator returnButton, continueButton, quitButton;
        [SerializeField] private PanelAnimator pausePanel;

        [InjectOptional] private NetworkGameManager _network;
        [Inject] private IGameStateService _gameState;

        private bool _isGameFinished;

        void Start()
        {
            returnButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                if (NetworkManager.Singleton != null)
                    NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Menu");
            });

            continueButton.onClick.AddListener(TogglePause);

            quitButton.onClick.AddListener(() =>
            { AudioManager.PlayButtonClick(); Application.Quit(); });

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
            pausePanel.Show();
        }

        private void TogglePause()
        {
            AudioManager.PlayButtonClick();
            if (pausePanel == null) return;
            header.text = "Pause";
            continueButton.gameObject.SetActive(true);
            if (pausePanel.IsVisible) pausePanel.Hide(); else pausePanel.Show();
        }
    }
}