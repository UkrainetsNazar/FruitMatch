using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Presentation.Animations
{
    public class FruitAnimator : MonoBehaviour
    {
        [SerializeField] private float _stepDuration = 0.08f;
        [SerializeField] private float _destroyDuration = 2f;

        public async UniTask AnimateFall(List<Vector3> worldPath)
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
                    transform.DOMove(worldPath[i], ease_duration)
                        .SetEase(easeType)
                );
            }

            await sequence.AsyncWaitForCompletion();
        }

        public async UniTask AnimateSwap(Vector3 worldPos)
        {
            await transform.DOMove(worldPos, 0.2f)
                .SetEase(Ease.OutBack)
                .AsyncWaitForCompletion();
        }

        public async UniTask AnimateDestroy()
        {
            Vector3 startPos = transform.position;

            float horizontalOffset = Random.Range(-0.8f, 0.8f);
            float verticalJump = Random.Range(0.3f, 0.7f);
            float randomRotation = Random.Range(-180f, 180f);

            Vector3 jumpPeak = startPos + new Vector3(horizontalOffset, verticalJump, 0);
            Vector3 fallEnd = new Vector3(jumpPeak.x, startPos.y - 6f, 0);

            var sequence = DOTween.Sequence();

            sequence.Append(
                transform.DOMove(jumpPeak, _destroyDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );

            sequence.Append(
                transform.DOMove(fallEnd, _destroyDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );

            sequence.Insert(0f,
                transform.DORotate(
                    new Vector3(0, 0, randomRotation),
                    _destroyDuration * 0.3f,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.OutQuad)
            );

            sequence.Insert(_destroyDuration * 0.3f,
                transform.DOScale(0f, _destroyDuration * 0.8f)
                    .SetEase(Ease.InQuad)
            );

            await sequence.AsyncWaitForCompletion();

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}