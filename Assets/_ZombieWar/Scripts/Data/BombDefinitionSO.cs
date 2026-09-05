using UnityEngine;
using ZombieWar.VFX;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Bomb Definition", fileName = "BombDefinition")]
    public sealed class BombDefinitionSO : ScriptableObject
    {
        [Header("Pool")]
        // Bombs that can be in the air at once. Pacing lives in the Auto Bomb skill, so this
        // only has to cover the largest volley twice over.
        [SerializeField] private int _poolSize = 8;

        [Header("Throw")]
        [SerializeField] private float _throwRange = 6f;
        [SerializeField] private float _flightTime = 0.55f;
        [SerializeField] private float _fuseDuration = 1.2f;
        [SerializeField] private float _telegraphLead = 0.45f;
        // Extra bombs in a volley land up to this far from the first, so they never stack on one spot.
        [SerializeField] private float _volleyScatter = 1.6f;

        [Header("Blast")]
        [SerializeField] private float _blastRadius = 4.5f;
        [SerializeField] private float _damageAtCenter = 160f;
        [SerializeField] private float _damageAtEdge = 60f;
        [SerializeField] private float _knockbackAtCenter = 9f;
        [SerializeField] private float _knockbackAtEdge = 2f;
        [SerializeField] private float _propForceMultiplier = 1f;
        [SerializeField] private float _explosionUpwardsModifier = 0.5f;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _explosionVfx;
        [SerializeField] private AudioClip _explosionClip;
        [SerializeField] private AudioClip _throwClip;
        [SerializeField] private float _cameraImpulse = 0.55f;

        public int PoolSize => _poolSize;
        public float ThrowRange => _throwRange;
        public float FlightTime => _flightTime;
        public float FuseDuration => _fuseDuration;
        public float TelegraphLead => _telegraphLead;
        public float VolleyScatter => _volleyScatter;
        public float BlastRadius => _blastRadius;
        public float DamageAtCenter => _damageAtCenter;
        public float DamageAtEdge => _damageAtEdge;
        public float KnockbackAtCenter => _knockbackAtCenter;
        public float KnockbackAtEdge => _knockbackAtEdge;
        public float PropForceMultiplier => _propForceMultiplier;
        public float ExplosionUpwardsModifier => _explosionUpwardsModifier;
        public PooledVfx ExplosionVfx => _explosionVfx;
        public AudioClip ExplosionClip => _explosionClip;
        public AudioClip ThrowClip => _throwClip;
        public float CameraImpulse => _cameraImpulse;
    }
}
