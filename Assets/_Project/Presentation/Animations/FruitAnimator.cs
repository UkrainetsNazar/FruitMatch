using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Presentation.Views;

namespace Presentation.Animations
{
    public class FruitAnimator : MonoBehaviour
    {
        [SerializeField] private float _stepDuration = 0.05f;
        private FruitView _fruitView;

        public void Initialize(FruitView fruitView)
        {
            _fruitView = fruitView;
        }

        public async UniTask AnimateFall(List<Vector2> worldPath)
        {
            transform.DOKill();
            _fruitView.ResetScale();
            if (worldPath.Count == 0) return;


            var sequence = DOTween.Sequence();

            for (int i = 0; i < worldPath.Count; i++)
            {
                bool isLastStep = (i == worldPath.Count - 1);

                float duration = isLastStep ? _stepDuration * 1.5f : _stepDuration;
                Ease easeType = isLastStep ? Ease.OutBounce : Ease.Linear;

                sequence.Append(
                    transform.DOMove(new Vector3(worldPath[i].x, worldPath[i].y, 0f), duration)
                        .SetEase(easeType)
                );
            }

            await sequence.AsyncWaitForCompletion();
        }

        public async UniTask AnimateSwap(Vector2 worldPos)
        {
            transform.DOKill();
            _fruitView.ResetScale();
            await transform.DOMove(worldPos, 0.15f)
                .SetEase(Ease.OutCubic)
                .AsyncWaitForCompletion();
        }

        public async UniTask AnimateDestroy()
        {
            transform.DOKill();
            var sequence = DOTween.Sequence();

            sequence.Append(transform.DOMove(transform.position + Vector3.up * 0.5f, 0.2f).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(0f, 0.4f).SetEase(Ease.InBack));
            sequence.Join(transform.DORotate(new Vector3(0, 0, 90), 0.4f));

            await sequence.AsyncWaitForCompletion();

            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        public void PlayPulse()
        {
            transform.DOKill(complete: false);
            transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, vibrato: 1, elasticity: 0.5f)
                     .SetEase(Ease.OutQuad);
        }
    }
}