using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using Unity.Netcode;
using UnityEngine;

namespace Data.Services
{
    public class ClientGameController : IGameController
    {
        private readonly IBoardFactory _boardFactory;
        private readonly IBoardView _boardView;
        private readonly IGameStateService _gameState;
        private readonly NetworkGameManager _network;

        private bool _isMyTurn;
        private readonly Queue<Func<UniTask>> _animationQueue = new();
        private bool _isProcessingQueue;

        private ulong _localPlayerId =>
            NetworkManager.Singleton.LocalClientId;

        public ClientGameController(
            IBoardFactory boardFactory,
            IBoardView boardView,
            IGameStateService gameState,
            NetworkGameManager network)
        {
            _boardFactory = boardFactory;
            _boardView = boardView;
            _gameState = gameState;
            _network = network;

            _network.OnBoardDataReceived += OnBoardDataReceived;
            _network.OnGameStarted += OnGameStarted;
            _network.OnTurnChanged += OnTurnChanged;
            _network.OnSwapReceived += OnSwapReceived;
            _network.OnMatchesProcessed += OnMatchesProcessed;
            _network.OnGravityApplied += OnGravityApplied;
        }

        public async UniTask StartGame()
        {
            _gameState.SetPhase(GamePhase.Lobby);
            await UniTask.CompletedTask;
        }

        public async UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (!_isMyTurn) return;
            _network.SendMoveServerRpc(from, to, _localPlayerId);
            await UniTask.CompletedTask;
        }

        public async UniTask ProcessBoard() => await UniTask.CompletedTask;

        // ── Events ────────────────────────────────────────────

        private void OnBoardDataReceived(int shapeIndex, int seed)
        {
            UnityEngine.Random.InitState(seed);
            _boardFactory.CreateRandom();
        }

        private void OnGameStarted() { }

        private void OnTurnChanged(ulong playerId)
        {
            _isMyTurn = playerId == _localPlayerId;
            _gameState.SetPhase(_isMyTurn ? GamePhase.Playing : GamePhase.Paused);
        }

        private void OnSwapReceived(Vector2Int from, Vector2Int to)
            => Enqueue(() => _boardView.PlaySwap(from, to));

        private void OnMatchesProcessed(List<Vector2Int> destroyed)
            => Enqueue(() => _boardView.PlayDestroy(destroyed));

        private void OnGravityApplied(List<FruitMovement> movements)
            => Enqueue(() => _boardView.PlayGravity(movements, 0));

        // ── Queue ─────────────────────────────────────────────

        private void Enqueue(Func<UniTask> animation)
        {
            _animationQueue.Enqueue(animation);
            if (!_isProcessingQueue)
                ProcessQueue().Forget();
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
    }
}