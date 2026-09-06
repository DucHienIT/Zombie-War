using System;
using Cinemachine;
using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Player;
using ZombieWar.VFX;

namespace ZombieWar.Weapons
{
    public sealed class WeaponController : MonoBehaviour
    {
        private const string LogPrefix = "[Weapon]";

        [SerializeField] private Gun[] _guns;
        [SerializeField] private PlayerDefinitionSO _playerDefinition;
        [SerializeField] private PlayerAim _aim;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ProjectileManager _projectiles;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private AudioService _audio;
        [SerializeField] private CinemachineImpulseSource _impulseSource;
        [SerializeField] private ProfileService _profile;
        [SerializeField] private PlayerStatSheet _stats;

        private Transform _transform;
        private int _currentIndex;
        private float _stateTimer;
        private float _pendingCooldownAfterSwitch;

        public event Action<Gun> OnGunChanged;
        public event Action<Gun> OnShotFired;

        public Gun CurrentGun => _guns[_currentIndex];
        public WeaponState State { get; private set; }

        private void Awake()
        {
            _transform = transform;
            bool missing = _guns == null || _guns.Length == 0 || _playerDefinition == null || _aim == null || _health == null
                           || _flow == null || _projectiles == null || _vfx == null || _audio == null || _impulseSource == null
                           || _profile == null || _stats == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} WeaponController has an unassigned reference.", this);
                return;
            }

            ApplyUpgrades();
            for (int i = 0; i < _guns.Length; i++)
            {
                _guns[i].SetVisible(i == _currentIndex);
            }

            _aim.SetScanRange(CurrentGun.Definition.Range);
        }

        private void OnEnable()
        {
            _flow.OnRunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            _flow.OnRunStarted -= HandleRunStarted;
        }

        private void Start()
        {
            OnGunChanged?.Invoke(CurrentGun);
        }

        // Upgrades bought in the menu land here, so a run always starts with the saved levels.
        private void HandleRunStarted(LevelDefinitionSO level)
        {
            ApplyUpgrades();
            OnGunChanged?.Invoke(CurrentGun);
        }

        private void ApplyUpgrades()
        {
            for (int i = 0; i < _guns.Length; i++)
            {
                Gun gun = _guns[i];
                gun.ApplyUpgrade(_profile.GetGunLevel(gun.Definition));
            }
        }

        // Everything runs after the animator, in this order: the barrel is re-aimed (the run cycle
        // swings the hand up to 13 degrees off the body facing), the recoil pose is applied, and
        // only then may a shot fire - so bullets, tracer and flash spawn on the muzzle pose that
        // this very frame renders, not on the one the previous frame left behind.
        private void LateUpdate()
        {
            if (!_health.IsAlive)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Gun gun = CurrentGun;
            AlignBarrel();
            gun.TickMotion(deltaTime);

            if (_flow.State != GameState.Playing)
            {
                return;
            }

            switch (State)
            {
                case WeaponState.Ready:
                    TryFire(gun);
                    break;
                case WeaponState.Cooldown:
                    TickCooldown(deltaTime);
                    break;
                case WeaponState.Switching:
                    TickSwitch(deltaTime);
                    break;
            }
        }

        // Also called from the hand IK pass, which runs before LateUpdate: the hands must reach for
        // the barrel pose this frame renders. Same inputs both times, so the second call is a no-op.
        public void AlignBarrel()
        {
            if (!_health.IsAlive)
            {
                return;
            }

            Gun gun = CurrentGun;
            if (_aim.HasTarget)
            {
                gun.AlignBarrelAt(_transform.forward, _aim.TargetPosition);
            }
            else
            {
                gun.AlignBarrel(_transform.forward);
            }
        }

        public void RequestSwitch()
        {
            if (State == WeaponState.Switching || _guns.Length < 2 || _flow.State != GameState.Playing)
            {
                return;
            }

            float remainingCooldown = State == WeaponState.Cooldown ? _stateTimer : 0f;
            CurrentGun.SetVisible(false);
            _currentIndex = (_currentIndex + 1) % _guns.Length;
            Gun next = CurrentGun;
            next.SetVisible(true);
            _aim.SetScanRange(next.Definition.Range);

            State = WeaponState.Switching;
            _stateTimer = _playerDefinition.SwitchLockDuration;
            _pendingCooldownAfterSwitch = Mathf.Max(_playerDefinition.MinCooldownAfterSwitch, remainingCooldown);
            OnGunChanged?.Invoke(next);
        }

        private void TryFire(Gun gun)
        {
            if (!_aim.HasTarget || _aim.AimErrorDegrees > _playerDefinition.AimToleranceDegrees)
            {
                return;
            }

            Fire(gun);
        }

        private void Fire(Gun gun)
        {
            State = WeaponState.Firing;
            GunDefinitionSO definition = gun.Definition;
            Transform muzzle = gun.Muzzle;
            Vector3 origin = muzzle.position;
            Vector3 forward = muzzle.forward;
            forward.y = 0f;
            forward.Normalize();

            // The menu upgrade is baked into gun.Stats; the run's passives scale it from there.
            var shot = new ShotStats(
                gun.Stats.Damage * _stats.Multiplier(StatId.WeaponDamage),
                definition.Knockback * _stats.Multiplier(StatId.Knockback),
                definition.Pierce + Mathf.RoundToInt(_stats.Additive(StatId.ProjectilePierce)));
            SpawnPellets(definition, origin, forward, shot);
            // Attached, not dropped at a world position: the flash rides the muzzle while the soldier runs.
            _vfx.Play(definition.MuzzleVfx, muzzle);
            _audio.PlayWorld(definition.ShotClip, origin);
            _impulseSource.GenerateImpulseWithForce(definition.CameraImpulse);
            gun.Kick();
            OnShotFired?.Invoke(gun);

            State = WeaponState.Cooldown;
            _stateTimer = gun.Stats.FireInterval / _stats.Multiplier(StatId.FireRate);
        }

        private void SpawnPellets(GunDefinitionSO definition, Vector3 origin, Vector3 forward, in ShotStats shot)
        {
            int pellets = definition.PelletCount;
            float halfSpread = definition.SpreadAngle * 0.5f;
            for (int i = 0; i < pellets; i++)
            {
                float yaw = pellets == 1
                    ? UnityEngine.Random.Range(-halfSpread, halfSpread)
                    : Mathf.Lerp(-halfSpread, halfSpread, (i + 0.5f) / pellets) + UnityEngine.Random.Range(-halfSpread, halfSpread) / pellets;
                Vector3 direction = Quaternion.AngleAxis(yaw, Vector3.up) * forward;
                _projectiles.Spawn(origin, direction, definition, shot);
            }
        }

        private void TickCooldown(float deltaTime)
        {
            _stateTimer -= deltaTime;
            if (_stateTimer > 0f)
            {
                return;
            }

            State = WeaponState.Ready;
        }

        private void TickSwitch(float deltaTime)
        {
            _stateTimer -= deltaTime;
            if (_stateTimer > 0f)
            {
                return;
            }

            State = WeaponState.Cooldown;
            _stateTimer = _pendingCooldownAfterSwitch;
        }
    }
}
