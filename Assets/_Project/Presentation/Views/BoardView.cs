using System.Collections.Generic;
using System.Linq;
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

        public bool IsInitialized => _viewUtils != null;

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
            if (board == null) return;

            InitializeUtils(board.Width, board.Height);

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

        private void InitializeUtils(int width, int height)
        {
            if (_viewUtils != null) return;
            _viewUtils = new BoardViewUtils(_cellSize, _cellSpacing);
            _viewUtils.CalculateStartPos(width, height);
            _previewManager.Initialize(_viewUtils);
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
            await UniTask.Delay(50);
        }

        public async UniTask PlayGravity(List<FruitMovement> movements, int startDelayMs)
        {
            if (startDelayMs > 0) await UniTask.Delay(startDelayMs);

            var allFallTasks = new List<UniTask>();

            foreach (var move in movements)
            {
                if (move.From.y < _matchBoard.CurrentBoard.Height)
                {
                    if (_fruitView.TryGetValue(move.From, out var view))
                    {
                        _fruitView.Remove(move.From);
                        _fruitView[move.To] = view;
                        allFallTasks.Add(view.Animator.AnimateFall(BuildWorldPath(move.Path)));
                    }
                }
            }

            var byColumn = movements
                .Where(m => m.From.y >= _matchBoard.CurrentBoard.Height)
                .GroupBy(m => m.From.x)
                .ToList();

            foreach (var column in byColumn)
            {
                var sortedColumn = column.OrderBy(m => m.To.y).ToList();
                allFallTasks.Add(SpawnColumnSequential(sortedColumn));
            }

            await UniTask.WhenAll(allFallTasks);
            await UniTask.Delay(100);
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
            var tasks = new List<UniTask>();
            foreach (var move in column)
            {
                Fruit fruit = move.SyncFruitType >= 0
                    ? new Fruit((FruitType)move.SyncFruitType)
                    : _matchBoard.CurrentBoard.GetCell(move.To.x, move.To.y).Fruit;

                if (fruit == null) continue;

                var view = SpawnFruitView(move.To, fruit);
                view.transform.position = _viewUtils.GridToWorld(move.From);

                tasks.Add(view.Animator.AnimateFall(BuildWorldPath(move.Path)));

                await UniTask.Delay(60);
            }
            await UniTask.WhenAll(tasks);
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