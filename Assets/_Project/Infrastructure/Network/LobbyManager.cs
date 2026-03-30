using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Core.Domain;
using Core.Interfaces;
using System;

namespace Infrastructure.Network
{
    public class LobbyManager
    {
        private Lobby _currentLobby;
        private readonly IGameStateService _gameState;

        public event Action OnLobbyUpdated;
        public event Action OnKicked;
        public event Action OnHostLeft;
        public event Action<string> OnRelayCodeReady;

        public Lobby CurrentLobby => _currentLobby;

        public bool IsHost =>
            _currentLobby?.HostId ==
            AuthenticationService.Instance.PlayerId;

        public LobbyManager(IGameStateService gameState)
        {
            _gameState = gameState;
        }

        #region Create / Join

        public async UniTask<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers = 2)
        {
            try
            {
                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = CreatePlayer(_gameState.LocalPlayer.PlayerName)
                };

                _currentLobby = await LobbyService.Instance
                    .CreateLobbyAsync(lobbyName, maxPlayers, options);

                StartHeartbeat().Forget();
                StartPolling().Forget();

                return _currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"CreateLobby failed: {e.Message}");
                throw;
            }
        }

        public async UniTask<Lobby> JoinLobbyAsync(string lobbyId)
        {
            try
            {
                var options = new JoinLobbyByIdOptions
                {
                    Player = CreatePlayer(_gameState.LocalPlayer.PlayerName)
                };

                _currentLobby = await LobbyService.Instance
                    .JoinLobbyByIdAsync(lobbyId, options);

                StartPolling().Forget();

                return _currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"JoinLobby failed: {e.Message}");
                throw;
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

            if (_currentLobby == null)
                return result;

            foreach (var player in _currentLobby.Players)
            {
                var data = new PlayerData
                {
                    PlayerId = player.Id,
                    PlayerName = GetValue(player, "PlayerName")
                };

                result.Add(data);
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
            try
            {
                var options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayJoinCode", new DataObject(
                            DataObject.VisibilityOptions.Member, relayCode) }
                    }
                };

                _currentLobby = await LobbyService.Instance
                    .UpdateLobbyAsync(_currentLobby.Id, options);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"SetRelayCode failed: {e.Message}");
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
            if (_currentLobby == null) return;

            try
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    _currentLobby.Id,
                    AuthenticationService.Instance.PlayerId);

                _currentLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"LeaveLobby failed: {e.Message}");
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
            while (_currentLobby != null && IsHost)
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
                await UniTask.Delay(15000);
            }
        }

        private async UniTaskVoid StartPolling()
        {
            while (_currentLobby != null)
            {
                await UniTask.Delay(1500);

                if (_currentLobby == null) break;

                try
                {
                    var updated = await LobbyService.Instance
                        .GetLobbyAsync(_currentLobby.Id);

                    var localId = AuthenticationService.Instance.PlayerId;
                    bool stillInLobby = updated.Players
                        .Exists(p => p.Id == localId);

                    if (!stillInLobby)
                    {
                        _currentLobby = null;
                        OnKicked?.Invoke();
                        break;
                    }

                    bool hostLeft = !updated.Players
                        .Exists(p => p.Id == updated.HostId);

                    if (hostLeft && !IsHost)
                    {
                        _currentLobby = null;
                        OnHostLeft?.Invoke();
                        break;
                    }

                    _currentLobby = updated;
                    OnLobbyUpdated?.Invoke();

                    if (!IsHost && _currentLobby.Data != null)
                    {
                        if (_currentLobby.Data.TryGetValue("RelayJoinCode", out var code)
                            && !string.IsNullOrEmpty(code.Value))
                        {
                            OnRelayCodeReady?.Invoke(code.Value);
                            _currentLobby = null;
                            break;
                        }
                    }
                }
                catch
                {
                    Debug.LogWarning("Lobby polling stopped");
                    _currentLobby = null;
                    break;
                }
            }
        }

        #endregion
    }
}