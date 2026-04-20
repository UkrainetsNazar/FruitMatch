using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Infrastructure.PostProcessing
{
    public class PostProcessingController : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Volume _volume;

        [Header("Camera Shake")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _shakeStrength = 0.08f;
        [SerializeField] private float _shakeDuration = 0.25f;
        [SerializeField] private int _shakeVibrato = 15;

        private Bloom _bloom;
        private Vignette _vignette;
        private ChromaticAberration _chromatic;

        private float _baseBloomIntensity;
        private float _baseVignetteIntensity;

        private static PostProcessingController _instance;
        public static PostProcessingController Instance => _instance;

        private void Awake()
        {
            _instance = this;

            if (_volume == null)
                _volume = FindObjectOfType<Volume>();

            if (_volume != null)
            {
                _volume.profile.TryGet(out _bloom);
                _volume.profile.TryGet(out _vignette);
                _volume.profile.TryGet(out _chromatic);
            }

            if (_bloom != null) _baseBloomIntensity = _bloom.intensity.value;
            if (_vignette != null) _baseVignetteIntensity = _vignette.intensity.value;

            if (_camera == null)
                _camera = Camera.main;
        }

        // ── Destroy ───────────────────────────────────────────

        public static void OnFruitsDestroyed(int count)
            => _instance?.PlayDestroyEffect(count).Forget();

        private async UniTaskVoid PlayDestroyEffect(int count)
        {
            float intensity = Mathf.Clamp(count * 0.3f, 0.5f, 2.5f);

            if (_bloom != null)
            {
                DOTween.To(
                    () => _bloom.intensity.value,
                    v => _bloom.intensity.Override(v),
                    _baseBloomIntensity + intensity,
                    0.1f
                ).OnComplete(() =>
                    DOTween.To(
                        () => _bloom.intensity.value,
                        v => _bloom.intensity.Override(v),
                        _baseBloomIntensity,
                        0.3f
                    )
                );
            }

            if (_vignette != null)
            {
                DOTween.To(
                    () => _vignette.intensity.value,
                    v => _vignette.intensity.Override(v),
                    _baseVignetteIntensity + 0.2f,
                    0.1f
                ).OnComplete(() =>
                    DOTween.To(
                        () => _vignette.intensity.value,
                        v => _vignette.intensity.Override(v),
                        _baseVignetteIntensity,
                        0.4f
                    )
                );
            }

            if (_camera != null && count >= 3)
            {
                float strength = Mathf.Clamp(count * 0.02f, 0.02f, _shakeStrength);
                _camera.transform.DOShakePosition(
                    _shakeDuration,
                    strength,
                    _shakeVibrato,
                    randomnessMode: ShakeRandomnessMode.Harmonic
                );
            }

            await UniTask.CompletedTask;
        }

        // ── Combo ─────────────────────────────────────────────

        public static void OnCombo(int comboLevel)
            => _instance?.PlayComboEffect(comboLevel).Forget();

        private async UniTaskVoid PlayComboEffect(int comboLevel)
        {
            if (comboLevel < 2) return;

            if (_chromatic != null)
            {
                float target = Mathf.Clamp((comboLevel - 1) * 0.2f, 0.1f, 0.8f);

                DOTween.To(
                    () => _chromatic.intensity.value,
                    v => _chromatic.intensity.Override(v),
                    target,
                    0.1f
                ).OnComplete(() =>
                    DOTween.To(
                        () => _chromatic.intensity.value,
                        v => _chromatic.intensity.Override(v),
                        0f,
                        0.5f
                    )
                );
            }

            if (_bloom != null)
            {
                float boost = comboLevel * 0.4f;
                DOTween.To(
                    () => _bloom.intensity.value,
                    v => _bloom.intensity.Override(v),
                    _baseBloomIntensity + boost,
                    0.15f
                ).OnComplete(() =>
                    DOTween.To(
                        () => _bloom.intensity.value,
                        v => _bloom.intensity.Override(v),
                        _baseBloomIntensity,
                        0.6f
                    )
                );
            }

            await UniTask.CompletedTask;
        }

        // ── Game Start ────────────────────────────────────────

        public static void OnGameStart()
            => _instance?.PlayStartEffect().Forget();

        private async UniTaskVoid PlayStartEffect()
        {
            if (_vignette == null) return;

            _vignette.intensity.Override(0.7f);

            DOTween.To(
                () => _vignette.intensity.value,
                v => _vignette.intensity.Override(v),
                _baseVignetteIntensity,
                1.2f
            ).SetEase(Ease.OutCubic);

            await UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            if (_bloom != null) _bloom.intensity.Override(_baseBloomIntensity);
            if (_vignette != null) _vignette.intensity.Override(_baseVignetteIntensity);
            if (_chromatic != null) _chromatic.intensity.Override(0f);
        }
    }
}