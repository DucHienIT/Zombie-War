using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class PausePopupUI : PopupBase
    {
        private const string LogPrefix = "[Popup]";

        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        [Header("Settings")]
        [SerializeField] private SwitchToggleView _shakeSwitch;

        private Action _onResume;
        private Action _onRestart;
        private Action _onMenu;
        private Action<bool> _onShakeChanged;
        private Action _exitAction;

        protected override void Awake()
        {
            base.Awake();
            if (_resumeButton == null || _restartButton == null || _menuButton == null || _shakeSwitch == null)
            {
                Debug.LogError($"{LogPrefix} PausePopupUI has an unassigned reference.", this);
                return;
            }

            _resumeButton.onClick.AddListener(HandleResumeClicked);
            _restartButton.onClick.AddListener(HandleRestartClicked);
            _menuButton.onClick.AddListener(HandleMenuClicked);
            _shakeSwitch.OnValueChanged += HandleShakeChanged;
        }

        public void Setup(Action onResume, Action onRestart, Action onMenu, bool shakeEnabled, Action<bool> onShakeChanged)
        {
            _onResume = onResume;
            _onRestart = onRestart;
            _onMenu = onMenu;
            _onShakeChanged = onShakeChanged;
            _shakeSwitch.SetOnSilently(shakeEnabled);
            // Any close the player did not choose explicitly means "resume the run".
            _exitAction = onResume;
        }

        protected override void OnClosed()
        {
            Action exit = _exitAction;
            _exitAction = _onResume;
            exit?.Invoke();
        }

        private void HandleResumeClicked()
        {
            _exitAction = _onResume;
            Close();
        }

        private void HandleRestartClicked()
        {
            _exitAction = _onRestart;
            Close();
        }

        private void HandleMenuClicked()
        {
            _exitAction = _onMenu;
            Close();
        }

        private void HandleShakeChanged(bool enabled) => _onShakeChanged?.Invoke(enabled);
    }
}
