using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.Data;

namespace ZombieWar.Core
{
    public sealed class LevelLoader : MonoBehaviour
    {
        private const string LogPrefix = "[Level]";

        [SerializeField] private LevelSelectionSO _selection;
        [SerializeField] private string _menuSceneName = "MainMenu";
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        private void Awake()
        {
            if (_selection == null)
            {
                Debug.LogError($"{LogPrefix} LevelLoader has no level selection asset.", this);
            }
        }

        public void LoadLevel(LevelDefinitionSO level)
        {
            _selection.Select(level);
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(_gameplaySceneName);
        }

        public void LoadMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(_menuSceneName);
        }

        public void ReloadCurrent()
        {
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
