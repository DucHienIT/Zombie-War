using DG.Tweening;
using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Weapons
{
    public sealed class Gun : MonoBehaviour
    {
        private const string LogPrefix = "[Weapon]";

        [SerializeField] private GunDefinitionSO _definition;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _recoilPivot;

        [Header("Recoil")]
        [SerializeField] private float _recoilDistance = 0.06f;
        [SerializeField] private float _recoilReturnDuration = 0.08f;
        [SerializeField] private float _recoilKickDegrees = 5f;

        [Header("Reload")]
        // No reload clip ships with the soldier: the gun dips and tilts over the reload instead.
        [SerializeField] private float _reloadTiltDegrees = 35f;
        [SerializeField] private float _reloadDipDistance = 0.05f;

        [Header("Switch")]
        [SerializeField] private float _switchPopScale = 0.7f;
        [SerializeField] private float _switchPopDuration = 0.2f;
        [SerializeField] private Ease _switchPopEase = Ease.OutBack;

        [Header("Aim")]
        // How far the barrel may swing away from the body facing to stay on the target.
        [SerializeField] private float _aimYawLimitDegrees = 25f;

        private Transform _transform;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _restScale;
        private bool _restCached;
        private float _recoilOffset;
        private float _reloadDuration;
        private float _reloadRemaining;
        private bool _pivotDirty;

        public GunDefinitionSO Definition => _definition;
        public Transform Muzzle => _muzzle;
        // Effective numbers at the applied upgrade level; WeaponController applies them before use.
        public GunStats Stats { get; private set; }
        public int UpgradeLevel { get; private set; }
        public int Ammo { get; private set; }
        public bool IsEmpty => Ammo <= 0;

        private void Awake()
        {
            _transform = transform;
            if (_definition == null || _muzzle == null || _recoilPivot == null)
            {
                Debug.LogError($"{LogPrefix} Gun {name} has an unassigned reference.", this);
                return;
            }

            _restPosition = _recoilPivot.localPosition;
            _restRotation = _recoilPivot.localRotation;
            _restScale = _recoilPivot.localScale;
            _restCached = true;
        }

        public void ApplyUpgrade(int upgradeLevel)
        {
            UpgradeLevel = upgradeLevel;
            Stats = _definition.GetStats(upgradeLevel);
            Ammo = Stats.MagazineSize;
        }

        public void ResetAmmo()
        {
            Ammo = Stats.MagazineSize;
        }

        public void ConsumeRound()
        {
            Ammo = Mathf.Max(0, Ammo - 1);
        }

        public void Kick()
        {
            _recoilOffset = _recoilDistance;
            _pivotDirty = true;
        }

        public void BeginReloadMotion(float duration)
        {
            _reloadDuration = duration;
            _reloadRemaining = duration;
            _pivotDirty = true;
        }

        public void TickMotion(float deltaTime)
        {
            if (!_pivotDirty)
            {
                return;
            }

            if (_recoilOffset > 0f)
            {
                float returnSpeed = _recoilDistance / _recoilReturnDuration;
                _recoilOffset = Mathf.MoveTowards(_recoilOffset, 0f, returnSpeed * deltaTime);
            }

            float reload = 0f;
            if (_reloadRemaining > 0f)
            {
                _reloadRemaining -= deltaTime;
                float progress = 1f - Mathf.Clamp01(_reloadRemaining / _reloadDuration);
                // One sine arc: the gun swings out and settles back exactly as the magazine lands.
                reload = Mathf.Sin(progress * Mathf.PI);
            }

            float recoil = _recoilDistance > 0f ? _recoilOffset / _recoilDistance : 0f;
            Vector3 position = _restPosition + Vector3.back * _recoilOffset + Vector3.down * (_reloadDipDistance * reload);
            // Negative pitch raises the muzzle, so the kick and the reload tilt pull in opposite directions.
            Quaternion rotation = _restRotation * Quaternion.Euler(_reloadTiltDegrees * reload - _recoilKickDegrees * recoil, 0f, 0f);
            _recoilPivot.SetLocalPositionAndRotation(position, rotation);
            _pivotDirty = _recoilOffset > 0f || _reloadRemaining > 0f;
        }

        // Levels the barrel along the body facing, whatever pose the hand is in. Called after the
        // animator has run, so the locomotion sway of the arm never bends where bullets go.
        public void AlignBarrel(Vector3 bodyForward)
        {
            SetBarrelDirection(bodyForward);
        }

        // Same, but swung towards the target within the yaw limit so a shot inside the aim
        // tolerance leaves the muzzle pointing exactly at what it hits.
        public void AlignBarrelAt(Vector3 bodyForward, Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - _transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0f)
            {
                SetBarrelDirection(bodyForward);
                return;
            }

            float yaw = Vector3.SignedAngle(bodyForward, toTarget, Vector3.up);
            yaw = Mathf.Clamp(yaw, -_aimYawLimitDegrees, _aimYawLimitDegrees);
            SetBarrelDirection(Quaternion.AngleAxis(yaw, Vector3.up) * bodyForward);
        }

        private void SetBarrelDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            _transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (!visible)
            {
                ResetMotion();
                return;
            }

            // Shown at load before this component woke up: the rest pose is unknown, and the first
            // gun has nothing to pop in from anyway.
            if (!_restCached)
            {
                return;
            }

            _recoilPivot.localScale = _restScale * _switchPopScale;
            _recoilPivot.DOScale(_restScale, _switchPopDuration).SetEase(_switchPopEase).SetLink(gameObject);
        }

        private void ResetMotion()
        {
            _recoilOffset = 0f;
            _reloadRemaining = 0f;
            _pivotDirty = false;
            // Hidden before its first activation: nothing has moved yet, and no rest pose exists to restore.
            if (!_restCached)
            {
                return;
            }

            _recoilPivot.DOKill();
            _recoilPivot.SetLocalPositionAndRotation(_restPosition, _restRotation);
            _recoilPivot.localScale = _restScale;
        }
    }
}
