using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Core.Interfaces;
using Unity.Netcode;

namespace Presentation.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _browsePanelGO;
        [SerializeField] private GameObject _lobbyPanelGO;

        [Header("Browse Panel")]
        [SerializeField] private TMP_InputField _lobbyNameInput;
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Transform _lobbyListContainer;
        [SerializeField] private GameObject _lobbyItemPrefab;
        [SerializeField] private Button _menuButton;

        [Header("Lobby Panel")]
        [SerializeField] private TMP_Text _lobbyNameText;
        [SerializeField] private TMP_Text _playersCountText;
        [SerializeField] private Transform _playersContainer;
        [SerializeField] private GameObject _playerItemPrefab;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Button _startButton;

        [Header("Game Settings")]
        [SerializeField] private Button boardShape;
        [SerializeField] private Slider fruitCoundSlider;

        [Inject] private LobbyManager _lobbyManager;
        [Inject] private NetworkService _networkService;
        [Inject] private IGameStateService _gameState;

        private readonly string[] _shapeNames = { "Random", "Square", "Ring", "Diamond", "Hourglass" };
        private int _selectedShapeIndex = 0;
        private string _pendingRelayCode;

        private void Start()
        {
            _createButton.onClick.AddListener(() => OnCreateClicked().Forget());
            _refreshButton.onClick.AddListener(() => OnRefreshClicked().Forget());
            _leaveButton.onClick.AddListener(() => OnLeaveClicked().Forget());
            _startButton.onClick.AddListener(() => OnStartClicked().Forget());
            _menuButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));

            _lobbyManager.OnLobbyUpdated += RefreshLobbyPanel;
            _lobbyManager.OnKicked += OnKickedFromLobby;
            _lobbyManager.OnHostLeft += OnHostLeftLobby;
            _lobbyManager.OnRelayCodeReady += OnRelayCodeReady;

            boardShape.onClick.AddListener(OnShapeCycleClicked);
            fruitCoundSlider.minValue = 5;
            fruitCoundSlider.maxValue = 7;
            fruitCoundSlider.wholeNumbers = true;
            fruitCoundSlider.value = 7;
            fruitCoundSlider.onValueChanged.AddListener(OnFruitCountChanged);

            UpdateShapeButtonText();

            ShowBrowsePanel();
            OnRefreshClicked().Forget();
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
            _browsePanelGO.SetActive(true);
            _lobbyPanelGO.SetActive(false);
        }

        private void ShowLobbyPanel()
        {
            _browsePanelGO.SetActive(false);
            _lobbyPanelGO.SetActive(true);
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
            RenderLobbyList(lobbies);

            _refreshButton.interactable = true;
        }

        private void RenderLobbyList(List<Lobby> lobbies)
        {
            foreach (Transform child in _lobbyListContainer)
                Destroy(child.gameObject);

            foreach (var lobby in lobbies)
            {
                var item = Instantiate(_lobbyItemPrefab, _lobbyListContainer);

                item.transform.Find("LobbyName").GetComponent<TMP_Text>().text =
                    lobby.Name;

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

            await _networkService.StartHostAsync();

            await UniTask.WaitUntil(() =>
                NetworkManager.Singleton.ConnectedClients.Count >= 2);

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
            
        }
    }
}