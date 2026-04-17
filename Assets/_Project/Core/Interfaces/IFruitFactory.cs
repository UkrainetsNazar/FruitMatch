using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Interfaces
{
    public interface IFruitFactory
    {
        void SetFruitTypeCount(int count);
        Fruit CreateRandom();
        Fruit Create(FruitType type);
    }
}