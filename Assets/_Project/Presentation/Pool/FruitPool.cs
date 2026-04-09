using System.Collections.Generic;
using DG.Tweening;
using Presentation.Views;
using UnityEngine;

namespace Presentation.Pool
{
    public class FruitPool : MonoBehaviour
    {
        [SerializeField] private GameObject _fruitPrefab;
        [SerializeField] private int _initialSize = 120;

        private Queue<FruitView> _pool;

        private void Awake()
        {
            _pool = new Queue<FruitView>();

            for (int i = 0; i < _initialSize; i++)
                _pool.Enqueue(CreateFruitView());
        }

        public FruitView Get()
        {
            var view = _pool.Count > 0 ? _pool.Dequeue() : CreateFruitView();
            view.transform.DOKill();
            view.gameObject.SetActive(true);
            return view;
        }

        public void Return(FruitView view)
        {
            view.transform.DOKill();
            view.transform.localScale = Vector3.one;
            view.transform.localRotation = Quaternion.identity;
            view.gameObject.SetActive(false);
            view.transform.SetParent(transform);
            _pool.Enqueue(view);
        }

        private FruitView CreateFruitView()
        {
            var go = Instantiate(_fruitPrefab, transform);
            go.SetActive(false);
            return go.GetComponent<FruitView>();
        }
    }
}