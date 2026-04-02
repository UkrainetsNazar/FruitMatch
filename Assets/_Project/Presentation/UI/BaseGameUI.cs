using Core.Interfaces;
using TMPro;
using UnityEngine;
using Zenject;

namespace Presentation.UI
{
    public abstract class BaseGameUI : MonoBehaviour
    {
        [SerializeField] protected TMP_Text playerScore, playerMoves;

        [Inject] protected IGameStateService _gameState;

        protected virtual void Start()
        {
            _gameState.OnDataUpdated += RefreshUI;
        }

        protected abstract void RefreshUI();
    }
}