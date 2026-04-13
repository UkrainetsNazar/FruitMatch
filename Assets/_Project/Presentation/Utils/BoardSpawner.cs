using Core.Domain;
using Presentation.Config;
using Presentation.Pool;
using Presentation.Views;
using UnityEngine;

namespace Presentation.Utils
{
    public class BoardSpawner
    {
        private readonly GameObject _brightCellPrefab;
        private readonly GameObject _darkCellPrefab;
        private readonly FruitPool _fruitPool;
        private readonly FruitConfig _fruitConfig;
        private readonly BoardViewUtils _viewUtils;
        private readonly FruitViewRegistry _registry;
        private readonly Transform _parent;

        public BoardSpawner(GameObject bright, GameObject dark, FruitPool fruitPool,
            FruitConfig config, BoardViewUtils viewUtils, FruitViewRegistry registry, Transform parent)
        {
            _brightCellPrefab = bright;
            _darkCellPrefab = dark;
            _fruitPool = fruitPool;
            _fruitConfig = config;
            _viewUtils = viewUtils;
            _registry = registry;
            _parent = parent;
        }

        public void BuildBoard(Board board)
        {
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable) continue;
                    SpawnCell(x, y);
                    if (cell.Fruit != null)
                        SpawnFruitView(new Vector2Int(x, y), cell.Fruit);
                }
        }

        public FruitView SpawnFruitView(Vector2Int gridPos, Fruit fruit)
        {
            var view = _fruitPool.Get();
            view.transform.position = _viewUtils.GridToWorld(gridPos);
            view.transform.SetParent(_parent);
            view.Setup(fruit, _fruitConfig.GetSprite(fruit.Type));
            _registry.Set(gridPos, view);
            return view;
        }

        public void ReturnToPool(FruitView view) => _fruitPool.Return(view);

        private void SpawnCell(int x, int y)
        {
            var prefab = (x + y) % 2 == 0 ? _brightCellPrefab : _darkCellPrefab;
            var go = Object.Instantiate(prefab, _viewUtils.GridToWorld(x, y), Quaternion.identity, _parent);
            go.name = $"Cell ({x},{y})";
        }
    }
}