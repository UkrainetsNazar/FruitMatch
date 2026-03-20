using System;
using System.Collections.Generic;
using Core.Domain;
using UnityEngine;

namespace Core.Interfaces
{
    public interface IMatchBoard
    {
        Board CurrentBoard { get; }
        event Action OnBoardInitialized;
        event Action<List<FruitMovement>> OnGravityApplied;
        event Action<List<Vector2Int>> OnMatchesProcessed;
        
        void Initialize(Board board);
        bool TrySwap(Vector2Int from, Vector2Int to);
        List<MatchResult> FindMatches();
        void ProcessMatches(List<MatchResult> matches);
    }
}