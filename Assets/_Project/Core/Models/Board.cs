using UnityEngine;

namespace Core.Domain
{
    public class Board
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly Cell[,] _cells;

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[x, y] = new Cell(new Vector2Int(x, y), isUsable: true);
        }

        public Board(int[,] mask)
        {
            Width = mask.GetLength(0);
            Height = mask.GetLength(1);
            _cells = new Cell[Width, Height];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    _cells[x, y] = new Cell(new Vector2Int(x, y), isUsable: mask[x, y] == 1);
        }

        public Cell GetCell(Vector2Int pos) => _cells[pos.x, pos.y];
        public bool IsValid(Vector2Int pos) =>
            pos.x >= 0 && pos.x < Width &&
            pos.y >= 0 && pos.y < Height &&
            _cells[pos.x, pos.y].IsUsable;
    }
}
