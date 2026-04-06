using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using Presentation.Views;
using Unity.Netcode;
using UnityEngine;

namespace Data.Services
{
    public class HostGameController : IGameController
    {
        private readonly IMatchBoard _matchBoard;
        private readonly IBoardView _boardView;
        private readonly IBoardFactory _boardFactory;
        private readonly PreviewManager _previewManager;
        private readonly IGameStateService _gameState;
        private readonly NetworkGameManager _network;
        private readonly HintSystem _hint;

        private int _hostMoves = 20;
        private int _clientMoves = 20;
        private bool _clientBoardReady = false;
        private Dictionary<string, int> _playerScores = new();
        private string _currentTurnPlayerId;
        private string _localPlayerId => NetworkManager.Singleton.LocalClientId.ToString();

        public HostGameController(IMatchBoard matchBoard, IBoardFactory boardFactory, IBoardView boardView, PreviewManager previewManager, IGameStateService gameState, NetworkGameManager network)
        {
            _matchBoard = matchBoard;
            _boardView = boardView;
            _boardFactory = boardFactory;
            _previewManager = previewManager;
            _gameState = gameState;
            _network = network;

            _hint = new HintSystem(_boardView);

            _network.OnMoveReceived += OnMoveReceived;
            _network.OnClientBoardReady += () => _clientBoardReady = true;
        }

        public async UniTask StartGame()
        {
            _gameState.SetPhase(GamePhase.Lobby);
            await WaitForPlayersAsync();

            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                string id = client.Key.ToString();
                _playerScores[id] = 0;
                _gameState.UpdateScore(id, 0);
                _gameState.UpdateMoves(id, 20);
                _network.UpdatePlayerStatsClientRpc(id, 0, 20);
            }

            _boardFactory.CreateRandom(out int shapeIndex, out int seed);

            await UniTask.WaitUntil(() => _boardView.IsInitialized);

            _network.BroadcastBoardDataClientRpc(shapeIndex, seed);
            _network.BroadcastGameStartedClientRpc();

            await UniTask.WaitUntil(() => _clientBoardReady);

            _currentTurnPlayerId = _localPlayerId;
            _network.BroadcastTurnClientRpc(_currentTurnPlayerId, Vector2Int.zero, Vector2Int.zero);

            await ProcessAndBroadcast(isInitialProcess: true);

            _gameState.SetPhase(GamePhase.Playing);
        }

        private async UniTask WaitForPlayersAsync()
        {
            while (NetworkManager.Singleton.ConnectedClients.Count < 2)
                await UniTask.Delay(100);
        }

        public async UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (_currentTurnPlayerId != _localPlayerId) return;
            _hint.OnPlayerActed();

            if (!_matchBoard.TrySwap(from, to))
            {
                await _previewManager.ResetPreview();
                return;
            }

            _network.BroadcastSwapClientRpc(from, to);

            await _boardView.PlaySwap(from, to);

            await ProcessAndBroadcast();
            SwitchTurn();
        }

        private void OnMoveReceived(Vector2Int from, Vector2Int to, string senderId)
        {
            if (_currentTurnPlayerId != senderId) return;
            HandleRemoteMove(from, to, senderId).Forget();
        }

        private async UniTaskVoid HandleRemoteMove(Vector2Int from, Vector2Int to, string senderId)
        {
            if (!_matchBoard.TrySwap(from, to))
            {
                ClientRpcParams rpcParams = new()
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { ulong.Parse(senderId) } }
                };
                _network.NotifySwapFailedClientRpc(rpcParams);
                return;
            }

            _network.BroadcastSwapClientRpc(from, to);
            await _boardView.PlaySwap(from, to);
            await ProcessAndBroadcast();
            SwitchTurn();
        }

        private async UniTask ProcessAndBroadcast(bool isInitialProcess = false)
        {
            _hint.OnTurnEnded();
            _gameState.SetPhase(GamePhase.Processing);

            int currentCombo = 1;
            int totalTurnScore = 0;
            var matches = _matchBoard.FindMatches();

            while (matches.Count > 0)
            {
                var destroyed = matches.SelectMany(m => m.MatchedPositions).Distinct().ToList();

                _matchBoard.ProcessMatches(matches, currentCombo);

                if (!isInitialProcess)
                {
                    int currentStepScore = matches.Sum(m => m.Score);
                    totalTurnScore += currentStepScore;
                    _gameState.UpdateScore(_currentTurnPlayerId, currentStepScore);
                }

                var movements = _matchBoard.ApplyGravity();

                _network.BroadcastMatchesClientRpc(destroyed.ToArray());
                _network.BroadcastGravityClientRpc(ToNetworkData(movements));

                await _boardView.PlayDestroy(destroyed);
                await _boardView.PlayGravity(movements, 0);

                currentCombo++;
                matches = _matchBoard.FindMatches();
            }

            if (isInitialProcess)
            {
                _gameState.SetPhase(GamePhase.Playing);
                return;
            }

            _gameState.SetPhase(GamePhase.Playing);

            if (!_playerScores.ContainsKey(_currentTurnPlayerId))
                _playerScores[_currentTurnPlayerId] = 0;

            _playerScores[_currentTurnPlayerId] += totalTurnScore;

            if (_currentTurnPlayerId == _localPlayerId)
                _hostMoves--;
            else
                _clientMoves--;

            int currentMoves = (_currentTurnPlayerId == _localPlayerId) ? _hostMoves : _clientMoves;

            _network.UpdatePlayerStatsClientRpc(_currentTurnPlayerId, _playerScores[_currentTurnPlayerId], currentMoves);

            _gameState.UpdateScore(_currentTurnPlayerId, _playerScores[_currentTurnPlayerId]);
            _gameState.UpdateMoves(_currentTurnPlayerId, currentMoves);

            if (!isInitialProcess && !_matchBoard.HasAnyValidMove())
                await ShuffleAndBroadcast();
        }

        private void SwitchTurn()
        {
            var clients = NetworkManager.Singleton.ConnectedClients.Keys.ToList();
            ulong nextTurnId = clients.First(id => id.ToString() != _currentTurnPlayerId);
            _currentTurnPlayerId = nextTurnId.ToString();

            bool isMyTurn = _currentTurnPlayerId == _localPlayerId;
            _gameState.SetPhase(isMyTurn ? GamePhase.Playing : GamePhase.Paused);

            var hint = _matchBoard.FindHint();
            var hintFrom = hint?.Item1 ?? Vector2Int.zero;
            var hintTo = hint?.Item2 ?? Vector2Int.zero;

            _network.BroadcastTurnClientRpc(_currentTurnPlayerId, hintFrom, hintTo);

            if (isMyTurn) _hint.OnTurnStarted(hintFrom, hintTo);
            else _hint.OnTurnEnded();
        }

        private async UniTask ShuffleAndBroadcast()
        {
            do { _matchBoard.ShuffleBoard(); }
            while (!_matchBoard.HasAnyValidMove());

            var movements = _matchBoard.BuildSpawnMovements();

            _network.BroadcastShuffleClientRpc(ToNetworkData(movements));

            await _boardView.PlayShuffle(movements);

            await ProcessAndBroadcast(isInitialProcess: true);
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