using Cysharp.Threading.Tasks;
using Core.Domain;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Interfaces
{
    public interface IBoardView
    {
        UniTask AnimateSwap(Vector2Int from, Vector2Int to);
        UniTask PlayDestroy(List<Vector2Int> positions);
        UniTask PlayGravity(List<FruitMovement> movements, int startDelayMs);
    }
}