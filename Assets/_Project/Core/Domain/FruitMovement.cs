using System.Collections.Generic;
using UnityEngine;

namespace Core.Domain
{
    public class FruitMovement
    {
        public Vector2Int From { get; set; }
        public Vector2Int To   { get; set; }
        public List<Vector2Int> Path { get; set; }
    }
}