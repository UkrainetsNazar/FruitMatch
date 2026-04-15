using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using Infrastructure.Network;
using Unity.Netcode;

namespace Data.Services
{
    public class ScoreTracker
    {
        private readonly NetworkGameManager _network;
        private readonly IGameStateService _gameState;
        private readonly Dictionary<string, int> _scores = new();

        private int _hostMoves = 20;
        private int _clientMoves = 20;

        private string _localPlayerId => NetworkManager.Singleton.LocalClientId.ToString();

        public ScoreTracker(NetworkGameManager network, IGameStateService gameState)
        {
            _network = network;
            _gameState = gameState;
        }

        public void Initialize(IEnumerable<string> playerIds)
        {
            foreach (var id in playerIds)
            {
                _scores[id] = 0;
                _gameState.UpdateScore(id, 0);
                _gameState.UpdateMoves(id, 20);
                _network.UpdatePlayerStatsClientRpc(id, 0, 20);
            }
        }

        public void AddScore(string playerId, int score)
        {
            if (!_scores.ContainsKey(playerId)) _scores[playerId] = 0;
            _scores[playerId] += score;
        }

        public void DecrementMoves(string playerId)
        {
            if (playerId == _localPlayerId) _hostMoves--;
            else _clientMoves--;
        }

        public int GetMoves(string playerId) =>
            playerId == _localPlayerId ? _hostMoves : _clientMoves;

        public bool IsGameOver() => _hostMoves <= 0 && _clientMoves <= 0;

        public string DetermineWinner()
        {
            int hostScore = _scores.GetValueOrDefault(_localPlayerId, 0);
            string clientId = _scores.Keys.FirstOrDefault(id => id != _localPlayerId);
            int clientScore = clientId != null ? _scores.GetValueOrDefault(clientId, 0) : 0;

            if (hostScore > clientScore) return _localPlayerId;
            if (clientScore > hostScore) return clientId;
            return string.Empty;
        }

        public void SyncToNetwork(string playerId)
        {
            _network.UpdatePlayerStatsClientRpc(playerId, _scores[playerId], GetMoves(playerId));
            _gameState.UpdateScore(playerId, _scores[playerId]);
            _gameState.UpdateMoves(playerId, GetMoves(playerId));
        }
    }
}

/*

Core/
-Domain/
--Board.cs
--Cell.cs
--Fruit.cs
--FruitMovement.cs
--FruitType.cs
--GamePhase.cs
--LobbySettings.cs
--MatchResult.cs
--PlayerData.cs
-Interfaces/
--IBoardFactory.cs
--IBoardView.cs
--IFruitFactory.cs
--IGameController.cs
--IGameStateService.cs
--IMatchBoard.cs
Data/
-Config/
--BoardShapeConfig.cs
-Factories/
--BoardFactory.cs
--FruitFactory.cs
-Services/
--Game/
---MatchBoard.cs
---MultiplayerClientController.cs
---MultiplayerHostController.cs
---SinglePlayerController.cs
--Heplers/
---MatchFinder.cs
---MatchProcessor.cs
---GravityProcessor.cs
---ScoreTracker.cs
---TurnManager.cs
---GameStateService.cs
Infrastructure/
-Addressables/
-Audio/
--AudioManager.cs
--VolumeSliderBinder.cs
-Network/
--AuthManager.cs
--LobbyManager.cs
--NetworkGameManager.cs
--NetworkService.cs
--RelayManager.cs
Presentation/
-Animations/
--FruitAnimator.cs
-Config/
--FruitConfig.cs
-EntryPoint/
--GameEntryPoint.cs
--MainMenuEntryPoint.cs
-Installers/
--GameInstaller.cs
--LobbyInstaller.cs
--ProjectInstaller.cs
-Input/
--InputHandler.cs
-Pool/
--FruitPool.cs
--ScorePopupPool.cs
-UI/
--BaseGameUI.cs
--LobbyUI.cs
--MainMenuUI.cs
--OnlineGameUI.cs
--SingleGameUI.cs
--PauseUI.cs
-Utils/
--BoardAnimator.cs
--BoardSpawner.cs
--BoardViewUtils.cs
--FruitViewRegistry.cs
--HintSystem.cs
--PreviewManager.cs
-Views/
--BoardView.cs
--FruitView.cs
--ScorePopup.cs

*/