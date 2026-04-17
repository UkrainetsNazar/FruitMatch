using System;
using System.Threading;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using Data.Services.Helpers;
using UnityEngine;

namespace Presentation.Utils
{
    public class HintSystem : IDisposable
    {
        private readonly IBoardView _boardView;
        private readonly AStarHintService _hintService;
        private CancellationTokenSource _cts;
        private const float HintDelay = 10f;

        public HintSystem(IBoardView boardView, IMatchBoard matchBoard)
        {
            _boardView = boardView;
            _hintService = new AStarHintService(matchBoard);
        }

        public HintSystem(IBoardView boardView)
        {
            _boardView = boardView;
            _hintService = null;
        }

        public void OnTurnStarted(Vector2Int from, Vector2Int to)
        {
            Cancel();
            _cts = new CancellationTokenSource();
            RunHintTimer(from, to, _cts.Token).Forget();
        }

        public void OnPlayerActed() => Cancel();
        public void OnTurnEnded() => Cancel();

        private void Cancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _boardView.ClearHint();
        }

        private async UniTaskVoid RunHintTimer(
            Vector2Int from, Vector2Int to, CancellationToken ct)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(HintDelay), cancellationToken: ct)
                .SuppressCancellationThrow();

            if (ct.IsCancellationRequested || _boardView == null) return;

            var hint = _hintService?.FindBestHint();

            if (hint != null)
            {
                var (hintFrom, hintTo, _) = hint.Value;
                _boardView.ShowHint(hintFrom, hintTo);
            }
            else
            {
                _boardView.ShowHint(from, to);
            }
        }

        public void Dispose() => Cancel();
    }
}