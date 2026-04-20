using Core.Interfaces;
using DG.Tweening;
using Infrastructure.PostProcessing;
using UnityEngine;
using Zenject;

namespace Presentation.EntryPoints
{
    public class GameEntryPoint : MonoBehaviour
    {
        [Inject] private IGameController _gameController;

        private void Awake()
        {
            DOTween.SetTweensCapacity(500, 125);
        }

        private async void Start()
        {
            PostProcessingController.OnGameStart();
            await _gameController.StartGame();
        }
    }
}