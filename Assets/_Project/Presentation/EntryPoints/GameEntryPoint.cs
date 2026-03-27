using Infrastructure.Network;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
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