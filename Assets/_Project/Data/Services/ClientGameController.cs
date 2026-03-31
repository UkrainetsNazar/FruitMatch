using System;
using System.Collections.Generic;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using Unity.Netcode;
using UnityEngine;

namespace Data.Services
{
    public class ClientGameController : IGameController
    {
        private readonly IMatchBoard _matchBoard;
        private readonly IBoardView _boardView;
        private readonly IBoardFactory _boardFactory;
        private readonly PreviewManager _previewManager;
        private readonly IGameStateService _gameState;
        private readonly NetworkGameManager _network;

        private bool _isMyTurn;
        private bool _isLocalPredicting;
        private Vector2Int _pendingFrom;
        private Vector2Int _pendingTo;

        private readonly Queue<Func<UniTask>> _animationQueue = new();
        private bool _isProcessingQueue;

        private ulong _localPlayerId => NetworkManager.Singleton.LocalClientId;

        public ClientGameController(IMatchBoard matchBoard, IBoardFactory boardFactory, IBoardView boardView, PreviewManager previewManager, IGameStateService gameState, NetworkGameManager network)
        {
            _matchBoard = matchBoard;
            _boardFactory = boardFactory;
            _boardView = boardView;
            _previewManager = previewManager;
            _gameState = gameState;
            _network = network;

            _network.OnBoardDataReceived += OnBoardDataReceived;
            _network.OnGameStarted += () => Debug.Log("Game Started on Client");

            _network.OnTurnChanged += id =>
            {
                _isMyTurn = id == _localPlayerId;
                _gameState.SetPhase(_isMyTurn ? GamePhase.Playing : GamePhase.Paused);
            };

            _network.OnSwapReceived += OnSwapReceived;
            _network.OnMatchesProcessed += destroyed => Enqueue(() => _boardView.PlayDestroy(destroyed));
            _network.OnGravityApplied += movements => Enqueue(() => _boardView.PlayGravity(movements, 0));
        }

        private void OnBoardDataReceived(int shapeIndex, int seed)
        {
            Debug.Log($"Client: Отримано дані дошки. Seed: {seed}");
            UnityEngine.Random.InitState(seed);

            _animationQueue.Clear();
            _isProcessingQueue = false;

            _boardFactory.CreateRandom();
        }

        public async UniTask StartGame() { _gameState.SetPhase(GamePhase.Lobby); await UniTask.CompletedTask; }

        public UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (!_isMyTurn || _isProcessingQueue) return UniTask.CompletedTask;

            if (!_matchBoard.TrySwap(from, to)) return UniTask.CompletedTask;

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

                _matchBoard.TrySwap(from, to);
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
            _isProcessingQueue = true;
            while (_animationQueue.Count > 0)
            {
                var next = _animationQueue.Dequeue();
                await next();
            }
            _isProcessingQueue = false;
        }

        public UniTask ProcessBoard() => UniTask.CompletedTask;
    }
}