using System.Collections.Generic;
using UnityEngine;

namespace Core.Domain.ValueObjects
{
    public class MatchResult
    {
        public List<Vector2Int> MatchedPositions { get; } = new();
        public int Score { get; set; }
        public int ComboMultiplier { get; set; } = 1;
    }
}