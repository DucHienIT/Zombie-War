using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Roguelike
{
    // Fills the cards one level-up offers: distinct skills, weighted, never one the player
    // has already stacked to its cap.
    public sealed class SkillDraft
    {
        private readonly SkillDefinitionSO[] _pool;
        private readonly List<int> _candidates;

        public SkillDraft(SkillDefinitionSO[] pool)
        {
            _pool = pool;
            _candidates = new List<int>(pool.Length);
        }

        // Writes into the caller's buffer and returns how many slots it filled, which can be
        // fewer than asked once most of the pool is maxed out.
        public int Roll(Dictionary<SkillDefinitionSO, int> stacks, SkillDefinitionSO[] offers, int wanted)
        {
            _candidates.Clear();
            for (int i = 0; i < _pool.Length; i++)
            {
                SkillDefinitionSO skill = _pool[i];
                if (skill == null)
                {
                    continue;
                }

                stacks.TryGetValue(skill, out int owned);
                if (owned < skill.MaxStacks)
                {
                    _candidates.Add(i);
                }
            }

            int count = Mathf.Min(wanted, Mathf.Min(offers.Length, _candidates.Count));
            for (int slot = 0; slot < count; slot++)
            {
                int picked = PickWeighted();
                offers[slot] = _pool[_candidates[picked]];
                _candidates.RemoveAt(picked);
            }

            return count;
        }

        private int PickWeighted()
        {
            float total = 0f;
            for (int i = 0; i < _candidates.Count; i++)
            {
                total += _pool[_candidates[i]].DraftWeight;
            }

            float roll = Random.value * total;
            for (int i = 0; i < _candidates.Count; i++)
            {
                roll -= _pool[_candidates[i]].DraftWeight;
                if (roll <= 0f)
                {
                    return i;
                }
            }

            return _candidates.Count - 1;
        }
    }
}
