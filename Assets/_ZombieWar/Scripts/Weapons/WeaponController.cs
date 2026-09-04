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

        private int _currentIndex;
        private float _stateTimer;
        private float _reloadDuration;
        private float _pendingCooldownAfterSwitch;

        public event Action<Gun> OnGunChanged;
        public event Action<int, int> OnAmmoChanged;
        public event Action<float> OnReloadProgress;
        public event Action OnReloadStarted;
        public event Action OnReloadEnded;
        public event Action<Gun> OnShotFired;

        public Gun CurrentGun => _guns[_currentIndex];
        public WeaponState State { get; private set; }

        private void Awake()
        {
            bool missing = _guns == null || _guns.Length == 0 || _playerDefinition == null || _aim == null || _health == null
                           || _flow == null || _projectiles == null || _vfx == null || _audio == null || _impulseSource == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} WeaponController has an unassigned reference.", this);
                return;
            }

            for (int i = 0; i < _guns.Length; i++)
            {
                _guns[i].ResetAmmo();
                _guns[i].SetVisible(i == _currentIndex);
            }

            _aim.SetScanRange(CurrentGun.Definition.Range);
        }

        private void Start()
        {
            PublishGunState();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            Gun gun = CurrentGun;
            gun.TickRecoil(deltaTime);

            if (_flow.State != GameState.Playing || !_health.IsAlive)
            {
                return;
            }

            switch (State)
            {
                case WeaponState.Ready:
                    TryFire(gun);
                    break;
                case WeaponState.Cooldown:
                    TickCooldown(gun, deltaTime);
                    break;
                case WeaponState.Reloading:
                    TickReload(gun, deltaTime);
                    break;
                case WeaponState.Switching:
                    TickSwitch(gun, deltaTime);
                    break;
            }
        }

        public void RequestSwitch()
        {
            if (State == WeaponState.Switching || _guns.Length < 2 || _flow.State != GameState.Playing)
            {
                return;
            }

            float remainingCooldown = State == WeaponState.Cooldown ? _stateTimer : 0f;
            if (State == WeaponState.Reloading)
            {
                OnReloadEnded?.Invoke();
            }

            CurrentGun.SetVisible(false);
            _currentIndex = (_currentIndex + 1) % _guns.Length;
            Gun next = CurrentGun;
            next.SetVisible(true);
            _aim.SetScanRange(next.Definition.Range);

            State = WeaponState.Switching;
            _stateTimer = _playerDefinition.SwitchLockDuration;
            _pendingCooldownAfterSwitch = Mathf.Max(_playerDefinition.MinCooldownAfterSwitch, remainingCooldown);
            PublishGunState();
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

            SpawnPellets(definition, origin, forward);
            _vfx.Play(definition.MuzzleVfx, origin, Quaternion.LookRotation(forward));
            _audio.PlayWorld(definition.ShotClip, origin);
            _impulseSource.GenerateImpulseWithForce(definition.CameraImpulse);
            gun.Kick();
            gun.ConsumeRound();

            OnAmmoChanged?.Invoke(gun.Ammo, definition.MagazineSize);
            OnShotFired?.Invoke(gun);

            if (gun.IsEmpty)
            {
                BeginReload(gun);
                return;
            }

            State = WeaponState.Cooldown;
            _stateTimer = definition.FireInterval;
        }

        private void SpawnPellets(GunDefinitionSO definition, Vector3 origin, Vector3 forward)
        {
            int pellets = definition.PelletCount;
            float halfSpread = definition.SpreadAngle * 0.5f;
            for (int i = 0; i < pellets; i++)
            {
                float yaw = pellets == 1
                    ? UnityEngine.Random.Range(-halfSpread, halfSpread)
                    : Mathf.Lerp(-halfSpread, halfSpread, (i + 0.5f) / pellets) + UnityEngine.Random.Range(-halfSpread, halfSpread) / pellets;
                Vector3 direction = Quaternion.AngleAxis(yaw, Vector3.up) * forward;
                _projectiles.Spawn(origin, direction, definition);
            }
        }

        private void TickCooldown(Gun gun, float deltaTime)
        {
            _stateTimer -= deltaTime;
            if (_stateTimer > 0f)
            {
                return;
            }

            if (gun.IsEmpty)
            {
                BeginReload(gun);
                return;
            }

            State = WeaponState.Ready;
        }

        private void BeginReload(Gun gun)
        {
            State = WeaponState.Reloading;
            _reloadDuration = gun.Definition.ReloadDuration;
            _stateTimer = _reloadDuration;
            _audio.PlayWorld(gun.Definition.ReloadClip, gun.Muzzle.position);
            OnReloadStarted?.Invoke();
            OnReloadProgress?.Invoke(0f);
        }

        private void TickReload(Gun gun, float deltaTime)
        {
            _stateTimer -= deltaTime;
            OnReloadProgress?.Invoke(1f - Mathf.Clamp01(_stateTimer / _reloadDuration));
            if (_stateTimer > 0f)
            {
                return;
            }

            gun.ResetAmmo();
            State = WeaponState.Ready;
            OnAmmoChanged?.Invoke(gun.Ammo, gun.Definition.MagazineSize);
            OnReloadEnded?.Invoke();
        }

        private void TickSwitch(Gun gun, float deltaTime)
        {
            _stateTimer -= deltaTime;
            if (_stateTimer > 0f)
            {
                return;
            }

            if (gun.IsEmpty)
            {
                BeginReload(gun);
                return;
            }

            State = WeaponState.Cooldown;
            _stateTimer = _pendingCooldownAfterSwitch;
        }

        private void PublishGunState()
        {
            Gun gun = CurrentGun;
            OnGunChanged?.Invoke(gun);
            OnAmmoChanged?.Invoke(gun.Ammo, gun.Definition.MagazineSize);
        }
    }
}
