using System;

namespace Core.Domain
{
    public class Fruit
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public FruitType Type { get; private set; }
        public bool IsMatched  { get; set; }

        public Fruit(FruitType type)
        {
            Type = type;
        }
    }
}