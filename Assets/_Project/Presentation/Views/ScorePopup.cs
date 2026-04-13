using Cysharp.Threading.Tasks;
using DG.Tweening;
using Presentation.Pool;
using TMPro;
using UnityEngine;

namespace Presentation.Views
{
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _riseDuration = 0.6f;
        [SerializeField] private float _riseHeight = 1f;
        [SerializeField] private float _fadeDuration = 0.3f;

        private ScorePopupPool _pool;

        void Awake()
        {
            var meshRenderer = _text.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingLayerName = "Default";
                meshRenderer.sortingOrder = 10;
            }
        }

        public void Initialize(ScorePopupPool pool)
        {
            _pool = pool;
        }

        public void Play(Vector3 worldPos, int score)
        {
            transform.position = worldPos;
            _text.text = $"+{score}";
            _text.alpha = 1f;
            transform.localScale = Vector3.one;

            gameObject.SetActive(true);
            AnimateAsync().Forget();
        }

        private async UniTaskVoid AnimateAsync()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOMoveY(transform.position.y + _riseHeight, _riseDuration)
                .SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(1.3f, _riseDuration * 0.4f)
                .SetEase(Ease.OutBack)
                .SetLoops(2, LoopType.Yoyo));
            sequence.Append(_text.DOFade(0f, _fadeDuration)
                .SetEase(Ease.InQuad));

            await sequence.AsyncWaitForCompletion();

            _pool.Return(this);
        }
    }
}