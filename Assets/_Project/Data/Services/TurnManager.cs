using System.Linq;
using Core.Interfaces;
using Infrastructure.Network;
using Presentation.Views;
using Unity.Netcode;
using UnityEngine;

namespace Data.Services
{
    public class TurnManager
    {
        private readonly NetworkGameManager _network;
        private readonly IMatchBoard _matchBoard;

        private string _currentTurnPlayerId;
        private string _localPlayerId => NetworkManager.Singleton.LocalClientId.ToString();

        public string CurrentTurnPlayerId => _currentTurnPlayerId;
        public bool IsMyTurn => _currentTurnPlayerId == _localPlayerId;

        public TurnManager(NetworkGameManager network, IMatchBoard matchBoard)
        {
            _network = network;
            _matchBoard = matchBoard;
        }

        public void SetInitialTurn(string playerId, HintSystem hint)
        {
            _currentTurnPlayerId = playerId;
            BroadcastTurn(hint);
        }

        public void SwitchTurn(HintSystem hint)
        {
            var clients = NetworkManager.Singleton.ConnectedClients.Keys.ToList();
            _currentTurnPlayerId = clients
                .First(id => id.ToString() != _currentTurnPlayerId)
                .ToString();

            BroadcastTurn(hint);
        }

        private void BroadcastTurn(HintSystem hint)
        {
            var hintPair = _matchBoard.FindHint();
            var hintFrom = hintPair?.Item1 ?? Vector2Int.zero;
            var hintTo = hintPair?.Item2 ?? Vector2Int.zero;

            _network.BroadcastTurnClientRpc(_currentTurnPlayerId, hintFrom, hintTo);
        }
    }
}