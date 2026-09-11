using UnityEngine;
using ZombieWar.VFX;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Gun Definition", fileName = "GunDefinition")]
    public sealed class GunDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        // One line shown on the weapon detail sheet.
        [SerializeField] private string _description;
        // Drives the Animator WeaponType parameter on the upper-body layer.
        [SerializeField] private int _animatorWeaponType;

        [Header("Ballistics")]
        [SerializeField] private float _damage = 16f;
        [SerializeField] private float _fireInterval = 0.1333f;
        [SerializeField] private float _range = 16f;
        [SerializeField] private float _projectileSpeed = 32f;
        [SerializeField] private float _projectileRadius = 0.06f;
        [SerializeField] private int _pelletCount = 1;
        // Total cone angle in degrees; pellets are spread across it.
        [SerializeField] private float _spreadAngle = 1.5f;
        [SerializeField] private float _knockback = 0.6f;
        // Zombies one bullet passes through before it is spent; the ProjectilePierce passive adds to it.
        [SerializeField] private int _pierce;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _muzzleVfx;
        [SerializeField] private PooledVfx _fleshImpactVfx;
        [SerializeField] private PooledVfx _propImpactVfx;
        [SerializeField] private AudioClip _shotClip;
        // Automatic guns fire several times a second, so their shot sits well under the zombie voices.
        [SerializeField, Range(0f, 1f)] private float _shotVolume = 0.5f;
        [SerializeField] private float _cameraImpulse = 0.08f;

        [Header("Upgrade")]
        [SerializeField] private bool _unlockedByDefault = true;
        [SerializeField] private int _maxUpgradeLevel = 5;
        // Fractions of the base value gained (damage) or shaved (fire interval) per level.
        [SerializeField] private float _damageGainPerLevel = 0.12f;
        [SerializeField] private float _fireIntervalCutPerLevel = 0.03f;
        [SerializeField] private int _baseUpgradeCost = 150;
        [SerializeField] private float _upgradeCostGrowth = 1.6f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public int AnimatorWeaponType => _animatorWeaponType;
        public bool IsSpread => _pelletCount > 1;
        public float Damage => _damage;
        public float FireInterval => _fireInterval;
        public float Range => _range;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileRadius => _projectileRadius;
        public int PelletCount => _pelletCount;
        public float SpreadAngle => _spreadAngle;
        public float Knockback => _knockback;
        public int Pierce => _pierce;
        public PooledVfx MuzzleVfx => _muzzleVfx;
        public PooledVfx FleshImpactVfx => _fleshImpactVfx;
        public PooledVfx PropImpactVfx => _propImpactVfx;
        public AudioClip ShotClip => _shotClip;
        public float ShotVolume => _shotVolume;
        public float CameraImpulse => _cameraImpulse;
        public bool UnlockedByDefault => _unlockedByDefault;
        public int MaxUpgradeLevel => _maxUpgradeLevel;

        public GunStats GetStats(int upgradeLevel)
        {
            int level = Mathf.Clamp(upgradeLevel, 0, _maxUpgradeLevel);
            float damage = _damage * (1f + _damageGainPerLevel * level);
            float fireInterval = _fireInterval * (1f - _fireIntervalCutPerLevel * level);
            return new GunStats(damage, fireInterval);
        }

        // Cost of the step from currentLevel to currentLevel + 1.
        public int GetUpgradeCost(int currentLevel)
        {
            return Mathf.RoundToInt(_baseUpgradeCost * Mathf.Pow(_upgradeCostGrowth, Mathf.Max(0, currentLevel)));
        }
    }
}
