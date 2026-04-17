using Core.Domain.Enums;
using Core.Interfaces;
using Infrastructure.Network;
using UnityEngine;
using Zenject;

namespace Presentation.EntryPoints
{
    public class MainMenuEntryPoint : MonoBehaviour
    {
        [Inject] private AuthManager _authManager;
        [Inject] private IGameStateService _gameState;

        private async void Start()
        {
            await _authManager.InitializeAsync();

            if (string.IsNullOrEmpty(_authManager.PlayerId))
                return;

            _gameState.SetLocalPlayer(
                _authManager.PlayerId,
                $"Player_{_authManager.PlayerId[..4]}"
            );

            _gameState.SetPhase(GamePhase.Lobby);
        }
    }
}