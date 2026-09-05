using UnityEngine;
using ZombieWar.Data;
using ZombieWar.UI;

namespace ZombieWar.Core
{
    // Menu-side counterpart of GameplayUiBinder. The menu is an overlay in the one scene the
    // game ships with, so this only reacts to the flow sitting in its Menu state.
    public sealed class MenuUiBinder : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private UIManager _ui;
        [SerializeField] private GameFlowController _flow;
        // Order matches the authored card slots in UIRoot.prefab.
        [SerializeField] private LevelDefinitionSO[] _levels;

        private readonly SaveService _save = new SaveService();
        private LevelCardData[] _cards;

        private void Awake()
        {
            if (_ui == null || _flow == null || _levels == null || _levels.Length == 0)
            {
                Debug.LogError($"{LogPrefix} MenuUiBinder has an unassigned reference - no level would be selectable.", this);
                return;
            }

            _cards = new LevelCardData[_levels.Length];
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
            if (state != GameState.Menu)
            {
                return;
            }

            RefreshCards();
            _ui.ShowMenuScreen(_cards, HandleLevelSelected);
        }

        private void RefreshCards()
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                LevelDefinitionSO level = _levels[i];
                if (level == null)
                {
                    Debug.LogError($"{LogPrefix} Empty level slot {i} on MenuUiBinder.", this);
                    continue;
                }

                _cards[i] = new LevelCardData(
                    level.LevelIndex,
                    level.DisplayName,
                    _save.IsLevelUnlocked(level.LevelIndex),
                    _save.GetBestScore(level.LevelIndex));
            }
        }

        private void HandleLevelSelected(int slot)
        {
            LevelDefinitionSO level = _levels[slot];
            if (level == null)
            {
                Debug.LogError($"{LogPrefix} Level slot {slot} has no definition.", this);
                return;
            }

            _flow.StartRun(level);
        }
    }
}
