using Core.Domain;
using Presentation.Animations;
using UnityEngine;

namespace Presentation.Views
{
    [RequireComponent(typeof(FruitAnimator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FruitView : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private FruitAnimator _animator;
        private Vector3 _originalScale;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<FruitAnimator>();
            _originalScale = transform.localScale;
            _animator.Initialize(this);
        }

        public void Setup(Fruit fruit, Sprite sprite)
        {
            _spriteRenderer.sprite = sprite;
            gameObject.name = $"Fruit ({fruit.Type})";
            transform.localScale = _originalScale;
        }

        public FruitAnimator Animator => _animator;
        public void ResetScale() => transform.localScale = _originalScale;
    }
}