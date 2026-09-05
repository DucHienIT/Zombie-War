using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Player
{
    // The live value of every stat a passive skill can move. Combat reads this instead of
    // reaching into the progression system, so a gun never learns what a battle level is.
    public sealed class PlayerStatSheet : MonoBehaviour
    {
        private const int PermanentCapacity = 16;
        private static readonly int StatCount = Enum.GetValues(typeof(StatId)).Length;

        private struct PermanentEntry
        {
            public StatModifier[] Modifiers;
            public int Stacks;
        }

        // Filled at construction rather than in Awake: consumers sit on the same object and
        // a multiplier read before Awake would otherwise come back as zero.
        private readonly float[] _multipliers = CreateNeutralMultipliers();
        private readonly float[] _additives = new float[StatCount];
        // Bought outside the run; re-applied as the floor of every rebuild.
        private readonly List<PermanentEntry> _permanent = new List<PermanentEntry>(PermanentCapacity);

        public event Action OnChanged;

        public float Multiplier(StatId stat) => _multipliers[(int)stat];

        public float Additive(StatId stat) => _additives[(int)stat];

        public void ClearPermanent() => _permanent.Clear();

        public void AddPermanent(StatModifier[] modifiers, int stacks)
        {
            if (modifiers == null || stacks <= 0)
            {
                return;
            }

            _permanent.Add(new PermanentEntry { Modifiers = modifiers, Stacks = stacks });
        }

        public void BeginRebuild()
        {
            for (int i = 0; i < StatCount; i++)
            {
                _multipliers[i] = 1f;
                _additives[i] = 0f;
            }

            for (int i = 0; i < _permanent.Count; i++)
            {
                Apply(_permanent[i].Modifiers, _permanent[i].Stacks);
            }
        }

        public void Apply(StatModifier[] modifiers, int stacks)
        {
            if (modifiers == null || stacks <= 0)
            {
                return;
            }

            for (int i = 0; i < modifiers.Length; i++)
            {
                StatModifier modifier = modifiers[i];
                int index = (int)modifier.Stat;
                if (modifier.Kind == StatModifierKind.Additive)
                {
                    _additives[index] += modifier.ValuePerStack * stacks;
                    continue;
                }

                _multipliers[index] *= Mathf.Pow(modifier.ValuePerStack, stacks);
            }
        }

        // Raised once the whole sheet is settled, so a listener never reads a half-applied build.
        public void EndRebuild() => OnChanged?.Invoke();

        private static float[] CreateNeutralMultipliers()
        {
            var values = new float[StatCount];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = 1f;
            }

            return values;
        }
    }
}
