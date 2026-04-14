using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Interfaces;

namespace Data.Services
{
    public class GameStateService : IGameStateService
    {
        public PlayerData LocalPlayer { get; private set; }
        public GamePhase CurrentPhase { get; private set; }
        private readonly Dictionary<string, PlayerData> _players = new();

        public event Action<GamePhase> OnPhaseChanged;
        public event Action OnDataUpdated;
        public event Action<int> OnGameFinished;
        public event Action<string, int> OnComboAchieved;

        public void NotifyGameFinished(int finalScore)
        {
            OnGameFinished?.Invoke(finalScore);
        }

        public void UpdateScore(string playerId, int score)
        {
            EnsurePlayerExists(playerId);
            _players[playerId].Score = score;
            OnDataUpdated?.Invoke();
        }

        public void UpdateMoves(string playerId, int moves)
        {
            EnsurePlayerExists(playerId);
            _players[playerId].MovesLeft = moves;
            OnDataUpdated?.Invoke();
        }

        public PlayerData GetPlayerData(string playerId)
        {
            EnsurePlayerExists(playerId);
            return _players[playerId];
        }

        private void EnsurePlayerExists(string id)
        {
            if (!_players.ContainsKey(id))
                _players[id] = new PlayerData { PlayerId = id.ToString(), MovesLeft = 20 };
        }

        public void SetPhase(GamePhase phase)
        {
            if (CurrentPhase == phase) return;

            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        public void SetLocalPlayer(string playerId, string playerName)
        {
            LocalPlayer = new PlayerData
            {
                PlayerId = playerId,
                PlayerName = playerName,
                Score = 0
            };
        }

        public void NotifyCombo(string playerId, int combo)
        {
            OnComboAchieved?.Invoke(playerId, combo);
        }
    }
}