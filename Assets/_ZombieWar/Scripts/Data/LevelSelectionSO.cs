using UnityEngine;

namespace ZombieWar.Data
{
    // Runtime hand-off between the menu scene and the gameplay scene: the menu writes, the gameplay scene reads.
    [CreateAssetMenu(menuName = "Zombie War/Level Selection", fileName = "LevelSelection")]
    public sealed class LevelSelectionSO : ScriptableObject
    {
        [SerializeField] private LevelDefinitionSO _selected;

        public LevelDefinitionSO Selected => _selected;

        public void Select(LevelDefinitionSO level)
        {
            _selected = level;
        }
    }
}
