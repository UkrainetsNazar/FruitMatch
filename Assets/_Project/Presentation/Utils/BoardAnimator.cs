using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
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

        public BoardAnimator(FruitViewRegistry registry, BoardSpawner spawner,
            BoardViewUtils viewUtils, IMatchBoard matchBoard)
        {
            _registry = registry;
            _spawner = spawner;
            _viewUtils = viewUtils;
            _matchBoard = matchBoard;
        }

        public async UniTask PlaySwap(Vector2Int from, Vector2Int to)
        {
            if (!_registry.TryGet(from, out var viewFrom)) return;
            if (!_registry.TryGet(to, out var viewTo)) return;

            await UniTask.WhenAll(
                viewFrom.Animator.AnimateSwap(_viewUtils.GridToWorld(to)),
                viewTo.Animator.AnimateSwap(_viewUtils.GridToWorld(from))
            );

            _registry.Swap(from, to);
        }

        public async UniTask PlayDestroy(List<Vector2Int> positions)
        {
            var tasks = new List<UniTask>();
            foreach (var pos in positions)
            {
                if (!_registry.TryGet(pos, out var view)) continue;
                _registry.Remove(pos);
                tasks.Add(AnimateAndReturn(view));
            }
            await UniTask.WhenAll(tasks);
            await UniTask.Delay(50);
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
            await UniTask.Delay(100);
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
            await view.Animator.AnimateDestroy();
            _spawner.ReturnToPool(view);
        }

        private List<Vector2> BuildWorldPath(List<Vector2Int> path) =>
            path.ConvertAll(p => _viewUtils.GridToWorld(p));
    }
}