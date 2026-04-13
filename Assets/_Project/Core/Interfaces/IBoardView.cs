using Cysharp.Threading.Tasks;
using Core.Domain;
using System.Collections.Generic;
using UnityEngine;
using Presentation.Views;

namespace Core.Interfaces
{
    public interface IBoardView
    {
        bool IsInitialized { get; }

        UniTask PlaySwap(Vector2Int from, Vector2Int to);
        UniTask PlayDestroy(List<Vector2Int> positions, int score = 0);
        UniTask PlayGravity(List<FruitMovement> movements, int startDelayMs);
        UniTask PlayShuffle(List<FruitMovement> spawnMovements);

        void ShowHint(Vector2Int from, Vector2Int to);
        void ClearHint();

        bool HasFruitViewAt(Vector2Int pos);
        FruitView TryGetFruitView(Vector2Int pos);
        void SwapFruitViewKeys(Vector2Int a, Vector2Int b);
        Vector2Int WorldToGrid(Vector3 worldPos);
    }
}