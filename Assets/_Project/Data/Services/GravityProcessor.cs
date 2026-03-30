using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Interfaces;
using UnityEngine;

namespace Data.Services
{
    public class GravityProcessor
    {
        private readonly IFruitFactory _fruitFactory;

        public GravityProcessor(IFruitFactory fruitFactory)
        {
            _fruitFactory = fruitFactory;
        }

        public List<FruitMovement> Apply(Board board, Func<FruitType> resolveFruitType)
        {
            var movements = new List<FruitMovement>();
            bool anyAction;

            do
            {
                anyAction = false;

                while (ApplyVertical(board, movements))
                    anyAction = true;

                if (ApplyDiagonal(board, movements))
                    anyAction = true;

                if (TrySpawn(board, movements, resolveFruitType))
                    anyAction = true;

            } while (anyAction);

            return movements;
        }

        private bool ApplyVertical(Board board, List<FruitMovement> movements)
        {
            bool moved = false;
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable || cell.Fruit != null) continue;

                    var upPos = new Vector2Int(x, y + 1);
                    if (board.IsValid(upPos))
                    {
                        var upCell = board.GetCell(upPos.x, upPos.y);
                        if (upCell.IsUsable && upCell.Fruit != null)
                        {
                            MoveFruit(upCell, cell, movements);
                            moved = true;
                        }
                    }
                }
            }
            return moved;
        }

        private bool ApplyDiagonal(Board board, List<FruitMovement> movements)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable || cell.Fruit != null) continue;

                    var diagonals = new[] { new Vector2Int(x - 1, y + 1), new Vector2Int(x + 1, y + 1) };
                    foreach (var diagPos in diagonals)
                    {
                        if (!board.IsValid(diagPos)) continue;
                        var diagCell = board.GetCell(diagPos.x, diagPos.y);

                        if (diagCell.IsUsable && diagCell.Fruit != null)
                        {
                            var straightDown = new Vector2Int(diagPos.x, diagPos.y - 1);
                            bool canGoStraight = false;
                            if (board.IsValid(straightDown))
                            {
                                var target = board.GetCell(straightDown.x, straightDown.y);
                                if (target.IsUsable && target.Fruit == null) canGoStraight = true;
                            }

                            if (!canGoStraight)
                            {
                                MoveFruit(diagCell, cell, movements);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool TrySpawn(Board board, List<FruitMovement> movements, Func<FruitType> resolveFruitType)
        {
            bool spawned = false;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = board.Height - 1; y >= 0; y--)
                {
                    var cell = board.GetCell(x, y);
                    if (!cell.IsUsable) continue;

                    if (cell.Fruit == null && IsTopmostAvailableCell(board, x, y))
                    {
                        var fruitType = resolveFruitType();
                        cell.Fruit = _fruitFactory.Create(fruitType);

                        var spawnPoint = new Vector2Int(x, board.Height);
                        movements.Add(new FruitMovement
                        {
                            From = spawnPoint,
                            To = cell.Position,
                            Path = new List<Vector2Int> { spawnPoint, cell.Position },
                            SyncFruitType = (int)fruitType
                        });
                        spawned = true;
                        break;
                    }
                    if (cell.Fruit != null) break;
                }
            }
            return spawned;
        }

        // --- Helpers ---

        private void MoveFruit(Cell from, Cell to, List<FruitMovement> movements)
        {
            to.Fruit = from.Fruit;
            from.Fruit = null;
            UpdateOrAddMovement(movements, from.Position, to.Position);
        }

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

        private bool IsTopmostAvailableCell(Board board, int x, int y)
        {
            for (int nextY = y + 1; nextY < board.Height; nextY++)
            {
                if (board.GetCell(x, nextY).IsUsable) return false;
            }
            return true;
        }
    }
}