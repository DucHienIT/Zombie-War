using System;
using UnityEngine;

namespace ZombieWar.Data
{
    [Serializable]
    public struct StatModifier
    {
        [SerializeField] private StatId _stat;
        [SerializeField] private StatModifierKind _kind;
        [SerializeField] private float _valuePerStack;

        public StatId Stat => _stat;
        public StatModifierKind Kind => _kind;
        public float ValuePerStack => _valuePerStack;
    }
}
