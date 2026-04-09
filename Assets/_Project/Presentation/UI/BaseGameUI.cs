using Core.Interfaces;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace Presentation.UI
{
    public abstract class BaseGameUI : MonoBehaviour
    {
        [SerializeField] protected TMP_Text playerScore, playerMoves;
        [SerializeField] protected GameObject resultPanel;
        [SerializeField] protected TMP_Text finalPlayerScore;
        [SerializeField] protected Button _returnButton;

        [Inject] protected IGameStateService _gameState;

        protected virtual void Start()
        {
            _gameState.OnDataUpdated += RefreshUI;
        }

        protected virtual void OnDestroy()
        {
            if (_gameState != null)
                _gameState.OnDataUpdated -= RefreshUI;
        }

        protected abstract void RefreshUI();
    }
}