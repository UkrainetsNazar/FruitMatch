using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Interfaces;
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

        public void Initialize(Board board)
        {
            _board = board;
            for (int i = 0; i < _board.Width; i++)
                for (int j = 0; j < _board.Height; j++)
                {
                    Cell cell = board.GetCell(new Vector2Int(i, j));
                    if (cell.IsUsable)
                    {
                        cell.Fruit = _fruitFactory.CreateRandom();
                    }
                }

            OnBoardInitialized?.Invoke();
        }

        public bool TrySwap(Vector2Int from, Vector2Int to)
        {
            throw new System.NotImplementedException();
        }

        public List<MatchResult> FindMatches()
        {
            throw new System.NotImplementedException();
        }

        public void ProcessMatches(List<MatchResult> matches)
        {
            throw new System.NotImplementedException();
        }
    }
}