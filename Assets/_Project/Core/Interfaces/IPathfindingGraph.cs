using System.Collections.Generic;
using UnityEngine;

public interface IPathfindingGraph
{
    List<Vector2Int> GetNeighbours(Vector2Int pos);
    float GetCost(Vector2Int from, Vector2Int to);
    bool IsWalkable(Vector2Int pos);
}