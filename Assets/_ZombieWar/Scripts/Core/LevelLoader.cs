using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Core
{
    // The one thing that has to survive the trip from the Loading scene into Gameplay: which
    // level, if any, the fresh scene should drop straight into instead of showing the menu.
    // Retry, next level and "back to menu" no longer come through here - GameFlowController
    // swaps runs in place behind the loading panel - so at runtime this is read-only.
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
    }
}
