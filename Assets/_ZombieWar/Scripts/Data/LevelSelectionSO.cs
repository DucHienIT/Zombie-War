using UnityEngine;

namespace ZombieWar.Data
{
    // Carries one level from outside the Gameplay scene into it: the editor cheat window stamps
    // a level here before entering play mode so the fresh scene skips the menu. Runs themselves
    // never write it - retry and next level swap in place inside the one Gameplay scene.
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
