using System;
using Core.Domain;

public class GameStateService : IGameStateService
{
    public GamePhase CurrentPhase { get; private set; }
    public PlayerData LocalPlayer { get; private set; }

    public event Action<GamePhase> OnPhaseChanged;

    public GameStateService()
    {
        LocalPlayer = new PlayerData { PlayerName = "Player" };
        CurrentPhase = GamePhase.Lobby;
    }

    public void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    public void UpdateScore(string playerId, int delta)
    {
        if (LocalPlayer.PlayerId == playerId)
            LocalPlayer.Score += delta;
    }
}