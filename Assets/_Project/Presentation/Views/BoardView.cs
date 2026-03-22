using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Presentation.Config;
using Presentation.Pool;
using UnityEngine;
using Zenject;

namespace Presentation.Views
{
    public class BoardView : MonoBehaviour, IBoardView
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

        void Start()
        {
            _matchBoard.OnBoardInitialized += BuildBoard;
        }

        void OnDestroy()
        {
            _matchBoard.OnBoardInitialized -= BuildBoard;
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

        public UniTask AnimateSwap(Vector2Int from, Vector2Int to)
        {
            throw new System.NotImplementedException();
        }

        public async UniTask PlayDestroy(List<Vector2Int> positions)
        {
            var tasks = new List<UniTask>();

            foreach (var toDestroy in positions)
            {
                if (_fruitView.TryGetValue(toDestroy, out var view))
                {
                    _fruitView.Remove(toDestroy);
                    tasks.Add(AnimateAndReturn(view));
                }
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask AnimateAndReturn(FruitView view)
        {
            await view.Animator.AnimateDestroy();
            _pool.Return(view);
        }

        public async UniTask PlayGravity(List<FruitMovement> movements, int startDelayMs)
        {
            await UniTask.Delay(startDelayMs);

            var fallTasks = new List<UniTask>();

            foreach (var move in movements)
            {
                if (move.From.y < _matchBoard.CurrentBoard.Height)
                {
                    if (_fruitView.TryGetValue(move.From, out var view))
                    {
                        _fruitView.Remove(move.From);
                        _fruitView[move.To] = view;

                        var worldPath = BuildWorldPath(move.Path);
                        fallTasks.Add(view.Animator.AnimateFall(worldPath));
                    }
                }
            }

            await UniTask.WhenAll(fallTasks);

            var byColumn = new Dictionary<int, List<FruitMovement>>();
            foreach (var move in movements)
            {
                if (move.From.y < _matchBoard.CurrentBoard.Height) continue;

                if (!byColumn.ContainsKey(move.From.x))
                    byColumn[move.From.x] = new List<FruitMovement>();

                byColumn[move.From.x].Add(move);
            }

            foreach (var column in byColumn.Values)
                column.Sort((a, b) => a.To.y.CompareTo(b.To.y));

            var columnTasks = new List<UniTask>();
            foreach (var column in byColumn.Values)
                columnTasks.Add(SpawnColumnSequential(column));

            await UniTask.WhenAll(columnTasks);
        }

        private async UniTask SpawnColumnSequential(List<FruitMovement> column)
        {
            foreach (var move in column)
            {
                var cell = _matchBoard.CurrentBoard.GetCell(move.To.x, move.To.y);
                var view = SpawnFruitView(move.To, cell.Fruit);
                view.transform.position = GridToWorld(move.From.x, move.From.y);

                var worldPath = BuildWorldPath(move.Path);

                view.Animator.AnimateFall(worldPath).Forget();

                await UniTask.Delay(100);
            }
        }

        private void SpawnVisualCell(int x, int y, Vector3 worldPos)
        {
            var prefab = (x + y) % 2 == 0 ? _brightCellPrefab : _darkCellPrefab;
            var cellGO = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            cellGO.name = $"Cell ({x},{y})";
        }

        private FruitView SpawnFruitView(Vector2Int gridPos, Fruit fruit)
        {
            var view = _pool.Get();
            view.transform.position = GridToWorld(gridPos.x, gridPos.y);
            view.transform.SetParent(transform);
            view.Setup(fruit, _fruitConfig.GetSprite(fruit.Type));
            _fruitView[gridPos] = view;
            return view;
        }

        private Vector3 GridToWorld(int x, int y) =>
            new(
                _startPos.x + x * (_cellSize + _cellSpacing),
                _startPos.y + y * (_cellSize + _cellSpacing),
                0f
            );

        private List<Vector3> BuildWorldPath(List<Vector2Int> path) =>
            path.ConvertAll(p => (Vector3)GridToWorld(p.x, p.y));
    }
}