using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // The only entry point into the UI tree. It holds no reference that points outside
    // UIRoot.prefab: gameplay reaches it through a binder that lives in the scene, and the
    // UI reaches gameplay only through the Action fields that binder hands over.
    public sealed class UIManager : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [Header("Screens")]
        [SerializeField] private Canvas _hudCanvas;
        [SerializeField] private MenuScreenView _menuScreen;
        [SerializeField] private CountdownView _countdown;
        [SerializeField] private PopupManager _popups;

        [Header("HUD")]
        [SerializeField] private HealthBarView _healthBar;
        [SerializeField] private TimerView _timer;
        [SerializeField] private ScoreView _score;
        [SerializeField] private GunHudView _gunHud;
        [SerializeField] private BombButtonView _bombButton;
        [SerializeField] private Button _pauseButton;

        [Header("Popups")]
        [SerializeField] private PausePopupUI _pausePopup;
        [SerializeField] private ResultPopupUI _resultPopup;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _tapClip;
        [SerializeField] private AudioClip _gunSwitchClip;
        [SerializeField] private AudioClip _bombReadyClip;
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioClip _loseClip;

        private Action _onPauseRequested;
        private Action _onSwitchGunRequested;
        private Action _onThrowBombRequested;
        private bool _wasBombReady = true;
        private int _bombCharges;

        private void Awake()
        {
            bool missing = _hudCanvas == null || _menuScreen == null || _countdown == null || _popups == null
                           || _healthBar == null || _timer == null || _score == null || _gunHud == null
                           || _bombButton == null || _pauseButton == null || _pausePopup == null
                           || _resultPopup == null || _audioSource == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} UIManager has an unassigned reference - the HUD would run blind.", this);
                return;
            }

            _pauseButton.onClick.AddListener(HandlePauseClicked);
            _gunHud.Init(HandleSwitchGunClicked);
            _bombButton.Init(HandleThrowBombClicked);
        }

        public void BindGameplayCommands(Action onPause, Action onSwitchGun, Action onThrowBomb)
        {
            _onPauseRequested = onPause;
            _onSwitchGunRequested = onSwitchGun;
            _onThrowBombRequested = onThrowBomb;
        }

        public void ShowGameplayScreen()
        {
            _menuScreen.SetVisible(false);
            _hudCanvas.enabled = true;
        }

        public void ShowMenuScreen(LevelCardData[] cards, Action<int> onLevelSelected)
        {
            _hudCanvas.enabled = false;
            _countdown.SetVisible(false);
            _menuScreen.SetVisible(true);
            _menuScreen.Bind(cards, onLevelSelected, PlayTap);
        }

        public void SetHudVisible(bool visible) => _hudCanvas.enabled = visible;

        public void SetPauseButtonInteractable(bool interactable) => _pauseButton.interactable = interactable;

        public void SetHealth(float normalized) => _healthBar.SetHealth(normalized);

        public void SetRemainingTime(float seconds) => _timer.SetRemaining(seconds);

        public void SetScore(int kills, int score) => _score.SetScore(kills, score);

        public void SetGun(Sprite icon, string displayName) => _gunHud.SetGun(icon, displayName);

        public void SetAmmo(int ammo, int magazine) => _gunHud.SetAmmo(ammo, magazine);

        public void SetReloadProgress(float progress) => _gunHud.SetReloadProgress(progress);

        public void SetBombCharges(int charges)
        {
            _bombCharges = charges;
            _bombButton.SetCharges(charges);
        }

        public void ShowCountdown(bool visible) => _countdown.SetVisible(visible);

        public void SetCountdownSeconds(int seconds) => _countdown.SetSeconds(seconds);

        public void SetBombCooldown(float progress)
        {
            _bombButton.SetCooldown(progress);
            bool ready = progress >= 1f;
            if (ready && !_wasBombReady && _bombCharges > 0)
            {
                Play(_bombReadyClip);
                _bombButton.PlayReady();
            }

            _wasBombReady = ready;
        }

        public void ShowPausePopup(Action onResume, Action onRestart, Action onMenu, bool shakeEnabled, Action<bool> onShakeChanged)
        {
            _pausePopup.Setup(Wrap(onResume), Wrap(onRestart), Wrap(onMenu), shakeEnabled, Wrap(onShakeChanged));
            _popups.Show(_pausePopup);
        }

        // The caller banks the score before this runs, so the panel only ever draws.
        public void ShowResultPopup(in ResultData data, Action onRetry, Action onNext, Action onMenu)
        {
            Play(data.Won ? _winClip : _loseClip);
            _resultPopup.Setup(data, Wrap(onRetry), Wrap(onNext), Wrap(onMenu));
            _popups.Show(_resultPopup);
        }

        public void PlayTap() => Play(_tapClip);

        // Every button that leaves the UI makes the same click, so the tap lives here once.
        private Action Wrap(Action action)
        {
            if (action == null)
            {
                return null;
            }

            return () =>
            {
                PlayTap();
                action();
            };
        }

        private Action<bool> Wrap(Action<bool> action)
        {
            if (action == null)
            {
                return null;
            }

            return value =>
            {
                PlayTap();
                action(value);
            };
        }

        private void HandlePauseClicked()
        {
            PlayTap();
            _onPauseRequested?.Invoke();
        }

        private void HandleSwitchGunClicked()
        {
            Play(_gunSwitchClip);
            _onSwitchGunRequested?.Invoke();
        }

        private void HandleThrowBombClicked() => _onThrowBombRequested?.Invoke();

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
