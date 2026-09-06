using UnityEngine;
using ZombieWar.Roguelike;
using ZombieWar.VFX;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/XP Orb", fileName = "XpOrb")]
    public sealed class XpOrbDefinitionSO : ScriptableObject
    {
        [Header("Pool")]
        [SerializeField] private XpOrb _prefab;
        // Worst case is one orb per zombie alive plus the ones still flying in.
        [SerializeField] private int _poolSize = 96;

        [Header("Drop")]
        // How far from the corpse the orb comes to rest, so a pile of kills reads as a spray.
        [SerializeField] private float _scatterRadius = 0.8f;
        [SerializeField] private float _popHeight = 0.7f;
        [SerializeField] private float _popDuration = 0.4f;
        [SerializeField] private float _hoverHeight = 0.5f;
        [SerializeField] private float _bobAmplitude = 0.08f;
        [SerializeField] private float _bobFrequency = 1.6f;
        [SerializeField] private float _spinDegreesPerSecond = 120f;

        [Header("Pickup")]
        // Inside this radius the orb starts flying to the player; outside it just waits.
        [SerializeField] private float _magnetRadius = 2.5f;
        // The orb counts as collected once it is this close to the player.
        [SerializeField] private float _collectRadius = 0.45f;
        [SerializeField] private float _flySpeed = 4f;
        [SerializeField] private float _flyAcceleration = 22f;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _collectVfx;
        [SerializeField] private AudioClip _collectClip;

        public XpOrb Prefab => _prefab;
        public int PoolSize => _poolSize;
        public float ScatterRadius => _scatterRadius;
        public float PopHeight => _popHeight;
        public float PopDuration => _popDuration;
        public float HoverHeight => _hoverHeight;
        public float BobAmplitude => _bobAmplitude;
        public float BobFrequency => _bobFrequency;
        public float SpinDegreesPerSecond => _spinDegreesPerSecond;
        public float MagnetRadius => _magnetRadius;
        public float CollectRadius => _collectRadius;
        public float FlySpeed => _flySpeed;
        public float FlyAcceleration => _flyAcceleration;
        public PooledVfx CollectVfx => _collectVfx;
        public AudioClip CollectClip => _collectClip;
    }
}
