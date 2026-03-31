using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Presentation.Animations
{
    public class FruitAnimator : MonoBehaviour
    {
        [SerializeField] private float _stepDuration = 0.05f;

        public async UniTask AnimateFall(List<Vector2> worldPath)
        {
            if (worldPath.Count == 0) return;

            if (worldPath.Count == 1)
            {
                await transform.DOMove(worldPath[0], _stepDuration)
                    .SetEase(Ease.OutBounce)
                    .AsyncWaitForCompletion();
                return;
            }

            var sequence = DOTween.Sequence();

            for (int i = 0; i < worldPath.Count; i++)
            {
                float ease_duration = _stepDuration;

                Ease easeType = i == worldPath.Count - 1
                    ? Ease.OutBounce
                    : Ease.InQuad;

                sequence.Append(
                    transform.DOMove(new Vector3(worldPath[i].x, worldPath[i].y, 0f), ease_duration)
                        .SetEase(easeType)
                );
            }

            await sequence.AsyncWaitForCompletion();
        }

        public async UniTask AnimateSwap(Vector2 worldPos)
        {
            await transform.DOMove(new Vector3(worldPos.x, worldPos.y, 0f), 0.15f)
                .SetEase(Ease.OutCubic)
                .AsyncWaitForCompletion();
        }

        public async UniTask AnimateDestroy()
        {
            var sequence = DOTween.Sequence();

            sequence.Append(transform.DOMove(transform.position + Vector3.up * 0.5f, 0.2f).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(0f, 0.4f).SetEase(Ease.InBack));
            sequence.Join(transform.DORotate(new Vector3(0, 0, 90), 0.4f));

            await sequence.AsyncWaitForCompletion();

            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }
    }
}