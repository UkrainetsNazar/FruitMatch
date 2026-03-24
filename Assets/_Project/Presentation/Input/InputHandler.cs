using Core.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Presentation.PlayerInput
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] float dragThreathold = 0.3f;

        private Vector2Int _fromGridPos;
        private Vector2Int _toGridPos;
        private Vector2 _startWorldPos;
        private bool _isDragging;
        private bool _isProcessing;
        [Inject] private IBoardView _boardView;
        [Inject] private IMatchBoard _matchBoard;
        [Inject] private IGameController _gameController;
        [Inject] private PreviewManager _previewManager;

        void Update()
        {
            if (_isProcessing) return;

            if (Input.GetMouseButtonDown(0))
                StartDragging();

            if (Input.GetMouseButton(0) && _isDragging)
                OnMouseDrag();

            if (Input.GetMouseButtonUp(0))
                OnMouseUp();
        }

        private void StartDragging()
        {
            _startWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            var gridPos = _boardView.WorldToGrid(_startWorldPos);

            if (!_matchBoard.HasFruitAt(gridPos)) return;

            _fromGridPos = gridPos;
            _toGridPos = Vector2Int.zero;
            _isDragging = true;
        }

        private void OnMouseDrag()
        {
            if (_previewManager.IsAnimating) return;

            Vector2 currentWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 delta = currentWorldPos - _startWorldPos;

            if (delta.magnitude < dragThreathold)
            {
                if (_previewManager.IsPreviewActive)
                    HandleResetPreview().Forget();
                return;
            }

            var direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? Vector2Int.right : Vector2Int.left)
                : (delta.y > 0 ? Vector2Int.up : Vector2Int.down);

            Vector2Int candidateTo = _fromGridPos + direction;

            if (candidateTo == _fromGridPos)
            {
                if (_previewManager.IsPreviewActive)
                    HandleResetPreview().Forget();
                _toGridPos = Vector2Int.zero;
                return;
            }

            if (candidateTo == _toGridPos) return;

            _toGridPos = candidateTo;
            if (!_matchBoard.HasFruitAt(_toGridPos)) return;

            HandlePreview().Forget();
        }

        private async UniTaskVoid HandleResetPreview()
        {
            await _previewManager.ResetPreview();
            _toGridPos = Vector2Int.zero;
        }

        private async UniTaskVoid HandlePreview()
        {
            await _previewManager.StartPreview(_fromGridPos, _toGridPos);
        }

        private void OnMouseUp()
        {
            if (!_isDragging || _isProcessing) return;
            _isDragging = false;

            if (!_previewManager.IsPreviewActive) return;

            HandleSwap().Forget();
        }

        private async UniTaskVoid HandleSwap()
        {
            _isProcessing = true;
            await _gameController.OnPlayerSwap(_fromGridPos, _toGridPos);
            _isProcessing = false;
        }
    }
}