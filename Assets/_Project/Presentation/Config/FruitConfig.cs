using Core.Domain.Enums;
using UnityEngine;

namespace Presentation.Config
{
    [CreateAssetMenu(fileName = "FruitConfig", menuName = "Match3/FruitConfig")]
    public class FruitConfig : ScriptableObject
    {
        [System.Serializable]
        public struct FruitEntry
        {
            public FruitType Type;
            public Sprite Sprite;
        }

        public FruitEntry[] Fruits;

        public Sprite GetSprite(FruitType type)
        {
            foreach (var entry in Fruits)
                if (entry.Type == type) return entry.Sprite;
            return null;
        }
    }
}