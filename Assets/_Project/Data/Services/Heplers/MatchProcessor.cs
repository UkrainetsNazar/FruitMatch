using System.Collections.Generic;
using System.Linq;
using Core.Domain.ValueObjects;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;

namespace Data.Services
{
    public class MatchProcessor
    {
        private readonly IMatchBoard _matchBoard;
        private readonly IBoardView _boardView;
        private readonly IGameStateService _gameState;
        private readonly NetworkGameManager _network;

        public MatchProcessor(IMatchBoard matchBoard, IBoardView boardView,
            IGameStateService gameState, NetworkGameManager network)
        {
            _matchBoard = matchBoard;
            _boardView = boardView;
            _gameState = gameState;
            _network = network;
        }

        public async UniTask<int> ProcessCascade(string currentPlayerId, bool countScore)
        {
            int currentCombo = 1;
            int totalScore = 0;
            var matches = _matchBoard.FindMatches();

            while (matches.Count > 0)
            {
                var destroyed = matches.SelectMany(m => m.MatchedPositions).Distinct().ToList();
                _matchBoard.ProcessMatches(matches, currentCombo);

                int stepScore = 0;
                if (countScore)
                {
                    stepScore = matches.Sum(m => m.Score);
                    totalScore += stepScore;
                    _gameState.UpdateScore(currentPlayerId, stepScore);
                }

                var movements = _matchBoard.ApplyGravity();

                _network.BroadcastMatchesClientRpc(destroyed.ToArray(), stepScore);
                _network.BroadcastGravityClientRpc(ToNetworkData(movements));

                await _boardView.PlayDestroy(destroyed, stepScore);
                await _boardView.PlayGravity(movements, 0);

                if (countScore && currentCombo > 1)
                {
                    _gameState.NotifyCombo(currentPlayerId, currentCombo);
                    _network.BroadcastComboClientRpc(currentPlayerId, currentCombo);
                }

                currentCombo++;
                matches = _matchBoard.FindMatches();
            }

            return totalScore;
        }

        private FruitMovementData[] ToNetworkData(List<FruitMovement> movements) =>
            movements.Select(m => new FruitMovementData
            {
                From = m.From,
                To = m.To,
                Path = m.Path.ToArray(),
                NewFruitType = m.SyncFruitType
            }).ToArray();
    }
}