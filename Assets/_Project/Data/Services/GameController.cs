using Core.Interfaces;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Data.Services
{
    public class GameController : IGameController
    {
        private readonly IMatchBoard _matchBoard;
        private readonly IBoardFactory _boardFactory;
        private readonly IBoardView _boardView;

        public GameController(
            IMatchBoard matchBoard,
            IBoardFactory boardFactory,
            IBoardView boardView)
        {
            _matchBoard = matchBoard;
            _boardFactory = boardFactory;
            _boardView = boardView;
        }

        public async UniTask StartGame()
        {
            _boardFactory.CreateRandom();
            await ProcessBoard();
        }

        public async UniTask ProcessBoard()
        {
            var matches = _matchBoard.FindMatches();

            while (matches.Count > 0)
            {
                var destroyed = matches
                    .SelectMany(m => m.MatchedPositions)
                    .Distinct()
                    .ToList();

                _matchBoard.ProcessMatches(matches);
                var movements = _matchBoard.ApplyGravity();

                await UniTask.WhenAll(
                    _boardView.PlayDestroy(destroyed),
                    _boardView.PlayGravity(movements, 200)
                );

                matches = _matchBoard.FindMatches();
            }
        }

        public UniTask OnPlayerSwap(Vector2Int from, Vector2Int to)
        {
            throw new System.NotImplementedException();
        }
    }
}