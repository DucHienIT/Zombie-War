using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // Menu-side settings sheet; the same switches the pause popup offers during a run.
    public sealed class SettingsPopupUI : PopupBase
    {
        private const string LogPrefix = "[Popup]";

        [SerializeField] private Button _closeButton;
        [SerializeField] private SwitchToggleView _shakeSwitch;

        private Action<bool> _onShakeChanged;

        protected override void Awake()
        {
            base.Awake();
            if (_closeButton == null || _shakeSwitch == null)
            {
                Debug.LogError($"{LogPrefix} SettingsPopupUI has an unassigned reference.", this);
                return;
            }

            _closeButton.onClick.AddListener(Close);
            _shakeSwitch.OnValueChanged += HandleShakeChanged;
        }

        public void Setup(bool shakeEnabled, Action<bool> onShakeChanged)
        {
            _onShakeChanged = onShakeChanged;
            _shakeSwitch.SetOnSilently(shakeEnabled);
        }

        private void HandleShakeChanged(bool enabled) => _onShakeChanged?.Invoke(enabled);
    }
}
