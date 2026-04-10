using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Data.Factories;
using Infrastructure.Network;
using Presentation.Views;
using Unity.Netcode;
using UnityEngine;

namespace Data.Services
{
    public class ClientGameController : IGameController
    {
        private readonly IBoardView _boardView;
        private readonly IBoardFactory _boardFactory;
        private readonly IFruitFactory _fruitFactory;
        private readonly PreviewManager _previewManager;
        private readonly IGameStateService _gameState;
        private readonly NetworkGameManager _network;
        private readonly HintSystem _hint;

        private bool _isMyTurn;
        private bool _isLocalPredicting;
        private Vector2Int _pendingFrom;
        private Vector2Int _pendingTo;

        private readonly Queue<Func<UniTask>> _animationQueue = new();
        private bool _isProcessingQueue;

        private string _localPlayerId => NetworkManager.Singleton.LocalClientId.ToString();

        public ClientGameController(IFruitFactory fruitFactory, IBoardFactory boardFactory, IBoardView boardView, PreviewManager previewManager, IGameStateService gameState, NetworkGameManager network)
        {
            _fruitFactory = fruitFactory;
            _boardFactory = boardFactory;
            _boardView = boardView;
            _previewManager = previewManager;
            _gameState = gameState;
            _network = network;

            _hint = new HintSystem(_boardView);

            _network.OnBoardDataReceived += OnBoardDataReceived;

            _network.OnStatsReceived += (playerId, score, moves) =>
            {
                _gameState.UpdateScore(playerId, score);
                _gameState.UpdateMoves(playerId, moves);
            };

            _network.OnTurnChanged += (id, hintFrom, hintTo) =>
            {
                _isMyTurn = id == _localPlayerId;
                _gameState.SetPhase(_isMyTurn ? GamePhase.Playing : GamePhase.Paused);

                if (_isMyTurn) _hint.OnTurnStarted(hintFrom, hintTo);
                else _hint.OnTurnEnded();
            };

            _network.OnGameSettingsReceived += fruitCount =>
                _fruitFactory?.SetFruitTypeCount(fruitCount);

            _network.OnShuffleReceived += movements =>
                Enqueue(() => _boardView.PlayShuffle(movements));

            _network.OnSwapReceived += OnSwapReceived;
            _network.OnSwapFailed += () =>
            {
                _isLocalPredicting = false;
                _previewManager.ResetPreview().Forget();
            };
            _network.OnMatchesProcessed += destroyed => Enqueue(() => _boardView.PlayDestroy(destroyed));
            _network.OnGravityApplied += movements => Enqueue(() => _boardView.PlayGravity(movements, 0));

            _network.OnGameEnded += winnerId =>
            {
                string myId = _localPlayerId;
                bool iWon = winnerId == myId;
                bool isDraw = string.IsNullOrEmpty(winnerId);
                var me = _gameState.GetPlayerData(myId);
                _gameState.NotifyGameFinished(me.Score);
            };
        }

        private void OnBoardDataReceived(int shapeIndex, int seed)
        {
            if (NetworkManager.Singleton.IsHost) return;
            InitializeBoardAsync(shapeIndex, seed).Forget();
        }

        private async UniTaskVoid InitializeBoardAsync(int shapeIndex, int seed)
        {
            _boardFactory.CreateByShape(shapeIndex, seed);
            await UniTask.WaitUntil(() => _boardView.IsInitialized);
            _network.SendBoardReadyServerRpc(_localPlayerId);
        }

        public async UniTask StartGame() { _gameState.SetPhase(GamePhase.Lobby); await UniTask.CompletedTask; }

        public UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (!_isMyTurn || _isProcessingQueue)
                return UniTask.CompletedTask;

            _hint.OnPlayerActed();

            _isLocalPredicting = true;
            _pendingFrom = from;
            _pendingTo = to;

            _network.SendMoveServerRpc(from, to, _localPlayerId);

            return UniTask.CompletedTask;
        }

        private void OnSwapReceived(Vector2Int from, Vector2Int to)
        {
            Enqueue(async () =>
            {
                if (_isLocalPredicting && from == _pendingFrom && to == _pendingTo)
                {
                    _isLocalPredicting = false;
                    await _previewManager.ConfirmPreview();
                    return;
                }

                await _boardView.PlaySwap(from, to);
            });
        }

        private void Enqueue(Func<UniTask> action)
        {
            _animationQueue.Enqueue(action);
            if (!_isProcessingQueue) ProcessQueue().Forget();
        }

        private async UniTaskVoid ProcessQueue()
        {
            if (_isProcessingQueue) return;
            _isProcessingQueue = true;

            while (_animationQueue.Count > 0)
            {
                await UniTask.WaitUntil(() => _boardView.IsInitialized);
                var next = _animationQueue.Dequeue();
                await next();
            }

            _isProcessingQueue = false;
        }
    }
}