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
        // Drives the Animator WeaponType parameter on the upper-body layer.
        [SerializeField] private int _animatorWeaponType;

        [Header("Ballistics")]
        [SerializeField] private float _damage = 16f;
        [SerializeField] private float _fireInterval = 0.1333f;
        [SerializeField] private int _magazineSize = 30;
        [SerializeField] private float _reloadDuration = 1.25f;
        [SerializeField] private float _range = 16f;
        [SerializeField] private float _projectileSpeed = 32f;
        [SerializeField] private float _projectileRadius = 0.06f;
        [SerializeField] private int _pelletCount = 1;
        // Total cone angle in degrees; pellets are spread across it.
        [SerializeField] private float _spreadAngle = 1.5f;
        [SerializeField] private float _knockback = 0.6f;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _muzzleVfx;
        [SerializeField] private PooledVfx _fleshImpactVfx;
        [SerializeField] private PooledVfx _propImpactVfx;
        [SerializeField] private AudioClip _shotClip;
        [SerializeField] private AudioClip _reloadClip;
        [SerializeField] private float _cameraImpulse = 0.08f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public int AnimatorWeaponType => _animatorWeaponType;
        public float Damage => _damage;
        public float FireInterval => _fireInterval;
        public int MagazineSize => _magazineSize;
        public float ReloadDuration => _reloadDuration;
        public float Range => _range;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileRadius => _projectileRadius;
        public int PelletCount => _pelletCount;
        public float SpreadAngle => _spreadAngle;
        public float Knockback => _knockback;
        public PooledVfx MuzzleVfx => _muzzleVfx;
        public PooledVfx FleshImpactVfx => _fleshImpactVfx;
        public PooledVfx PropImpactVfx => _propImpactVfx;
        public AudioClip ShotClip => _shotClip;
        public AudioClip ReloadClip => _reloadClip;
        public float CameraImpulse => _cameraImpulse;
    }
}
