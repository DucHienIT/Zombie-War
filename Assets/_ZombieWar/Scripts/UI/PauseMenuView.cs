using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Audio;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private AudioService _audio;
        [SerializeField] private Canvas _overlayCanvas;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private AudioClip _tapClip;

        private void Awake()
        {
            bool missing = _flow == null || _audio == null || _overlayCanvas == null || _pauseButton == null || _resumeButton == null || _menuButton == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} PauseMenuView has an unassigned reference.", this);
                return;
            }

            _pauseButton.onClick.AddListener(HandlePauseClicked);
            _resumeButton.onClick.AddListener(HandleResumeClicked);
            _menuButton.onClick.AddListener(HandleMenuClicked);
        }

        private void OnEnable()
        {
            _flow.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            _flow.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            _overlayCanvas.enabled = state == GameState.Paused;
            _pauseButton.interactable = state == GameState.Playing;
        }

        private void HandlePauseClicked()
        {
            _audio.PlayUi(_tapClip);
            _flow.Pause();
        }

        private void HandleResumeClicked()
        {
            _audio.PlayUi(_tapClip);
            _flow.Resume();
        }

        private void HandleMenuClicked()
        {
            _audio.PlayUi(_tapClip);
            _flow.GoToMenu();
        }
    }
}
