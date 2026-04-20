using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Unity.Netcode;
using Infrastructure.Audio;
using Presentation.Animations;

namespace Presentation.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private PanelAnimator _browsePanelGO;
        [SerializeField] private PanelAnimator _lobbyPanelGO;

        [Header("Browse Panel")]
        [SerializeField] private TMP_InputField _lobbyNameInput;
        [SerializeField] private ButtonAnimator _createButton;
        [SerializeField] private ButtonAnimator _refreshButton;
        [SerializeField] private Transform _lobbyListContainer;
        [SerializeField] private GameObject _lobbyItemPrefab;
        [SerializeField] private ButtonAnimator _menuButton;

        [Header("Lobby Panel")]
        [SerializeField] private TMP_Text _lobbyNameText;
        [SerializeField] private TMP_Text _playersCountText;
        [SerializeField] private Transform _playersContainer;
        [SerializeField] private GameObject _playerItemPrefab;
        [SerializeField] private ButtonAnimator _leaveButton;
        [SerializeField] private ButtonAnimator _startButton;

        [Header("Game Settings")]
        [SerializeField] private ButtonAnimator boardShape;
        [SerializeField] private Slider fruitCountSlider;
        [SerializeField] private TMP_Text fruitTypeCount;
        [SerializeField] private GameObject gameSettingsPanel;

        [Header("General Settings")]
        [SerializeField] private PanelAnimator settingsPanel;
        [SerializeField] private Slider musicVolumeSlider, sfxVolumeSlider;
        [SerializeField] private TMP_Text musicVolumeText, sfxVolumeText;
        [SerializeField] private ButtonAnimator backButton, quitButton;

        [Header("Loading")]
        [SerializeField] private PanelAnimator _loadingPanel;
        [SerializeField] private TMP_Text _loadingText;

        [Inject] private LobbyManager _lobbyManager;
        [Inject] private NetworkService _networkService;

        private readonly string[] _shapeNames = { "Random", "Square", "Ring", "Diamond", "Hourglass" };
        private int _selectedShapeIndex = 0;
        private string _pendingRelayCode;

        private void Start()
        {
            _createButton.onClick.AddListener(() =>
            { AudioManager.PlayButtonClick(); OnCreateClicked().Forget(); });
            _refreshButton.onClick.AddListener(() =>
            { AudioManager.PlayButtonClick(); OnRefreshClicked().Forget(); });
            _leaveButton.onClick.AddListener(() =>
            { AudioManager.PlayButtonClick(); OnLeaveClicked().Forget(); });
            _startButton.onClick.AddListener(() =>
            { AudioManager.PlayButtonClick(); OnStartClicked().Forget(); });
            _menuButton.onClick.AddListener(() =>
            { AudioManager.PlayButtonClick(); SceneManager.LoadScene("Menu"); });
            quitButton.onClick.AddListener(() =>
            { AudioManager.PlayButtonClick(); Application.Quit(); });

            _lobbyManager.OnLobbyUpdated += RefreshLobbyPanel;
            _lobbyManager.OnKicked += OnKickedFromLobby;
            _lobbyManager.OnHostLeft += OnHostLeftLobby;
            _lobbyManager.OnRelayCodeReady += OnRelayCodeReady;

            boardShape.onClick.AddListener(OnShapeCycleClicked);
            fruitCountSlider.minValue = 5;
            fruitCountSlider.maxValue = 7;
            fruitCountSlider.wholeNumbers = true;
            fruitCountSlider.value = 7;
            fruitCountSlider.onValueChanged.AddListener(OnFruitCountChanged);
            fruitTypeCount.text = "7";

            UpdateShapeButtonText();

            ShowBrowsePanel();
            OnRefreshClicked().Forget();

            settingsPanel.Hide();
            VolumeSliderBinder.BindMusic(musicVolumeSlider, musicVolumeText);
            VolumeSliderBinder.BindSfx(sfxVolumeSlider, sfxVolumeText);

            backButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                settingsPanel.Hide();
            });

            quitButton.onClick.AddListener(() =>
            {
                AudioManager.PlayButtonClick();
                Application.Quit();
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                if (settingsPanel.IsVisible) settingsPanel.Hide(); else settingsPanel.Show();
        }

        private void OnDestroy()
        {
            _lobbyManager.OnLobbyUpdated -= RefreshLobbyPanel;
            _lobbyManager.OnKicked -= OnKickedFromLobby;
            _lobbyManager.OnHostLeft -= OnHostLeftLobby;
            _lobbyManager.OnRelayCodeReady -= OnRelayCodeReady;
        }

        // ── Browse Panel ──────────────────────────────────────

        private void ShowBrowsePanel()
        {
            _browsePanelGO.Show();
            _lobbyPanelGO.Hide();
        }

        private void ShowLobbyPanel()
        {
            _browsePanelGO.Hide();
            _lobbyPanelGO.Show();
        }

        private void OnGameLoading()
        {
            _loadingPanel.Show();
            _loadingText.text = "Host is starting the game...";
        }

        private async UniTaskVoid OnCreateClicked()
        {
            var name = string.IsNullOrWhiteSpace(_lobbyNameInput.text)
                ? $"Lobby_{Random.Range(1000, 9999)}"
                : _lobbyNameInput.text;

            _createButton.interactable = false;

            await _lobbyManager.CreateLobbyAsync(name);

            _createButton.interactable = true;

            ShowLobbyPanel();
            RefreshLobbyPanel();
        }

        private async UniTaskVoid OnRefreshClicked()
        {
            _refreshButton.interactable = false;

            var lobbies = await _lobbyManager.GetLobbiesAsync();

            if (this == null || !gameObject.activeInHierarchy) return;

            RenderLobbyList(lobbies);

            _refreshButton.interactable = true;
        }

        private void RenderLobbyList(List<Lobby> lobbies)
        {
            if (_lobbyListContainer == null) return;

            foreach (Transform child in _lobbyListContainer)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }

            foreach (var lobby in lobbies)
            {
                if (_lobbyListContainer == null) return;

                var item = Instantiate(_lobbyItemPrefab, _lobbyListContainer);

                item.transform.Find("LobbyName").GetComponent<TMP_Text>().text = lobby.Name;
                item.transform.Find("PlayersCount").GetComponent<TMP_Text>().text =
                    $"{lobby.Players.Count}/{lobby.MaxPlayers}";

                item.transform.Find("JoinButton").GetComponent<Button>()
                    .onClick.AddListener(() => OnJoinClicked(lobby.Id).Forget());
            }
        }

        private async UniTaskVoid OnJoinClicked(string lobbyId)
        {
            await _lobbyManager.JoinLobbyAsync(lobbyId);

            ShowLobbyPanel();
            RefreshLobbyPanel();
        }

        // ── Lobby Panel ───────────────────────────────────────

        private void RefreshLobbyPanel()
        {
            var lobby = _lobbyManager.CurrentLobby;
            if (lobby == null) return;

            _lobbyNameText.text = lobby.Name;
            _playersCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

            bool isFull = lobby.Players.Count >= lobby.MaxPlayers;
            _startButton.gameObject.SetActive(_lobbyManager.IsHost);
            _startButton.interactable = isFull;

            gameSettingsPanel.SetActive(_lobbyManager.IsHost);

            RenderPlayerList();
        }

        private void RenderPlayerList()
        {
            foreach (Transform child in _playersContainer)
                Destroy(child.gameObject);

            var players = _lobbyManager.GetPlayers();
            foreach (var player in players)
            {
                var item = Instantiate(_playerItemPrefab, _playersContainer);

                item.transform.Find("PlayerName").GetComponent<TMP_Text>().text =
                    player.PlayerName;

                var kickButton = item.transform.Find("KickButton").GetComponent<Button>();
                bool isLocalPlayer = player.PlayerId ==
                    Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;

                kickButton.gameObject.SetActive(_lobbyManager.IsHost && !isLocalPlayer);
                kickButton.onClick.AddListener(() =>
                    OnKickClicked(player.PlayerId).Forget());
            }
        }

        private async UniTaskVoid OnKickClicked(string playerId)
        {
            await _lobbyManager.KickPlayerFromLobby(playerId);
            RefreshLobbyPanel();
        }

        private async UniTaskVoid OnLeaveClicked()
        {
            await _lobbyManager.LeaveLobbyAsync();
            ShowBrowsePanel();
            OnRefreshClicked().Forget();
        }

        private async UniTaskVoid OnStartClicked()
        {
            _startButton.interactable = false;

            _loadingPanel.Show();
            _loadingText.text = "Creating session...";

            int shapeChoice = _selectedShapeIndex == 0 ? -1 : _selectedShapeIndex - 1;
            int fruitCount = (int)fruitCountSlider.value;

            PlayerPrefs.SetInt("LobbyShapeChoice", shapeChoice);
            PlayerPrefs.SetInt("LobbyFruitCount", fruitCount);

            await _networkService.StartHostAsync();

            var network = FindObjectOfType<NetworkGameManager>();
            network?.BroadcastGameLoadingClientRpc();

            _loadingText.text = "Waiting for opponent...";

            _loadingText.text = "Waiting for opponent...";

            await UniTask.WaitUntil(() =>
                NetworkManager.Singleton.ConnectedClients.Count >= 2);

            _loadingText.text = "Starting game...";

            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }

        private void OnKickedFromLobby()
        {
            ShowBrowsePanel();
            OnRefreshClicked().Forget();
        }

        private void OnHostLeftLobby()
        {
            ShowBrowsePanel();
            OnRefreshClicked().Forget();
        }

        private void OnRelayCodeReady(string code)
        {
            _pendingRelayCode = code;
            JoinAsClient().Forget();
        }

        private async UniTaskVoid JoinAsClient()
        {
            await _networkService.StartClientAsync(_pendingRelayCode);
        }

        private void OnShapeCycleClicked()
        {
            AudioManager.PlayButtonClick();
            _selectedShapeIndex = (_selectedShapeIndex + 1) % _shapeNames.Length;
            UpdateShapeButtonText();
        }

        private void UpdateShapeButtonText()
        {
            boardShape.GetComponentInChildren<TMP_Text>().text =
                _shapeNames[_selectedShapeIndex];
        }

        private void OnFruitCountChanged(float value)
        {
            fruitTypeCount.text = ((int)value).ToString();
        }
    }
}