using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.UI;

namespace ZombieWar.Core
{
    // The Loading scene is the only way into Gameplay: first launch, retry, next level and
    // "back to menu" all pass through it, so pools, physics and the map always start from a
    // fresh Gameplay scene while the player looks at a real progress bar instead of a freeze.
    public sealed class LoadingSceneController : MonoBehaviour
    {
        private const string LogPrefix = "[Loading]";

        // Unity parks an async load at this progress while allowSceneActivation is false.
        private const float ActivationHoldProgress = 0.9f;

        [SerializeField] private LoadingView _view;
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        [Header("Pacing")]
        [SerializeField] private float _minimumDisplaySeconds = 1.2f;
        [SerializeField] private float _fillPerSecond = 1.6f;

        private AsyncOperation _load;
        private float _elapsed;
        private float _shownProgress;

        private void Awake()
        {
            if (_view == null)
            {
                Debug.LogError($"{LogPrefix} LoadingSceneController has no view assigned.", this);
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            _load = SceneManager.LoadSceneAsync(_gameplaySceneName, LoadSceneMode.Single);
            if (_load == null)
            {
                Debug.LogError($"{LogPrefix} Scene '{_gameplaySceneName}' is not in the build settings.", this);
                enabled = false;
                return;
            }

            _load.allowSceneActivation = false;
            _view.SetProgress(0f);
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            _elapsed += deltaTime;

            float target = Mathf.Clamp01(_load.progress / ActivationHoldProgress);
            _shownProgress = Mathf.MoveTowards(_shownProgress, target, _fillPerSecond * deltaTime);
            _view.SetProgress(_shownProgress);

            bool barFull = _shownProgress >= 1f;
            bool sceneReady = _load.progress >= ActivationHoldProgress;
            bool shownLongEnough = _elapsed >= _minimumDisplaySeconds;
            if (barFull && sceneReady && shownLongEnough)
            {
                _load.allowSceneActivation = true;
                enabled = false;
            }
        }
    }
}
