using System;
using System.Threading;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Presentation.Views
{
    public class HintSystem : IDisposable
    {
        private readonly IBoardView _boardView;
        private CancellationTokenSource _cts;
        private const float HintDelay = 10f;

        public HintSystem(IBoardView boardView)
        {
            _boardView = boardView;
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

        private async UniTaskVoid RunHintTimer(Vector2Int from, Vector2Int to, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(HintDelay), cancellationToken: ct)
                         .SuppressCancellationThrow();

            if (ct.IsCancellationRequested) return;

            if (_boardView == null) return;

            _boardView.ShowHint(from, to);
        }

        public void Dispose() => Cancel();
    }
}