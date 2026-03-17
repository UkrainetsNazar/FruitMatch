using UnityEngine;

namespace Data.Config
{
    [CreateAssetMenu(fileName = "BoardShapeConfig", menuName = "Match3/BoardShapeConfig")]
    public class BoardShapeConfig : ScriptableObject
    {
        [System.Serializable]
        public class BoardShape
        {
            public string Name;
            public int Width;
            public int Height;

            [TextArea]
            public string MaskPattern;

            public int[,] GetMask()
            {
                var rows = MaskPattern
                    .Trim()
                    .Split('\n');

                var mask = new int[Width, Height];

                for (int y = 0; y < Height; y++)
                {
                    var row = rows[y].Trim();
                    for (int x = 0; x < Width; x++)
                        mask[x, y] = row[x] == '1' ? 1 : 0;
                }

                return mask;
            }
        }

        public BoardShape[] Shapes;

        public BoardShape GetRandom()
        {
            if (Shapes == null || Shapes.Length == 0)
                return null;

            return Shapes[Random.Range(0, Shapes.Length)];
        }
    }
}