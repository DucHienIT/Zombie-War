using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // A uGUI Toggle skinned as an on/off switch. Toggle only fades its single checkmark
    // graphic, while the pack's switch is a frame plus a handle per state, so this swaps
    // two whole visuals instead.
    public sealed class SwitchToggleView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Toggle _toggle;
        [SerializeField] private GameObject _onVisual;
        [SerializeField] private GameObject _offVisual;

        public event Action<bool> OnValueChanged;

        public bool IsOn => _toggle.isOn;

        private void Awake()
        {
            if (_toggle == null || _onVisual == null || _offVisual == null)
            {
                Debug.LogError($"{LogPrefix} SwitchToggleView has an unassigned reference.", this);
                return;
            }

            _toggle.onValueChanged.AddListener(HandleValueChanged);
            Refresh();
        }

        // Presets the switch without raising OnValueChanged; used when a popup opens.
        public void SetOnSilently(bool on)
        {
            _toggle.SetIsOnWithoutNotify(on);
            Refresh();
        }

        private void HandleValueChanged(bool on)
        {
            Refresh();
            OnValueChanged?.Invoke(on);
        }

        private void Refresh()
        {
            bool on = _toggle.isOn;
            _onVisual.SetActive(on);
            _offVisual.SetActive(!on);
        }
    }
}
