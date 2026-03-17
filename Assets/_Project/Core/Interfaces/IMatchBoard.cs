using System.Collections.Generic;
using Core.Domain;
using UnityEngine;

namespace Core.Interfaces
{
    public interface IMatchBoard
    {
        Board CurrentBoard { get; }

        void Initialize(int[,] mask);
        bool TrySwap(Vector2Int from, Vector2Int to);
        List<MatchResult> FindMatches();
    }
}