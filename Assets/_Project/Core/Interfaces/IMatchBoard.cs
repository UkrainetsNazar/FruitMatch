using System;
using System.Collections.Generic;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.ValueObjects;
using UnityEngine;

namespace Core.Interfaces
{
    public interface IMatchBoard
    {
        Board CurrentBoard { get; }
        event Action OnBoardInitialized;
        
        void Initialize(Board board, int seed);
        bool TrySwap(Vector2Int from, Vector2Int to);
        List<MatchResult> FindMatches();
        void ProcessMatches(List<MatchResult> matches, int comboMultiplier);
        List<FruitMovement> ApplyGravity();
        List<FruitMovement> ApplyGravityWithTypes(Queue<FruitType> syncedTypes);
        List<FruitMovement> BuildSpawnMovements();
        int[] ShuffleBoard();
        (Vector2Int, Vector2Int, int)? FindHint();
        bool HasFruitAt(Vector2Int pos);
        bool HasAnyValidMove();
    }
}