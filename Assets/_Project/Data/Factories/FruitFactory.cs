using System;
using Core.Domain;
using Core.Interfaces;

namespace Data.Factories
{
    public class FruitFactory : IFruitFactory
    {
        public Fruit CreateRandom()
        {
            var fruitTypes = Enum.GetValues(typeof(FruitType));
            var randomType = (FruitType)fruitTypes.GetValue(UnityEngine.Random.Range(0, fruitTypes.Length));
            return Create(randomType);
        }

        public Fruit Create(FruitType type)
        {
            return new Fruit(type);
        }
    }
}