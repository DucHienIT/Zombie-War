using System;
using UnityEngine;

namespace ZombieWar.Data
{
    [Serializable]
    public struct ScriptedSpawn
    {
        [SerializeField] private float _time;
        [SerializeField] private ZombieDefinitionSO _definition;
        [SerializeField] private int _count;

        public float Time => _time;
        public ZombieDefinitionSO Definition => _definition;
        public int Count => _count;
    }
}
