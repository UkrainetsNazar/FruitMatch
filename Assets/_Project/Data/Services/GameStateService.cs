using System;
using Core.Domain;
using Core.Interfaces;

namespace Data.Services
{
    public class GameStateService : IGameStateService
    {
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Lobby;
        public PlayerData LocalPlayer { get; private set; }

        public event Action<GamePhase> OnPhaseChanged;

        public GameStateService()
        {
            LocalPlayer = new PlayerData();
        }

        public void SetPhase(GamePhase phase)
        {
            if (CurrentPhase == phase) return;

            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        public void UpdateScore(string playerId, int delta)
        {
            if (LocalPlayer.PlayerId == playerId)
                LocalPlayer.Score += delta;
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
    }
}