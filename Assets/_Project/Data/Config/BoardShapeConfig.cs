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

            [TextArea]
            public string MaskPattern;

            public int[,] GetMask()
            {
                var rows = MaskPattern
                    .Trim()
                    .Split('\n');

                int height = rows.Length;
                int width  = rows[0].Trim().Length;

                var mask = new int[width, height];

                for (int y = 0; y < height; y++)
                {
                    var row = rows[y].Trim();
                    for (int x = 0; x < width; x++)
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