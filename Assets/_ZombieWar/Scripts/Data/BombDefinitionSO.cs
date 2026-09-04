using UnityEngine;
using ZombieWar.VFX;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Bomb Definition", fileName = "BombDefinition")]
    public sealed class BombDefinitionSO : ScriptableObject
    {
        [Header("Supply")]
        [SerializeField] private int _chargesPerLevel = 3;
        [SerializeField] private float _cooldown = 2f;

        [Header("Throw")]
        [SerializeField] private float _throwRange = 6f;
        [SerializeField] private float _flightTime = 0.55f;
        [SerializeField] private float _fuseDuration = 1.2f;
        [SerializeField] private float _telegraphLead = 0.45f;

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

        public int ChargesPerLevel => _chargesPerLevel;
        public float Cooldown => _cooldown;
        public float ThrowRange => _throwRange;
        public float FlightTime => _flightTime;
        public float FuseDuration => _fuseDuration;
        public float TelegraphLead => _telegraphLead;
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
