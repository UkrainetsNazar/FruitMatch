using System;
using Core.Domain;

namespace Core.Interfaces
{
    public interface IGameStateService
    {
        GamePhase CurrentPhase { get; }
        PlayerData LocalPlayer { get; }

        event Action<GamePhase> OnPhaseChanged;
        event Action OnDataUpdated;
        event Action<int> OnGameFinished;

        void NotifyGameFinished(int finalScore);
        void SetPhase(GamePhase phase);
        PlayerData GetPlayerData(string playerId);
        void UpdateScore(string playerId, int delta);
        void UpdateMoves(string playerId, int moves);
        void SetLocalPlayer(string playerId, string playerName);
    }
}