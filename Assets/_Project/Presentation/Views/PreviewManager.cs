using System.Threading.Tasks;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Infrastructure.Audio;
using Presentation.Utils;
using Presentation.Views;
using UnityEngine;

public class PreviewManager
{
    private readonly IBoardView _boardView;
    private BoardViewUtils _viewUtils;

    private Vector2Int _previewFrom;
    private Vector2Int _previewTo;
    private bool _isPreviewActive;
    private bool _isAnimating;

    public PreviewManager(IBoardView boardView)
    {
        _boardView = boardView;
    }

    public void Initialize(BoardViewUtils viewUtils)
    {
        _viewUtils = viewUtils;
    }

    public async UniTask StartPreview(Vector2Int from, Vector2Int to)
    {
        if (_isAnimating) return;

        if (_isPreviewActive && _previewFrom == from && _previewTo == to) return;

        _isAnimating = true;

        if (_isPreviewActive)
            await ResetPreviewInternal();

        _previewFrom = from;
        _previewTo = to;
        _isPreviewActive = true;

        var fromView = _boardView.TryGetFruitView(from);
        var toView = _boardView.TryGetFruitView(to);

        if (fromView != null && toView != null)
        {
            AudioManager.PlayFruitSwap();

            await UniTask.WhenAll(
                fromView.Animator.AnimateSwap(_viewUtils.GridToWorld(to)),
                toView.Animator.AnimateSwap(_viewUtils.GridToWorld(from))
            );
        }

        _isAnimating = false;
    }

    public async UniTask ResetPreview()
    {
        if (!_isPreviewActive) return;

        await UniTask.WaitUntil(() => !_isAnimating);

        _isAnimating = true;
        await ResetPreviewInternal();
        _isPreviewActive = false;
        _isAnimating = false;
    }

    private async UniTask ResetPreviewInternal()
    {
        var fromView = _boardView.TryGetFruitView(_previewFrom);
        var toView = _boardView.TryGetFruitView(_previewTo);

        if (fromView != null && toView != null)
        {
            AudioManager.PlayFruitSwap();
            await UniTask.WhenAll(
                fromView.Animator.AnimateSwap(_viewUtils.GridToWorld(_previewFrom)),
                toView.Animator.AnimateSwap(_viewUtils.GridToWorld(_previewTo))
            );
        }
    }

    public UniTask ConfirmPreview()
    {
        if (!_isPreviewActive) return UniTask.CompletedTask;
        _boardView.SwapFruitViewKeys(_previewFrom, _previewTo);
        _isPreviewActive = false;
        return UniTask.CompletedTask;
    }

    public bool IsPreviewActive => _isPreviewActive;
    public bool IsAnimating => _isAnimating;
}
