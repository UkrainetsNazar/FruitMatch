using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Interfaces
{
    public interface IGameController
    {
        UniTask StartGame();
        UniTask OnPlayerSwap(Vector2Int from, Vector2Int to);
    }
}