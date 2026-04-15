using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Presentation.Animations
{
    [RequireComponent(typeof(Button))]
    public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _pressScale = 0.92f;
        [SerializeField] private float _hoverDuration = 0.15f;
        [SerializeField] private float _pressDuration = 0.08f;

        private Vector3 _originalScale;
        private Button _button;
        private Button Button => _button != null ? _button : (_button = GetComponent<Button>());

        public UnityEvent onClick => Button.onClick;
        public bool interactable
        {
            get => Button.interactable;
            set => Button.interactable = value;
        }

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

        public void OnPointerEnter(PointerEventData _) =>
            transform.DOScale(_originalScale * _hoverScale, _hoverDuration).SetEase(Ease.OutQuad);

        public void OnPointerExit(PointerEventData _) =>
            transform.DOScale(_originalScale, _hoverDuration).SetEase(Ease.OutQuad);

        public void OnPointerDown(PointerEventData _) =>
            transform.DOScale(_originalScale * _pressScale, _pressDuration).SetEase(Ease.OutQuad);

        public void OnPointerUp(PointerEventData _) =>
            transform.DOScale(_originalScale * _hoverScale, _pressDuration).SetEase(Ease.OutQuad);
    }
}