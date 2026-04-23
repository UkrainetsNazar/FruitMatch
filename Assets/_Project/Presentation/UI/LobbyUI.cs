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
using System.Threading;
using System;

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
        private float _lastRefreshTime = -999f;
        private const float RefreshCooldown = 3f;
        private CancellationTokenSource _refreshLoopCts;
        private bool _startAborted = false;


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
            _lobbyManager.OnLobbyUpdated += OnLobbyUpdatedDuringStart;

            boardShape.onClick.AddListener(OnShapeCycleClicked);
            fruitCountSlider.minValue = 5;
            fruitCountSlider.maxValue = 7;
            fruitCountSlider.wholeNumbers = true;
            fruitCountSlider.value = 7;
            fruitCountSlider.onValueChanged.AddListener(OnFruitCountChanged);
            fruitTypeCount.text = "7";

            UpdateShapeButtonText();

            ShowBrowsePanel();
            StartRefreshLoop().Forget();
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
            _lobbyManager.OnLobbyUpdated -= OnLobbyUpdatedDuringStart;
            _refreshLoopCts?.Cancel();
            _refreshLoopCts?.Dispose();
        }

        // ── Browse Panel ──────────────────────────────────────

        private void ShowBrowsePanel()
        {
            _browsePanelGO.Show();
            _lobbyPanelGO.Hide();
            StartRefreshLoop().Forget();
        }

        private void ShowLobbyPanel()
        {
            _refreshLoopCts?.Cancel();
            _browsePanelGO.Hide();
            _lobbyPanelGO.Show();
        }

        private async UniTaskVoid OnCreateClicked()
        {
            var name = string.IsNullOrWhiteSpace(_lobbyNameInput.text)
                ? $"Lobby_{UnityEngine.Random.Range(1000, 9999)}"
                : _lobbyNameInput.text;

            _createButton.interactable = false;

            var lobby = await _lobbyManager.CreateLobbyAsync(name);

            if (lobby != null)
            {
                ShowLobbyPanel();
                RefreshLobbyPanel();
            }

            _createButton.interactable = true;
        }

        private async UniTask OnRefreshClicked()
        {
            if (_lobbyPanelGO.IsVisible) return;

            if (Time.time - _lastRefreshTime < RefreshCooldown && !Mathf.Approximately(_lastRefreshTime, -999f)) return;
            _lastRefreshTime = Time.time;

            _refreshButton.interactable = false;
            var lobbies = await _lobbyManager.GetLobbiesAsync();

            if (this == null || !gameObject.activeInHierarchy) return;

            RenderLobbyList(lobbies);
            _refreshButton.interactable = true;
        }

        private async UniTaskVoid StartRefreshLoop()
        {
            _refreshLoopCts?.Cancel();
            _refreshLoopCts = new CancellationTokenSource();
            var token = _refreshLoopCts.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await OnRefreshClicked();
                    await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: token);
                }
            }
            catch (OperationCanceledException) { }
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
            _refreshButton.interactable = false;
            _createButton.interactable = false;

            var lobby = await _lobbyManager.JoinLobbyAsync(lobbyId);

            if (lobby != null)
            {
                ShowLobbyPanel();
                RefreshLobbyPanel();
            }
            else
            {
                await OnRefreshClicked();
                Debug.LogWarning("Failed to join: Lobby might be gone.");
            }

            _refreshButton.interactable = true;
            _createButton.interactable = true;
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
        }

        private void OnLobbyUpdatedDuringStart()
        {
            var lobby = _lobbyManager.CurrentLobby;
            if (lobby != null && lobby.Players.Count < 2)
                _startAborted = true;
        }

        private async UniTaskVoid OnStartClicked()
        {
            _startAborted = false;
            _lobbyPanelGO.Hide();
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

            float elapsed = 0f;
            const float timeout = 30f;

            while (NetworkManager.Singleton.ConnectedClients.Count < 2)
            {
                elapsed += Time.deltaTime;

                if (elapsed >= timeout || _startAborted)
                {
                    await _networkService.Disconnect();
                    _loadingPanel.Hide();
                    ShowBrowsePanel();
                    OnRefreshClicked().Forget();
                    return;
                }

                await UniTask.Yield();
            }

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