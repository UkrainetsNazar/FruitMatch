using Core.Domain;
using Presentation.Animations;
using UnityEngine;

namespace Presentation.Views
{
    public class FruitView : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private FruitAnimator _animator;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<FruitAnimator>();
        }

        public void Setup(Fruit fruit, Sprite sprite)
        {
            _spriteRenderer.sprite = sprite;
            gameObject.name = $"Fruit ({fruit.Type})";
        }

        public FruitAnimator Animator => _animator;
    }
}