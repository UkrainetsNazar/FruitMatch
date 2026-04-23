using System;
using System.Collections.Generic;
using Core.Domain.Enums;
using Core.Domain.ValueObjects;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Network;
using Presentation.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Data.Services
{
    public class MultiplayerClientController : IGameController
    {
        private readonly IBoardView _boardView;
        private readonly IBoardFactory _boardFactory;
        private readonly IFruitFactory _fruitFactory;
        private readonly PreviewManager _previewManager;
        private readonly IGameStateService _gameState;
        private readonly IMatchBoard _matchBoard;
        private readonly NetworkGameManager _network;
        private readonly HintSystem _hint;

        private bool _isMyTurn;
        private bool _isLocalPredicting;
        private Vector2Int _pendingFrom;
        private Vector2Int _pendingTo;

        private readonly Queue<Func<UniTask>> _animationQueue = new();
        private bool _isProcessingQueue;

        private string _localPlayerId => NetworkManager.Singleton.LocalClientId.ToString();

        public MultiplayerClientController(IMatchBoard matchBoard, IFruitFactory fruitFactory, IBoardFactory boardFactory, IBoardView boardView, PreviewManager previewManager, IGameStateService gameState, NetworkGameManager network)
        {
            _matchBoard = matchBoard;
            _fruitFactory = fruitFactory;
            _boardFactory = boardFactory;
            _boardView = boardView;
            _previewManager = previewManager;
            _gameState = gameState;
            _network = network;

            _hint = new HintSystem(_boardView);

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _network.OnBoardDataReceived += HandleBoardDataReceived;
            _network.OnStatsReceived += HandleStatsReceived;
            _network.OnTurnChanged += HandleTurnChanged;
            _network.OnGameSettingsReceived += HandleGameSettings;
            _network.OnShuffleReceived += HandleShuffle;
            _network.OnSwapFailed += HandleSwapFailed;
            _network.OnComboReceived += HandleCombo;
            _network.OnMatchesProcessed += HandleMatchesProcessed;
            _network.OnGravityApplied += HandleGravityApplied;
            _network.OnSwapReceived += HandleSwapReceived;
            _network.OnGameEnded += HandleGameEnded;
        }

        #region Network Handlers

        private void HandleBoardDataReceived(int shapeIndex, int seed)
        {
            if (NetworkManager.Singleton.IsHost) return;
            InitializeBoardAsync(shapeIndex, seed).Forget();
        }

        private void HandleStatsReceived(string playerId, int score, int moves)
        {
            _gameState.UpdateScore(playerId, score);
            _gameState.UpdateMoves(playerId, moves);
        }

        private void HandleTurnChanged(string id, Vector2Int hintFrom, Vector2Int hintTo)
        {
            _isMyTurn = id == _localPlayerId;
            _gameState.SetPhase(_isMyTurn ? GamePhase.Playing : GamePhase.Paused);

            if (_isMyTurn) _hint.OnTurnStarted(hintFrom, hintTo);
            else _hint.OnTurnEnded();
        }

        private void HandleGameSettings(int fruitCount) =>
            _fruitFactory?.SetFruitTypeCount(fruitCount);

        private void HandleShuffle(List<FruitMovement> movements)
        {
            Enqueue(async () =>
            {
                _matchBoard.SyncGravity(movements);
                await _boardView.PlayShuffle(movements);
            });
        }

        private void HandleSwapFailed()
        {
            if (_isLocalPredicting)
            {
                _isLocalPredicting = false;
                _matchBoard.ForceSwap(_pendingTo, _pendingFrom);

                Enqueue(async () => await _previewManager.ResetPreview());
            }
        }

        private void HandleCombo(string playerId, int combo) =>
            _gameState.NotifyCombo(playerId, combo);

        private void HandleMatchesProcessed(List<Vector2Int> destroyed, int score) =>
            Enqueue(() => _boardView.PlayDestroy(destroyed, score));

        private void HandleGravityApplied(List<FruitMovement> movements) =>
            Enqueue(() => _boardView.PlayGravity(movements, 0));

        private void HandleSwapReceived(Vector2Int from, Vector2Int to)
        {
            if (_isLocalPredicting && from == _pendingFrom && to == _pendingTo)
            {
                _isLocalPredicting = false;
                _previewManager.ConfirmPreview().Forget();
                return;
            }

            Enqueue(async () =>
            {
                _matchBoard.ForceSwap(from, to);
                await _boardView.PlaySwap(from, to);
            });
        }

        private void HandleGameEnded(string winnerId)
        {
            string myId = _localPlayerId;
            var me = _gameState.GetPlayerData(myId);
            _gameState.NotifyGameFinished(me.Score);
        }

        #endregion

        #region Infrastructure & Helpers

        private async UniTaskVoid InitializeBoardAsync(int shapeIndex, int seed)
        {
            _boardFactory.CreateByShape(shapeIndex, seed);
            await UniTask.WaitUntil(() => _boardView.IsInitialized);
            _network.SendBoardReadyServerRpc();
        }

        public async UniTask StartGame() { _gameState.SetPhase(GamePhase.Lobby); await UniTask.CompletedTask; }

        public async UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (!_isMyTurn || _isProcessingQueue || _isLocalPredicting) return;

            _hint.OnPlayerActed();

            if (!_matchBoard.TrySwap(from, to))
            {
                await _previewManager.ResetPreview();
                return;
            }

            _isLocalPredicting = true;
            _pendingFrom = from;
            _pendingTo = to;

            _network.SendMoveServerRpc(from, to, _localPlayerId);
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

        #endregion
    }
}