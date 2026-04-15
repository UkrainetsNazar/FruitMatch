using Core.Interfaces;
using DG.Tweening;
using Infrastructure.Audio;
using Presentation.Animations;
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
        [SerializeField] protected PanelAnimator resultPanel;
        [SerializeField] protected TMP_Text finalPlayerScore;
        [SerializeField] protected ButtonAnimator _returnButton;
        [SerializeField] protected TMP_Text musicVolumeText, sfxVolumeText;
        [SerializeField] protected Slider musicVolumeSlider, sfxVolumeSlider;
        [SerializeField] protected PanelAnimator settingsPanel;

        [Inject] protected IGameStateService _gameState;

        public bool IsVisible => gameObject.activeSelf;

        protected virtual void Start()
        {
            _gameState.OnDataUpdated += RefreshUI;

            if (settingsPanel != null) settingsPanel.Hide();
            if (musicVolumeSlider != null) VolumeSliderBinder.BindMusic(musicVolumeSlider, musicVolumeText);
            if (sfxVolumeSlider != null) VolumeSliderBinder.BindSfx(sfxVolumeSlider, sfxVolumeText);
        }

        protected virtual void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel != null)
                if (settingsPanel.IsVisible) settingsPanel.Hide(); else settingsPanel.Show();
        }

        protected virtual void OnDestroy()
        {
            if (_gameState != null)
                _gameState.OnDataUpdated -= RefreshUI;
        }

        protected async void ShowMultiplierOn(TMP_Text target, int combo)
        {
            if (combo <= 1 || target == null) return;

            target.text = $"x{combo}";
            target.gameObject.SetActive(true);
            target.transform.DOKill();
            target.transform.localScale = Vector3.zero;

            var sequence = DOTween.Sequence();
            sequence.Append(target.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
            sequence.Append(target.transform.DOScale(1f, 0.1f));
            sequence.AppendInterval(0.8f);
            sequence.Append(target.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
            sequence.OnComplete(() => target.gameObject.SetActive(false));

            await sequence.AsyncWaitForCompletion();
        }

        protected abstract void RefreshUI();
    }
}