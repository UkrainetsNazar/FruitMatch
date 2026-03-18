using System.Collections.Generic;
using Core.Interfaces;
using Presentation.Config;
using Presentation.Pool;
using UnityEngine;
using Zenject;

namespace Presentation.Views
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private GameObject _brightCellPrefab;
        [SerializeField] private GameObject _darkCellPrefab;
        [SerializeField] private FruitPool _pool;
        [SerializeField] private FruitConfig _fruitConfig;
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField] private float _cellSpacing = 0.1f;

        [Inject] private IMatchBoard _matchBoard;

        private Dictionary<Vector2Int, FruitView> _fruitView = new();

        private void Start()
        {
            _matchBoard.OnBoardInitialized += BuildBoard;
        }

        private void OnDestroy()
        {
            _matchBoard.OnBoardInitialized -= BuildBoard;
        }

        private void BuildBoard()
        {
            var board = _matchBoard.CurrentBoard;

            float totalWidth = board.Width * (_cellSize + _cellSpacing) - _cellSpacing;
            float totalHeight = board.Height * (_cellSize + _cellSpacing) - _cellSpacing;

            var startPos = new Vector2(
                -totalWidth / 2f + _cellSize / 2f,
                -totalHeight / 2f + _cellSize / 2f
            );

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = board.GetCell(new Vector2Int(x, y));
                    if (!cell.IsUsable) continue;

                    var worldPos = new Vector3(
                        startPos.x + x * (_cellSize + _cellSpacing),
                        startPos.y + y * (_cellSize + _cellSpacing),
                        0f
                    );

                    SpawnVisualCell(x, y, worldPos);

                    if (cell.Fruit != null)
                    {
                        var fruitViewFromPool = _pool.Get();
                        fruitViewFromPool.transform.position = worldPos;
                        fruitViewFromPool.transform.SetParent(transform);
                        fruitViewFromPool.Setup(cell.Fruit, _fruitConfig.GetSprite(cell.Fruit.Type));
                        _fruitView[new Vector2Int(x, y)] = fruitViewFromPool;
                    }
                }
            }
        }

        private void SpawnVisualCell(int x, int y, Vector3 worldPos)
        {
            var prefab = (x + y) % 2 == 0 ? _brightCellPrefab : _darkCellPrefab;
            var cellGO = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            cellGO.name = $"Cell ({x},{y})";
        }
    }
}