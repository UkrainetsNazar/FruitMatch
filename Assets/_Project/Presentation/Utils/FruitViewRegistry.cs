using System.Collections.Generic;
using System.Linq;
using Presentation.Views;
using UnityEngine;

namespace Presentation.Utils
{
    public class FruitViewRegistry
    {
        private readonly Dictionary<Vector2Int, FruitView> _views = new();

        public void Set(Vector2Int pos, FruitView view) => _views[pos] = view;
        public void Remove(Vector2Int pos) => _views.Remove(pos);
        public bool TryGet(Vector2Int pos, out FruitView view) => _views.TryGetValue(pos, out view);
        public bool Has(Vector2Int pos) => _views.ContainsKey(pos);
        public List<Vector2Int> AllPositions() => _views.Keys.ToList();

        public void Swap(Vector2Int a, Vector2Int b)
        {
            var viewA = _views.GetValueOrDefault(a);
            var viewB = _views.GetValueOrDefault(b);
            if (viewA == null || viewB == null) return;
            _views[a] = viewB;
            _views[b] = viewA;
        }
    }
}