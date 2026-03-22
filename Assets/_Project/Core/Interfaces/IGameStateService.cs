using System;
using Core.Domain;

public interface IGameStateService
{
    GamePhase CurrentPhase { get; }
    PlayerData LocalPlayer { get; }

    event Action<GamePhase> OnPhaseChanged;

    void SetPhase(GamePhase phase);
    void UpdateScore(string playerId, int delta);
}