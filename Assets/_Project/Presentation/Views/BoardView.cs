using System.Collections.Generic;
using Core.Domain;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Presentation.Config;
using Presentation.Pool;
using Presentation.Utils;
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
        [Inject] private PreviewManager _previewManager;

        private Dictionary<Vector2Int, FruitView> _fruitView = new();
        private BoardViewUtils _viewUtils;

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

            _viewUtils = new BoardViewUtils(_cellSize, _cellSpacing);
            _viewUtils.CalculateStartPos(board.Width, board.Height);
            _previewManager.Initialize(_viewUtils);

            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable) continue;

                    var worldPos = _viewUtils.GridToWorld(x, y);

                    SpawnVisualCell(x, y, worldPos);

                    if (cell.Fruit != null)
                        SpawnFruitView(new Vector2Int(x, y), cell.Fruit);
                }
        }

        public FruitView TryGetFruitView(Vector2Int pos)
        {
            if (_fruitView.TryGetValue(pos, out var view))
            {
                return view;
            }

            return null;
        }

        public void SwapFruitViewKeys(Vector2Int a, Vector2Int b)
        {
            var viewA = TryGetFruitView(a);
            var viewB = TryGetFruitView(b);

            if (viewA == null || viewB == null) return;

            _fruitView[a] = viewB;
            _fruitView[b] = viewA;
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (_viewUtils == null) return Vector2Int.zero;
            return _viewUtils.WorldToGrid(worldPos);
        }

        // ── IBoardView ────────────────────────────────────────

        public async UniTask PlaySwap(Vector2Int from, Vector2Int to)
        {
            if (!_fruitView.TryGetValue(from, out var viewFrom)) return;
            if (!_fruitView.TryGetValue(to, out var viewTo)) return;

            await UniTask.WhenAll(
                viewFrom.Animator.AnimateSwap(_viewUtils.GridToWorld(to)),
                viewTo.Animator.AnimateSwap(_viewUtils.GridToWorld(from))
            );

            SwapFruitViewKeys(from, to);
        }

        public async UniTask PlayDestroy(List<Vector2Int> positions)
        {
            var tasks = new List<UniTask>();

            foreach (var pos in positions)
            {
                if (_fruitView.TryGetValue(pos, out var view))
                {
                    _fruitView.Remove(pos);
                    tasks.Add(AnimateAndReturn(view));
                }
            }

            await UniTask.WhenAll(tasks);
        }

        public async UniTask PlayGravity(List<FruitMovement> movements, int startDelayMs)
        {
            await UniTask.Delay(startDelayMs);

            var fallTasks = new List<UniTask>();
            foreach (var move in movements)
            {
                if (move.From.y >= _matchBoard.CurrentBoard.Height) continue;

                if (_fruitView.TryGetValue(move.From, out var view))
                {
                    _fruitView.Remove(move.From);
                    _fruitView[move.To] = view;
                    fallTasks.Add(view.Animator.AnimateFall(BuildWorldPath(move.Path)));
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

        // ── Helpers ───────────────────────────────────────────

        private void SpawnVisualCell(int x, int y, Vector3 worldPos)
        {
            var prefab = (x + y) % 2 == 0 ? _brightCellPrefab : _darkCellPrefab;
            var cellGO = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            cellGO.name = $"Cell ({x},{y})";
        }

        private FruitView SpawnFruitView(Vector2Int gridPos, Fruit fruit)
        {
            var view = _pool.Get();
            view.transform.position = _viewUtils.GridToWorld(gridPos);
            view.transform.SetParent(transform);
            view.Setup(fruit, _fruitConfig.GetSprite(fruit.Type));
            _fruitView[gridPos] = view;
            return view;
        }

        private async UniTask SpawnColumnSequential(List<FruitMovement> column)
        {
            foreach (var move in column)
            {
                Fruit fruit;

                if (move.SyncFruitType >= 0)
                {
                    fruit = new Fruit((FruitType)move.SyncFruitType);
                }
                else
                {
                    fruit = _matchBoard.CurrentBoard.GetCell(move.To.x, move.To.y).Fruit;
                }

                if (fruit == null) continue;

                var view = SpawnFruitView(move.To, fruit);
                view.transform.position = _viewUtils.GridToWorld(move.From);

                view.Animator.AnimateFall(BuildWorldPath(move.Path)).Forget();
                await UniTask.Delay(100);
            }
        }

        private async UniTask AnimateAndReturn(FruitView view)
        {
            await view.Animator.AnimateDestroy();
            _pool.Return(view);
        }

        private List<Vector2> BuildWorldPath(List<Vector2Int> path) =>
            path.ConvertAll(p => _viewUtils.GridToWorld(p));
    }
}