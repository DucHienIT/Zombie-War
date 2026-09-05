using UnityEngine;
using ZombieWar.Level;

namespace ZombieWar.Data
{
    // The half of the Molotov ability that never changes with stacks: how the bottle flies and
    // what a fire patch is made of. The per-level numbers live on MolotovSkillSO.
    [CreateAssetMenu(menuName = "Zombie War/Molotov Definition", fileName = "MolotovDefinition")]
    public sealed class MolotovDefinitionSO : ScriptableObject
    {
        [Header("Pool")]
        [SerializeField] private int _bottlePoolSize = 4;
        // Fires outlive the throw cooldown, so this covers the longest burn a maxed skill can stack.
        [SerializeField] private int _firePoolSize = 6;

        [Header("Throw")]
        [SerializeField] private float _throwRange = 6f;
        [SerializeField] private float _flightTime = 0.6f;

        [Header("Fire")]
        [SerializeField] private FireZone _firePrefab;

        [Header("Feedback")]
        [SerializeField] private AudioClip _throwClip;
        [SerializeField] private AudioClip _igniteClip;

        public int BottlePoolSize => _bottlePoolSize;
        public int FirePoolSize => _firePoolSize;
        public float ThrowRange => _throwRange;
        public float FlightTime => _flightTime;
        public FireZone FirePrefab => _firePrefab;
        public AudioClip ThrowClip => _throwClip;
        public AudioClip IgniteClip => _igniteClip;
    }
}
