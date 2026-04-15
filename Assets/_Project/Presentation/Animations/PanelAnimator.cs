using DG.Tweening;
using UnityEngine;

namespace Presentation.Animations
{
    public class PanelAnimator : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.25f;
        [SerializeField] private Ease _easeIn = Ease.OutBack;
        [SerializeField] private Ease _easeOut = Ease.InBack;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, _duration).SetEase(_easeIn);
        }

        public void Hide()
        {
            transform.DOKill();
            transform.DOScale(0f, _duration)
                .SetEase(_easeOut)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}