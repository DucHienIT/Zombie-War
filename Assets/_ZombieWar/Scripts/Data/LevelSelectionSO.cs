using UnityEngine;

namespace ZombieWar.Data
{
    // The game runs in a single scene, so this asset only has to survive a scene reload:
    // retry and "next level" reload the scene with AutoStart set, everything else lands
    // on the menu overlay.
    [CreateAssetMenu(menuName = "Zombie War/Level Selection", fileName = "LevelSelection")]
    public sealed class LevelSelectionSO : ScriptableObject
    {
        [SerializeField] private LevelDefinitionSO _selected;
        [SerializeField] private bool _autoStart;

        public LevelDefinitionSO Selected => _selected;

        public void Select(LevelDefinitionSO level, bool autoStart)
        {
            _selected = level;
            _autoStart = autoStart;
        }

        // Reading clears the flag: a ScriptableObject keeps runtime edits in the editor, and
        // a stale flag would make the next Play skip the menu for no reason.
        public bool ConsumeAutoStart()
        {
            bool autoStart = _autoStart;
            _autoStart = false;
            return autoStart;
        }
    }
}
