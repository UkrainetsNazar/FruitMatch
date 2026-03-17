using Core.Interfaces;
using UnityEngine;
using Zenject;

namespace Presentation.EntryPoints
{
    public class GameEntryPoint : MonoBehaviour
    {
        [Inject] private IBoardFactory _boardFactory;

        private void Start()
        {
            _boardFactory.CreateRandom();
        }
    }
}