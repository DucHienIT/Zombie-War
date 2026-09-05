using UnityEngine;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Skill Tree", fileName = "SkillTree")]
    public sealed class SkillTreeSO : ScriptableObject
    {
        // Order is the slot order of the skill tree page. Every prerequisite must be listed too.
        [SerializeField] private SkillTreeNodeSO[] _nodes;

        public SkillTreeNodeSO[] Nodes => _nodes;

        public int IndexOf(SkillTreeNodeSO node)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] == node)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
