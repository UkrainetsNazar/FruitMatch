using System.Collections.Generic;
using System.Linq;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.ValueObjects;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Audio;
using Presentation.Pool;
using Presentation.Views;
using UnityEngine;

namespace Presentation.Utils
{
    public class BoardAnimator
    {
        private readonly FruitViewRegistry _registry;
        private readonly BoardSpawner _spawner;
        private readonly BoardViewUtils _viewUtils;
        private readonly IMatchBoard _matchBoard;
        private readonly ScorePopupPool _scorePopupPool;

        public BoardAnimator(FruitViewRegistry registry, BoardSpawner spawner,
            BoardViewUtils viewUtils, IMatchBoard matchBoard, ScorePopupPool scorePopupPool)
        {
            _registry = registry;
            _spawner = spawner;
            _viewUtils = viewUtils;
            _matchBoard = matchBoard;
            _scorePopupPool = scorePopupPool;
        }

        public async UniTask PlaySwap(Vector2Int from, Vector2Int to)
        {
            if (!_registry.TryGet(from, out var viewFrom)) return;
            if (!_registry.TryGet(to, out var viewTo)) return;

            AudioManager.PlayFruitSwap();

            await UniTask.WhenAll(
                viewFrom.Animator.AnimateSwap(_viewUtils.GridToWorld(to)),
                viewTo.Animator.AnimateSwap(_viewUtils.GridToWorld(from))
            );

            _registry.Swap(from, to);
        }

        public async UniTask PlayDestroy(List<Vector2Int> positions, int score = 0)
        {
            var tasks = new List<UniTask>();
            foreach (var pos in positions)
            {
                if (!_registry.TryGet(pos, out var view)) continue;
                _registry.Remove(pos);
                tasks.Add(AnimateAndReturn(view));
            }

            if (score > 0 && positions.Count > 0)
            {
                var center = Vector3.zero;
                foreach (var pos in positions)
                    center += (Vector3)_viewUtils.GridToWorld(pos);
                center /= positions.Count;

                _scorePopupPool.Get().Play(center, score);
            }

            await UniTask.WhenAll(tasks);
        }

        public async UniTask PlayGravity(List<FruitMovement> movements, int startDelayMs)
        {
            if (startDelayMs > 0) await UniTask.Delay(startDelayMs);

            var tasks = new List<UniTask>();
            var boardHeight = _matchBoard.CurrentBoard.Height;

            foreach (var move in movements.Where(m => m.From.y < boardHeight))
            {
                if (!_registry.TryGet(move.From, out var view)) continue;
                _registry.Remove(move.From);
                _registry.Set(move.To, view);
                tasks.Add(view.Animator.AnimateFall(BuildWorldPath(move.Path)));
            }

            foreach (var column in movements.Where(m => m.From.y >= boardHeight)
                         .GroupBy(m => m.From.x))
            {
                tasks.Add(SpawnColumnSequential(column.OrderBy(m => m.To.y).ToList()));
            }

            await UniTask.WhenAll(tasks);
        }

        public async UniTask PlayShuffle(List<FruitMovement> spawnMovements)
        {
            await PlayDestroy(_registry.AllPositions());
            await PlayGravity(spawnMovements, 0);
        }

        private async UniTask SpawnColumnSequential(List<FruitMovement> column)
        {
            var tasks = new List<UniTask>();
            foreach (var move in column)
            {
                var fruit = move.SyncFruitType >= 0
                    ? new Fruit((FruitType)move.SyncFruitType)
                    : _matchBoard.CurrentBoard.GetCell(move.To.x, move.To.y).Fruit;

                if (fruit == null) continue;

                var view = _spawner.SpawnFruitView(move.To, fruit);
                view.transform.position = _viewUtils.GridToWorld(move.From);
                tasks.Add(view.Animator.AnimateFall(BuildWorldPath(move.Path)));
                await UniTask.Delay(60);
            }
            await UniTask.WhenAll(tasks);
        }

        private async UniTask AnimateAndReturn(FruitView view)
        {
            AudioManager.PlayFruitDestroy();
            await view.Animator.AnimateDestroy();
            _spawner.ReturnToPool(view);
        }

        private List<Vector2> BuildWorldPath(List<Vector2Int> path) =>
            path.ConvertAll(p => _viewUtils.GridToWorld(p));
    }
}