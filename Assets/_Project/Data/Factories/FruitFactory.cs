using System;
using Core.Domain;
using Core.Interfaces;
using UnityEngine;

namespace Data.Factories
{
    public class FruitFactory : IFruitFactory
    {
        private int _fruitTypeCount = 7;

        public void SetFruitTypeCount(int count)
        {
            _fruitTypeCount = Mathf.Clamp(count, 1,
                Enum.GetValues(typeof(FruitType)).Length);
        }

        public Fruit CreateRandom()
        {
            var randomType = (FruitType)UnityEngine.Random.Range(0, _fruitTypeCount);
            return Create(randomType);
        }

        public Fruit Create(FruitType type) => new Fruit(type);
    }
}