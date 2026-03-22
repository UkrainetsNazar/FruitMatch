using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Interfaces;
using Data.Pathfinding;
using UnityEngine;

namespace Data.Services
{
    public class MatchBoard : IMatchBoard
    {
        private Board _board;
        private readonly IFruitFactory _fruitFactory;

        public event Action OnBoardInitialized;

        public Board CurrentBoard => _board;

        public MatchBoard(IFruitFactory fruitFactory)
        {
            _fruitFactory = fruitFactory;
        }

        // Creating board with fruits
        public void Initialize(Board board)
        {
            _board = board;

            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    Cell cell = _board.GetCell(x, y);
                    if (cell.IsUsable)
                    {
                        cell.Fruit = _fruitFactory.CreateRandom();
                    }
                }

            OnBoardInitialized?.Invoke();
        }

        // Finding matches on board
        public List<MatchResult> FindMatches()
        {
            var results = new List<MatchResult>();

            FindLineMatches(results, Vector2Int.right);
            FindLineMatches(results, Vector2Int.up);

            return results;
        }

        // Deleting matches on board
        public void ProcessMatches(List<MatchResult> matches)
        {
            var toDestroy = new HashSet<Vector2Int>();

            foreach (var match in matches)
                foreach (var pos in match.MatchedPositions)
                    toDestroy.Add(pos);

            foreach (var pos in toDestroy)
            {
                Cell cell = _board.GetCell(pos.x, pos.y);
                if (cell != null && cell.Fruit != null)
                    cell.Fruit = null;
            }
        }

        //Gravity
        public List<FruitMovement> ApplyGravity()
        {
            var movements = new List<FruitMovement>();
            bool anyMoved;

            do
            {
                anyMoved = false;

                for (int x = 0; x < _board.Width; x++)
                    for (int y = 0; y < _board.Height; y++)
                    {
                        var cell = _board.GetCell(x, y);
                        if (!cell.IsUsable || cell.Fruit == null) continue;

                        var downPos = new Vector2Int(x, y - 1);

                        if (_board.IsValid(downPos))
                        {
                            var downCell = _board.GetCell(downPos.x, downPos.y);

                            if (downCell.IsUsable && downCell.Fruit == null)
                            {
                                MoveFruit(cell, downCell, movements);
                                anyMoved = true;
                                continue;
                            }
                            else if (downCell.IsUsable && downCell.Fruit != null)
                                continue;
                        }

                        var diagLeft = new Vector2Int(x - 1, y - 1);
                        var diagRight = new Vector2Int(x + 1, y - 1);

                        foreach (var diagPos in new[] { diagLeft, diagRight })
                        {
                            if (!_board.IsValid(diagPos)) continue;
                            var diagCell = _board.GetCell(diagPos.x, diagPos.y);
                            if (!diagCell.IsUsable || diagCell.Fruit != null) continue;

                            MoveFruit(cell, diagCell, movements);
                            anyMoved = true;
                            break;
                        }
                    }
            }
            while (anyMoved);

            SpawnNewFruits(movements);
            return movements;
        }

        // Helpers
        private void UpdateOrAddMovement(List<FruitMovement> movements, Vector2Int from, Vector2Int to)
        {
            var existing = movements.Find(m => m.To == from);

            if (existing != null)
            {
                existing.To = to;
                existing.Path.Add(to);
            }
            else
            {
                movements.Add(new FruitMovement
                {
                    From = from,
                    To = to,
                    Path = new List<Vector2Int> { from, to }
                });
            }
        }

        private void SpawnNewFruits(List<FruitMovement> movements)
        {
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    var cell = _board.GetCell(x, y);
                    if (!cell.IsUsable || cell.Fruit != null) continue;

                    var spawnPoint = new Vector2Int(x, _board.Height);
                    cell.Fruit = _fruitFactory.CreateRandom();

                    var path = new List<Vector2Int>();
                    for (int py = _board.Height; py >= y; py--)
                        path.Add(new Vector2Int(x, py));

                    movements.Add(new FruitMovement
                    {
                        From = spawnPoint,
                        To = cell.Position,
                        Path = path
                    });
                }
        }

        private void FindLineMatches(List<MatchResult> results, Vector2Int direction)
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

        private void MoveFruit(Cell from, Cell to, List<FruitMovement> movements)
        {
            to.Fruit = from.Fruit;
            from.Fruit = null;
            UpdateOrAddMovement(movements, from.Position, to.Position);
        }

        // Input endpoint
        public bool TrySwap(Vector2Int from, Vector2Int to)
        {
            throw new System.NotImplementedException();
        }
    }
}