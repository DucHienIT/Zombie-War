using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.Data;

namespace ZombieWar.Core
{
    // Starting a level from the menu happens in place; retry, next level and "back to menu"
    // go through the Loading scene, which loads a fresh Gameplay scene behind a progress bar.
    // A fresh scene is the cheapest way to guarantee pools, physics and the map start clean.
    public sealed class LevelLoader : MonoBehaviour
    {
        private const string LogPrefix = "[Level]";

        [SerializeField] private LevelSelectionSO _selection;
        [SerializeField] private string _loadingSceneName = "Loading";

        public LevelDefinitionSO PendingLevel => _selection.Selected;

        private void Awake()
        {
            if (_selection == null)
            {
                Debug.LogError($"{LogPrefix} LevelLoader has no level selection asset.", this);
            }
        }

        public bool ConsumeAutoStart() => _selection.ConsumeAutoStart();

        public void Remember(LevelDefinitionSO level) => _selection.Select(level, false);

        public void RestartWith(LevelDefinitionSO level)
        {
            _selection.Select(level, true);
            Reload();
        }

        public void ReturnToMenu()
        {
            _selection.Select(null, false);
            Reload();
        }

        private void Reload()
        {
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Single);
        }
    }
}
