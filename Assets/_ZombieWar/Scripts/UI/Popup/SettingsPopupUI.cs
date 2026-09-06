using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // Menu-side settings sheet: sound, music, haptics and the same screen shake switch the
    // pause popup offers during a run.
    public sealed class SettingsPopupUI : PopupBase
    {
        private const string LogPrefix = "[Popup]";

        [SerializeField] private Button _closeButton;
        [SerializeField] private SwitchToggleView _soundSwitch;
        [SerializeField] private SwitchToggleView _musicSwitch;
        [SerializeField] private SwitchToggleView _hapticsSwitch;
        [SerializeField] private SwitchToggleView _shakeSwitch;

        private Action<SettingId, bool> _onChanged;

        protected override void Awake()
        {
            base.Awake();
            bool missing = _closeButton == null || _soundSwitch == null || _musicSwitch == null
                           || _hapticsSwitch == null || _shakeSwitch == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} SettingsPopupUI has an unassigned reference.", this);
                return;
            }

            _closeButton.onClick.AddListener(Close);
            _soundSwitch.OnValueChanged += HandleSoundChanged;
            _musicSwitch.OnValueChanged += HandleMusicChanged;
            _hapticsSwitch.OnValueChanged += HandleHapticsChanged;
            _shakeSwitch.OnValueChanged += HandleShakeChanged;
        }

        public void Setup(in SettingsData data, Action<SettingId, bool> onChanged)
        {
            _onChanged = onChanged;
            _soundSwitch.SetOnSilently(data.Sound);
            _musicSwitch.SetOnSilently(data.Music);
            _hapticsSwitch.SetOnSilently(data.Haptics);
            _shakeSwitch.SetOnSilently(data.CameraShake);
        }

        private void HandleSoundChanged(bool on) => _onChanged?.Invoke(SettingId.Sound, on);

        private void HandleMusicChanged(bool on) => _onChanged?.Invoke(SettingId.Music, on);

        private void HandleHapticsChanged(bool on) => _onChanged?.Invoke(SettingId.Haptics, on);

        private void HandleShakeChanged(bool on) => _onChanged?.Invoke(SettingId.CameraShake, on);
    }
}
