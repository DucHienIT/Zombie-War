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
        [SerializeField] private Button _settingsButton;

        private Action _onResume;
        private Action _onRestart;
        private Action _onMenu;
        private Action _onSettings;
        private Action _exitAction;

        protected override void Awake()
        {
            base.Awake();
            if (_resumeButton == null || _restartButton == null || _menuButton == null || _settingsButton == null)
            {
                Debug.LogError($"{LogPrefix} PausePopupUI has an unassigned reference.", this);
                return;
            }

            _resumeButton.onClick.AddListener(HandleResumeClicked);
            _restartButton.onClick.AddListener(HandleRestartClicked);
            _menuButton.onClick.AddListener(HandleMenuClicked);
            _settingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        public void Setup(Action onResume, Action onRestart, Action onMenu, Action onSettings)
        {
            _onResume = onResume;
            _onRestart = onRestart;
            _onMenu = onMenu;
            _onSettings = onSettings;
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

        // The settings sheet stacks on top; the pause popup stays open underneath it.
        private void HandleSettingsClicked() => _onSettings?.Invoke();
    }
}
