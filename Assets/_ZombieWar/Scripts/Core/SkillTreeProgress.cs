using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Core
{
    // Ranks the player owns in the permanent skill tree, plus the tree rules that read them:
    // what is open, what the next rank costs. Plain C# so the rules run without a scene.
    public sealed class SkillTreeProgress
    {
        private const string LogPrefix = "[Profile]";
        private static readonly SkillTreeNodeSO[] NoNodes = new SkillTreeNodeSO[0];

        private readonly SkillTreeSO _tree;
        private readonly SkillTreeNodeSO[] _nodes;
        private readonly SaveService _save;
        private readonly int[] _ranks;

        public SkillTreeProgress(SkillTreeSO tree, SaveService save)
        {
            _tree = tree;
            _save = save;
            _nodes = tree != null && tree.Nodes != null ? tree.Nodes : NoNodes;
            if (_nodes.Length == 0)
            {
                Debug.LogError($"{LogPrefix} the skill tree is missing or empty - the skill page would have nothing to sell.");
            }

            _ranks = new int[_nodes.Length];
            for (int i = 0; i < _nodes.Length; i++)
            {
                SkillTreeNodeSO node = _nodes[i];
                if (node == null)
                {
                    Debug.LogError($"{LogPrefix} skill tree slot {i} is empty.");
                    continue;
                }

                _ranks[i] = Mathf.Clamp(save.GetSkillRank(node.Id), 0, node.MaxRank);
            }
        }

        public int Count => _nodes.Length;

        public SkillTreeNodeSO NodeAt(int index) => _nodes[index];

        public int RankAt(int index) => _ranks[index];

        public bool IsMaxed(int index) => _ranks[index] >= _nodes[index].MaxRank;

        public int CostAt(int index) => _nodes[index].GetCost(_ranks[index]);

        public bool IsUnlocked(int index) => FirstMissingPrerequisite(index) == null;

        // The first parent still at rank zero, which is what the locked hint names.
        public SkillTreeNodeSO FirstMissingPrerequisite(int index)
        {
            SkillTreeNodeSO[] prerequisites = _nodes[index].Prerequisites;
            if (prerequisites == null)
            {
                return null;
            }

            for (int i = 0; i < prerequisites.Length; i++)
            {
                SkillTreeNodeSO parent = prerequisites[i];
                int parentIndex = _tree.IndexOf(parent);
                if (parentIndex < 0)
                {
                    Debug.LogError($"{LogPrefix} {_nodes[index].name} requires {parent.name}, which is not in the tree.");
                    return parent;
                }

                if (_ranks[parentIndex] <= 0)
                {
                    return parent;
                }
            }

            return null;
        }

        public void Advance(int index)
        {
            _ranks[index]++;
            _save.SetSkillRank(_nodes[index].Id, _ranks[index]);
        }
    }
}
