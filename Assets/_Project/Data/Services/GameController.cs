using System.Linq;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
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
        }

        public async UniTask StartGame()
        {
            _gameState.UpdateMoves(LocalPlayerId, _movesLeft);
            _gameState.UpdateScore(LocalPlayerId, _totalScore);

            _boardFactory.CreateRandom();
            await ProcessBoard(isInitial: true);
        }

        public async UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            if (_movesLeft <= 0) return;

            if (!_matchBoard.TrySwap(from, to))
            {
                await _previewManager.ResetPreview();
                return;
            }

            _movesLeft--;
            _gameState.UpdateMoves(LocalPlayerId, _movesLeft);

            await _previewManager.ConfirmPreview();

            await ProcessBoard();
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

                if (!isInitial)
                {
                    int turnScore = matches.Sum(m => m.Score);
                    _totalScore += turnScore;
                    _gameState.UpdateScore(LocalPlayerId, _totalScore);
                }

                var movements = _matchBoard.ApplyGravity();

                await UniTask.WhenAll(
                    _boardView.PlayDestroy(destroyed),
                    _boardView.PlayGravity(movements, 0)
                );

                currentCombo++;
                matches = _matchBoard.FindMatches();
            }

            _gameState.SetPhase(GamePhase.Playing);
        }
    }
}