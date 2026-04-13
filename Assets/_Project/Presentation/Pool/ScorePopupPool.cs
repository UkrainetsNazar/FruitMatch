using System.Collections.Generic;
using Presentation.Views;
using UnityEngine;

namespace Presentation.Pool
{
    public class ScorePopupPool : MonoBehaviour
    {
        [SerializeField] private ScorePopup _prefab;
        [SerializeField] private int _initialSize = 10;

        private readonly Queue<ScorePopup> _pool = new();

        void Awake()
        {
            for (int i = 0; i < _initialSize; i++)
                _pool.Enqueue(CreateNew());
        }

        public ScorePopup Get()
        {
            var popup = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
            return popup;
        }

        public void Return(ScorePopup popup)
        {
            popup.gameObject.SetActive(false);
            _pool.Enqueue(popup);
        }

        private ScorePopup CreateNew()
        {
            var go = Instantiate(_prefab, transform);
            go.Initialize(this);
            go.gameObject.SetActive(false);
            return go;
        }
    }
}