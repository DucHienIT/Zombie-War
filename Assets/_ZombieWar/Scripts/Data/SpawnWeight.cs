using System;
using UnityEngine;

namespace ZombieWar.Data
{
    [Serializable]
    public struct SpawnWeight
    {
        [SerializeField] private ZombieDefinitionSO _definition;
        [SerializeField] private float _weight;

        public ZombieDefinitionSO Definition => _definition;
        public float Weight => _weight;
    }
}
