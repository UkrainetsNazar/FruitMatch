using System.Collections.Generic;
using Core.Domain.Entities;
using UnityEngine;

namespace Core.Systems.Pathfinding
{
    public class BoardGraph
    {
        private readonly Board _board;
        private readonly int _spawnRow;

        public BoardGraph(Board board)
        {
            _board = board;
            _spawnRow = board.Height;
        }

        public List<Vector2Int> GetNeighbours(Vector2Int pos)
        {
            var dirs = new[]
            {
            Vector2Int.down,
            new Vector2Int(-1, -1),
            new Vector2Int( 1, -1)
        };

            var result = new List<Vector2Int>();
            foreach (var dir in dirs)
            {
                var next = pos + dir;
                if (next.x >= 0 && next.x < _board.Width &&
                    next.y >= 0 && next.y <= _spawnRow)
                    result.Add(next);
            }
            return result;
        }

        public float GetCost(Vector2Int from, Vector2Int to)
        {
            bool isDiagonal = from.x != to.x;

            return isDiagonal ? 1.4f : 1f;
        }

        public bool IsWalkable(Vector2Int pos)
        {
            if (pos.y == _spawnRow) return true;

            var cell = _board.GetCell(pos.x, pos.y);
            return cell != null && cell.IsUsable && cell.Fruit == null;
        }
    }
}