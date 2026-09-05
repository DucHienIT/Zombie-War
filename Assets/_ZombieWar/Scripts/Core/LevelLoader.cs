using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.Data;

namespace ZombieWar.Core
{
    // The game ships as one scene. Starting a level from the menu happens in place; retry,
    // next level and "back to menu" reload that same scene, which is the cheapest way to
    // guarantee pools, physics and the map start from a clean slate.
    public sealed class LevelLoader : MonoBehaviour
    {
        private const string LogPrefix = "[Level]";

        [SerializeField] private LevelSelectionSO _selection;

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
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
