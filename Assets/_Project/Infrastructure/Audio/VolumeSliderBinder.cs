using Infrastructure.Audio;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace Infrastructure.Audio
{
    public static class VolumeSliderBinder
    {
        public static void BindMusic(Slider slider, TMP_Text label)
        {
            Setup(slider, AudioManager.GetMusicVolume());
            UpdateLabel(label, slider.value);
            slider.onValueChanged.AddListener(v =>
            {
                AudioManager.SetMusicVolume(v / 100f);
                UpdateLabel(label, v);
            });
        }

        public static void BindSfx(Slider slider, TMP_Text label)
        {
            Setup(slider, AudioManager.GetSfxVolume());
            UpdateLabel(label, slider.value);
            slider.onValueChanged.AddListener(v =>
            {
                AudioManager.SetSfxVolume(v / 100f);
                UpdateLabel(label, v);
            });
        }

        private static void Setup(Slider slider, float savedVolume)
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.value = Mathf.RoundToInt(savedVolume * 100f);
        }

        private static void UpdateLabel(TMP_Text label, float value) =>
            label.text = $"{(int)value}%";
    }
}