using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Core.Interfaces;
using System;
using Core.Domain.Entities;
using System.Threading;

namespace Infrastructure.Network
{
    public class LobbyManager
    {
        private Lobby _currentLobby;
        private readonly IGameStateService _gameState;
        private bool _isProcessing;

        public event Action OnLobbyUpdated;
        public event Action OnKicked;
        public event Action OnHostLeft;
        public event Action<string> OnRelayCodeReady;

        public Lobby CurrentLobby => _currentLobby;

        private CancellationTokenSource _pollCts;
        private CancellationTokenSource _heartbeatCts;

        public bool IsHost =>
            _currentLobby?.HostId == AuthenticationService.Instance.PlayerId;

        public LobbyManager(IGameStateService gameState)
        {
            _gameState = gameState;
        }

        #region Create / Join

        public async UniTask<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers = 2)
        {
            if (_isProcessing) return null;
            _isProcessing = true;

            try
            {
                await LeaveLobbyAsync();

                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = CreatePlayer(_gameState.LocalPlayer.PlayerName)
                };

                _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                Debug.Log($"[Lobby] Created: {_currentLobby.Id}");

                StartHeartbeat().Forget();
                StartPolling().Forget();

                return _currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"CreateLobby failed: {e.Message}");
                return null;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public async UniTask<Lobby> JoinLobbyAsync(string lobbyId)
        {
            if (_isProcessing) return null;
            _isProcessing = true;

            try
            {
                await LeaveLobbyAsync();

                var options = new JoinLobbyByIdOptions
                {
                    Player = CreatePlayer(_gameState.LocalPlayer.PlayerName)
                };

                _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
                Debug.Log($"[Lobby] Joined: {_currentLobby.Id}");

                StartPolling().Forget();
                return _currentLobby;
            }
            catch (LobbyServiceException e)
            {
                _currentLobby = null;

                if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                    Debug.LogWarning("Lobby no longer exists.");
                else
                    Debug.LogError($"JoinLobby failed: {e.Message}");

                return null;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        #endregion

        #region Player Mapping

        private Player CreatePlayer(string playerName)
        {
            return new Player
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                }
            };
        }

        public List<PlayerData> GetPlayers()
        {
            var result = new List<PlayerData>();
            if (_currentLobby == null) return result;

            foreach (var player in _currentLobby.Players)
            {
                result.Add(new PlayerData
                {
                    PlayerId = player.Id,
                    PlayerName = GetValue(player, "PlayerName")
                });
            }

            return result;
        }

        private string GetValue(Player player, string key, string defaultValue = "Unknown")
        {
            if (player.Data != null && player.Data.ContainsKey(key))
                return player.Data[key].Value;
            return defaultValue;
        }

        #endregion

        #region Relay

        public async UniTask SetRelayCodeAsync(string relayCode)
        {
            if (_currentLobby == null)
            {
                Debug.LogError("SetRelayCode failed: no current lobby.");
                return;
            }

            if (!IsHost)
            {
                Debug.LogWarning("Only host can set relay code.");
                return;
            }

            try
            {
                var options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                };

                string lobbyId = _currentLobby.Id;
                _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
                Debug.Log($"[Lobby] Relay code set: {relayCode}");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"SetRelayCode failed: {e.Message}");
                if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                    _currentLobby = null;
                throw;
            }
        }

        #endregion

        #region Lobby Actions

        public async UniTask<List<Lobby>> GetLobbiesAsync()
        {
            try
            {
                var options = new QueryLobbiesOptions
                {
                    Count = 20,
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };

                var result = await LobbyService.Instance.QueryLobbiesAsync(options);
                return result.Results;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"QueryLobbies failed: {e.Message}");
                return new List<Lobby>();
            }
        }

        public async UniTask LeaveLobbyAsync()
        {
            _pollCts?.Cancel();
            _pollCts = null;

            _heartbeatCts?.Cancel();
            _heartbeatCts = null;

            if (_currentLobby == null) return;

            try
            {
                string lobbyIdToLeave = _currentLobby.Id;
                _currentLobby = null;

                await LobbyService.Instance.RemovePlayerAsync(
                    lobbyIdToLeave,
                    AuthenticationService.Instance.PlayerId);

                Debug.Log("[Lobby] Left lobby.");
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason != LobbyExceptionReason.LobbyNotFound)
                    Debug.LogWarning($"LeaveLobby error: {e.Message}");
            }
        }

        public async UniTask KickPlayerFromLobby(string playerId)
        {
            if (!IsHost || _currentLobby == null) return;

            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Kick failed: {e.Message}");
            }
        }

        #endregion

        #region Background Systems

        private async UniTaskVoid StartHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts = new CancellationTokenSource();
            var ct = _heartbeatCts.Token;

            while (_currentLobby != null && IsHost && !ct.IsCancellationRequested)
            {
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogWarning($"Heartbeat failed: {e.Message}");
                }

                bool cancelled = await UniTask.Delay(
                    TimeSpan.FromSeconds(15),
                    cancellationToken: ct
                ).SuppressCancellationThrow();

                if (cancelled) break;
            }
        }

        private async UniTaskVoid StartPolling()
        {
            _pollCts?.Cancel();
            _pollCts = new CancellationTokenSource();
            var ct = _pollCts.Token;

            while (_currentLobby != null && !ct.IsCancellationRequested)
            {
                bool cancelled = await UniTask.Delay(1500, cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (cancelled || _currentLobby == null) break;

                try
                {
                    var updated = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
                    var localId = AuthenticationService.Instance.PlayerId;

                    bool stillInLobby = updated.Players.Exists(p => p.Id == localId);
                    if (!stillInLobby)
                    {
                        _currentLobby = null;
                        OnKicked?.Invoke();
                        break;
                    }

                    _currentLobby = updated;
                    OnLobbyUpdated?.Invoke();

                    if (!IsHost && _currentLobby.Data != null)
                    {
                        if (_currentLobby.Data.TryGetValue("RelayJoinCode", out var code)
                            && !string.IsNullOrEmpty(code.Value))
                        {
                            Debug.Log($"[Lobby] Got relay code: {code.Value}");
                            _pollCts.Cancel();
                            OnRelayCodeReady?.Invoke(code.Value);
                            break;
                        }
                    }
                }
                catch (LobbyServiceException e)
                {
                    if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                    {
                        _currentLobby = null;
                        OnHostLeft?.Invoke();
                        break;
                    }
                    Debug.LogWarning($"[Lobby] Poll error: {e.Message}");
                }
            }
        }

        #endregion
    }
}