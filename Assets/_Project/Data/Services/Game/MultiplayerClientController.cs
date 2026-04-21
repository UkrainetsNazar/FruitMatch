using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain.Enums;
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
            {
                Enqueue(async () =>
                {
                    _matchBoard.SyncGravity(movements);
                    await _boardView.PlayShuffle(movements);
                });
            };

            _network.OnSwapFailed += () =>
            {
                _isLocalPredicting = false;
                _previewManager.ResetPreview().Forget();
            };

            _network.OnComboReceived += (playerId, combo) =>
                _gameState.NotifyCombo(playerId, combo);

            _network.OnGravityApplied += movements =>
            {
                Enqueue(async () =>
                {
                    _matchBoard.SyncGravity(movements);
                    await _boardView.PlayGravity(movements, 0);
                });
            };

            _network.OnMatchesProcessed += (destroyed, score) =>
            {
                if (_isLocalPredicting)
                    return;

                Enqueue(async () =>
                {
                    UpdateBoardData(destroyed);
                    await _boardView.PlayDestroy(destroyed, score);
                });
            };

            _network.OnSwapReceived += (from, to) =>
            {
                Enqueue(async () =>
                {
                    if (_isLocalPredicting && from == _pendingFrom && to == _pendingTo)
                    {
                        _isLocalPredicting = false;
                        return;
                    }

                    if (!_isMyTurn)
                    {
                        _matchBoard.ForceSwap(from, to);
                        await _boardView.PlaySwap(from, to);
                    }
                });
            };

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
            _network.SendBoardReadyServerRpc();
        }

        public async UniTask StartGame() { _gameState.SetPhase(GamePhase.Lobby); await UniTask.CompletedTask; }

        public async UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (!_isMyTurn || _isProcessingQueue) return;

            if (_matchBoard.TrySwap(from, to))
            {
                _isLocalPredicting = true;
                _pendingFrom = from;
                _pendingTo = to;

                _network.SendMoveServerRpc(from, to, _localPlayerId);

                await UniTask.WaitUntil(() => !_previewManager.IsAnimating);

                await _previewManager.ConfirmPreview();

                Enqueue(async () => await ExecuteFullTurnLocal(from, to));
            }
        }

        private async UniTask ExecuteFullTurnLocal(Vector2Int from, Vector2Int to)
        {
            var matches = _matchBoard.FindMatches();
            if (matches.Count > 0)
            {
                var destroyed = matches.SelectMany(m => m.MatchedPositions).Distinct().ToList();
                int score = matches.Sum(m => m.Score);

                _matchBoard.ProcessMatches(matches, 1);
                UpdateBoardData(destroyed);

                await _boardView.PlayDestroy(destroyed, score);
            }
        }

        private void Enqueue(Func<UniTask> action)
        {
            _animationQueue.Enqueue(action);
            if (!_isProcessingQueue) ProcessQueue().Forget();
        }

        private void UpdateBoardData(List<Vector2Int> destroyed)
        {
            foreach (var pos in destroyed)
                _matchBoard.CurrentBoard.GetCell(pos.x, pos.y).Fruit = null;
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