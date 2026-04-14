using System;
using System.Linq;
using Core.Domain;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Audio;
using Presentation.Views;
using UnityEngine;

namespace Data.Services
{
    public class GameController : IGameController
    {
        private readonly IMatchBoard _matchBoard;
        private readonly IBoardFactory _boardFactory;
        private readonly IBoardView _boardView;
        private readonly PreviewManager _previewManager;
        private readonly IGameStateService _gameState;
        private readonly HintSystem _hint;

        private int _movesLeft = 20;
        private int _totalScore = 0;
        private const string LocalPlayerId = "Local";

        public GameController(
            IMatchBoard matchBoard,
            IBoardFactory boardFactory,
            IBoardView boardView,
            PreviewManager previewManager,
            IGameStateService gameState)
        {
            _matchBoard = matchBoard;
            _boardFactory = boardFactory;
            _boardView = boardView;
            _previewManager = previewManager;
            _gameState = gameState;
            _hint = new HintSystem(_boardView);
        }

        public async UniTask StartGame()
        {
            _gameState.UpdateMoves(LocalPlayerId, _movesLeft);
            _gameState.UpdateScore(LocalPlayerId, _totalScore);

            _boardFactory.CreateRandom();
            await ProcessBoard(isInitial: true);

            StartHint();
        }

        public async UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (_movesLeft <= 0) return;

            _hint.OnPlayerActed();

            if (!_matchBoard.TrySwap(from, to))
            {
                await _previewManager.ResetPreview();
                return;
            }

            _movesLeft--;
            _gameState.UpdateMoves(LocalPlayerId, _movesLeft);

            await _previewManager.ConfirmPreview();
            await UniTask.Delay(100);
            await ProcessBoard();

            if (_movesLeft <= 0)
            {
                _gameState.SetPhase(GamePhase.Finished);
                await UniTask.Delay(500);
                _gameState.NotifyGameFinished(_totalScore);
                return;
            }

            StartHint();
        }

        public async UniTask ProcessBoard(bool isInitial = false)
        {
            _gameState.SetPhase(GamePhase.Processing);
            int currentCombo = 1;

            var matches = _matchBoard.FindMatches();

            while (matches.Count > 0)
            {
                var destroyed = matches.SelectMany(m => m.MatchedPositions).Distinct().ToList();
                _matchBoard.ProcessMatches(matches, currentCombo);

                int turnScore = 0;
                if (!isInitial)
                {
                    turnScore = matches.Sum(m => m.Score);
                    _totalScore += turnScore;
                    _gameState.UpdateScore(LocalPlayerId, _totalScore);
                }

                var movements = _matchBoard.ApplyGravity();

                await _boardView.PlayDestroy(destroyed, turnScore);
                await _boardView.PlayGravity(movements, 0);

                if (!isInitial && currentCombo > 1)
                    _gameState.NotifyCombo(LocalPlayerId, currentCombo);

                currentCombo++;
                matches = _matchBoard.FindMatches();
            }

            if (!_matchBoard.HasAnyValidMove())
                await ShuffleBoard();

            _gameState.SetPhase(GamePhase.Playing);
        }

        private async UniTask ShuffleBoard()
        {
            do { _matchBoard.ShuffleBoard(); }
            while (!_matchBoard.HasAnyValidMove());

            var movements = _matchBoard.BuildSpawnMovements();
            await _boardView.PlayShuffle(movements);

            await ProcessBoard(isInitial: true);
        }

        private void StartHint()
        {
            var hint = _matchBoard.FindHint();
            if (hint == null) return;

            var (from, to) = hint.Value;
            _hint.OnTurnStarted(from, to);
        }
    }
}