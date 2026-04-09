using System;

namespace Core.Domain
{
    public class Fruit
    {
        public FruitType Type { get; private set; }

        public Fruit(FruitType type)
        {
            Type = type;
        }
    }
}