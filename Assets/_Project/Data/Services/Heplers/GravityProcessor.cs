using System;
using System.Collections.Generic;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.ValueObjects;
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
            bool changed;

            do
            {
                changed = false;

                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        var cell = board.GetCell(x, y);
                        if (cell.IsUsable && cell.Fruit == null)
                        {
                            if (TryMoveFrom(board, cell, x, y + 1, movements, false))
                            {
                                changed = true;
                            }
                        }
                    }
                }

                if (changed) continue;

                if (TrySpawn(board, movements, resolveFruitType))
                {
                    changed = true;
                    continue;
                }

                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        var cell = board.GetCell(x, y);
                        if (cell.IsUsable && cell.Fruit == null)
                        {
                            if (IsPathAboveBlocked(board, x, y))
                            {
                                if (TryMoveFrom(board, cell, x - 1, y + 1, movements, true) ||
                                    TryMoveFrom(board, cell, x + 1, y + 1, movements, true))
                                {
                                    changed = true;
                                }
                            }
                        }
                    }
                }

            } while (changed);

            return movements;
        }

        private bool IsPathAboveBlocked(Board board, int x, int y)
        {
            var above = new Vector2Int(x, y + 1);
            if (!board.IsValid(above)) return true;

            var aboveCell = board.GetCell(above.x, above.y);
            return !aboveCell.IsUsable || aboveCell.Fruit != null;
        }

        private bool TryMoveFrom(Board board, Cell targetCell, int sourceX, int sourceY, List<FruitMovement> movements, bool isDiagonal)
        {
            if (!board.IsValid(new Vector2Int(sourceX, sourceY))) return false;
            var sourceCell = board.GetCell(sourceX, sourceY);

            if (!sourceCell.IsUsable || sourceCell.Fruit == null) return false;

            if (isDiagonal)
            {
                var straightDown = new Vector2Int(sourceX, sourceY - 1);
                if (board.IsValid(straightDown))
                {
                    var cellBelowSource = board.GetCell(straightDown.x, straightDown.y);
                    if (cellBelowSource.IsUsable && cellBelowSource.Fruit == null)
                        return false;
                }
            }

            MoveFruit(sourceCell, targetCell, movements);
            return true;
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

                    if (cell.Fruit != null) break;

                    if (IsTopmostAvailableCell(board, x, y))
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
                    }
                    break;
                }
            }
            return spawned;
        }

        // Heplers

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