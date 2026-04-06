using System.Collections.Generic;
using Core.Domain;
using UnityEngine;

namespace Data.Services
{
    public class MatchFinder
    {
        public List<MatchResult> FindMatches(Board board)
        {
            var results = new List<MatchResult>();
            FindLineMatches(board, results, Vector2Int.right);
            FindLineMatches(board, results, Vector2Int.up);
            return results;
        }

        private void FindLineMatches(Board _board, List<MatchResult> results, Vector2Int direction)
        {
            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    var startPos = new Vector2Int(x, y);

                    Cell cell = _board.GetCell(startPos.x, startPos.y);
                    if (cell == null || !cell.IsUsable || cell.Fruit == null)
                        continue;

                    var chain = new List<Vector2Int> { startPos };
                    var targetType = cell.Fruit.Type;
                    var next = startPos + direction;

                    while (_board.IsValid(next))
                    {
                        Cell nextCell = _board.GetCell(next.x, next.y);
                        if (nextCell == null || !nextCell.IsUsable || nextCell.Fruit == null)
                            break;

                        if (nextCell.Fruit.Type == targetType)
                        {
                            chain.Add(next);
                            next += direction;
                        }
                        else break;
                    }

                    if (chain.Count >= 3)
                    {
                        var result = new MatchResult();
                        result.MatchedPositions.AddRange(chain);
                        result.Score = chain.Count * 10;
                        results.Add(result);

                        if (direction == Vector2Int.right)
                            x += chain.Count - 1;
                        else
                            y += chain.Count - 1;
                    }
                }
            }
        }

        public bool HasAnyValidMove(Board board)
        {
            var directions = new[] { Vector2Int.right, Vector2Int.up };

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable || cell.Fruit == null) continue;

                    foreach (var dir in directions)
                    {
                        var neighbour = pos + dir;
                        if (!board.IsValid(neighbour)) continue;
                        var nCell = board.GetCell(neighbour.x, neighbour.y);
                        if (!nCell.IsUsable || nCell.Fruit == null) continue;

                        (cell.Fruit, nCell.Fruit) = (nCell.Fruit, cell.Fruit);
                        bool hasMatch = FindMatches(board).Count > 0;
                        (cell.Fruit, nCell.Fruit) = (nCell.Fruit, cell.Fruit);

                        if (hasMatch) return true;
                    }
                }
            }
            return false;
        }
        public (Vector2Int, Vector2Int)? FindFirstValidMove(Board board)
        {
            var directions = new[] { Vector2Int.right, Vector2Int.up };

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable || cell.Fruit == null) continue;

                    foreach (var dir in directions)
                    {
                        var neighbour = pos + dir;
                        if (!board.IsValid(neighbour)) continue;
                        var nCell = board.GetCell(neighbour.x, neighbour.y);
                        if (!nCell.IsUsable || nCell.Fruit == null) continue;

                        (cell.Fruit, nCell.Fruit) = (nCell.Fruit, cell.Fruit);
                        bool hasMatch = FindMatches(board).Count > 0;
                        (cell.Fruit, nCell.Fruit) = (nCell.Fruit, cell.Fruit);

                        if (hasMatch) return (pos, neighbour);
                    }
                }
            }
            return null;
        }
    }
}