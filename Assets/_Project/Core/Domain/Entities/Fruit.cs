using Core.Domain.Enums;

namespace Core.Domain.Entities
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