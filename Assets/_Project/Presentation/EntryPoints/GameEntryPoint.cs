using Core.Interfaces;
using UnityEngine;
using Zenject;

namespace Presentation.EntryPoints
{
    public class GameEntryPoint : MonoBehaviour
    {
        [Inject] private IGameController _gameController;

        private async void Start()
        {
            await _gameController.StartGame();
        }
    }
}