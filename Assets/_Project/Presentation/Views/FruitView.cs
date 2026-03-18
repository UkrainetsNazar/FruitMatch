using Core.Domain;
using UnityEngine;

namespace Presentation.Views
{
    public class FruitView : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Setup(Fruit fruit, Sprite sprite)
        {
            _spriteRenderer.sprite = sprite;
            gameObject.name = $"Fruit ({fruit.Type})";
        }
    }
}