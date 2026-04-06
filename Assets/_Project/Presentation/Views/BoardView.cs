using System;
using System.Collections.Generic;
using System.Threading;
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

        private FruitViewRegistry _registry;
        private BoardViewUtils _viewUtils;
        private BoardSpawner _spawner;
        private BoardAnimator _animator;
        private CancellationTokenSource _hintCts;

        public bool IsInitialized => _viewUtils != null;

        void Awake() => _matchBoard.OnBoardInitialized += OnBoardInitialized;

        void Start()
        {
            if (_matchBoard.CurrentBoard != null && _viewUtils == null)
                OnBoardInitialized();
        }

        void OnDestroy()
        {
            _matchBoard.OnBoardInitialized -= OnBoardInitialized;
            ClearHint();
        }

        private void OnBoardInitialized()
        {
            var board = _matchBoard.CurrentBoard;
            if (board == null) return;

            _viewUtils = new BoardViewUtils(_cellSize, _cellSpacing);
            _viewUtils.CalculateStartPos(board.Width, board.Height);
            _previewManager.Initialize(_viewUtils);

            _registry = new FruitViewRegistry();
            _spawner = new BoardSpawner(_brightCellPrefab, _darkCellPrefab,
                _pool, _fruitConfig, _viewUtils, _registry, transform);
            _animator = new BoardAnimator(_registry, _spawner, _viewUtils, _matchBoard);

            _spawner.BuildBoard(board);
        }

        public UniTask PlaySwap(Vector2Int from, Vector2Int to) => _animator.PlaySwap(from, to);
        public UniTask PlayDestroy(List<Vector2Int> positions) => _animator.PlayDestroy(positions);
        public UniTask PlayGravity(List<FruitMovement> movements, int startDelayMs) => _animator.PlayGravity(movements, startDelayMs);
        public UniTask PlayShuffle(List<FruitMovement> spawnMovements) => _animator.PlayShuffle(spawnMovements);
        public void SwapFruitViewKeys(Vector2Int a, Vector2Int b) => _registry.Swap(a, b);
        public FruitView TryGetFruitView(Vector2Int pos) { _registry.TryGet(pos, out var v); return v; }
        public bool HasFruitViewAt(Vector2Int pos) => _registry.Has(pos);
        public Vector2Int WorldToGrid(Vector3 worldPos) => _viewUtils?.WorldToGrid(worldPos) ?? Vector2Int.zero;

        public void ShowHint(Vector2Int from, Vector2Int to)
        {
            ClearHint();
            _hintCts = new CancellationTokenSource();
            PulseHint(from, to, _hintCts.Token).Forget();
        }

        public void ClearHint()
        {
            _hintCts?.Cancel();
            _hintCts?.Dispose();
            _hintCts = null;
        }

        private async UniTaskVoid PulseHint(Vector2Int from, Vector2Int to, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_registry.TryGet(from, out var a)) a.Animator.PlayPulse();
                if (_registry.TryGet(to, out var b)) b.Animator.PlayPulse();
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct)
                             .SuppressCancellationThrow();
            }
        }
    }
}