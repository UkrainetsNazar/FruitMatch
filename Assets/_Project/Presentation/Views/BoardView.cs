using System.Collections.Generic;
using System.Linq;
using Core.Domain;
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
        private Vector2 _startPos;

        private void Start()
        {
            // Listening initialization of board
            _matchBoard.OnBoardInitialized += BuildBoard;
            // Listening which fruits have to be destroyed
            _matchBoard.OnMatchesProcessed += OnMatchesProcessed;
            // Listening how to move our fruits on board
            _matchBoard.OnGravityApplied += OnGravityApplied;
        }

        private void OnDestroy()
        {
            _matchBoard.OnBoardInitialized -= BuildBoard;
            _matchBoard.OnMatchesProcessed -= OnMatchesProcessed;
            _matchBoard.OnGravityApplied -= OnGravityApplied;
        }

        private void BuildBoard()
        {
            var board = _matchBoard.CurrentBoard;

            float totalWidth = board.Width * (_cellSize + _cellSpacing) - _cellSpacing;
            float totalHeight = board.Height * (_cellSize + _cellSpacing) - _cellSpacing;

            _startPos = new Vector2(
                -totalWidth / 2f + _cellSize / 2f,
                -totalHeight / 2f + _cellSize / 2f
            );

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable) continue;

                    var worldPos = new Vector3(
                        _startPos.x + x * (_cellSize + _cellSpacing),
                        _startPos.y + y * (_cellSize + _cellSpacing),
                        0f
                    );

                    SpawnVisualCell(x, y, worldPos);

                    if (cell.Fruit != null)
                        SpawnFruitView(new Vector2Int(x, y), cell.Fruit);
                }
            }
        }

        private void SpawnVisualCell(int x, int y, Vector3 worldPos)
        {
            var prefab = (x + y) % 2 == 0 ? _brightCellPrefab : _darkCellPrefab;
            var cellGO = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            cellGO.name = $"Cell ({x},{y})";
        }

        private void OnMatchesProcessed(List<Vector2Int> destroyedPositions)
        {
            foreach (var pos in destroyedPositions)
            {
                if (_fruitView.TryGetValue(pos, out var view))
                {
                    _pool.Return(view);
                    _fruitView.Remove(pos);
                }
            }
        }

        private void OnGravityApplied(List<FruitMovement> movements)
        {
            foreach (var movement in movements)
            {
                if (_fruitView.ContainsKey(movement.From))
                {
                    var fruitView = _fruitView[movement.From];
                    _fruitView.Remove(movement.From);
                    _fruitView[movement.To] = fruitView;

                    fruitView.transform.position = GridToWorld(movement.To.x, movement.To.y);
                }
                else
                {
                    var cell = _matchBoard.CurrentBoard.GetCell(movement.To.x, movement.To.y);
                    SpawnFruitView(movement.To, cell.Fruit);
                }
            }
        }

        private void SpawnFruitView(Vector2Int gridPos, Fruit fruit)
        {
            var view = _pool.Get();
            view.transform.position = GridToWorld(gridPos.x, gridPos.y);
            view.transform.SetParent(transform);
            view.Setup(fruit, _fruitConfig.GetSprite(fruit.Type));
            _fruitView[gridPos] = view;
        }

        private Vector3 GridToWorld(int x, int y) =>
            new(
                _startPos.x + x * (_cellSize + _cellSpacing),
                _startPos.y + y * (_cellSize + _cellSpacing),
                0f
            );
    }
}