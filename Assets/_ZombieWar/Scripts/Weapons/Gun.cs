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

        private Vector3 _recoilRestPosition;
        private float _recoilOffset;

        public GunDefinitionSO Definition => _definition;
        public Transform Muzzle => _muzzle;
        // Effective numbers at the applied upgrade level; WeaponController applies them before use.
        public GunStats Stats { get; private set; }
        public int UpgradeLevel { get; private set; }
        public int Ammo { get; private set; }
        public bool IsEmpty => Ammo <= 0;

        private void Awake()
        {
            if (_definition == null || _muzzle == null || _recoilPivot == null)
            {
                Debug.LogError($"{LogPrefix} Gun {name} has an unassigned reference.", this);
                return;
            }

            _recoilRestPosition = _recoilPivot.localPosition;
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
        }

        public void TickRecoil(float deltaTime)
        {
            if (_recoilOffset <= 0f)
            {
                return;
            }

            float returnSpeed = _recoilDistance / _recoilReturnDuration;
            _recoilOffset = Mathf.MoveTowards(_recoilOffset, 0f, returnSpeed * deltaTime);
            _recoilPivot.localPosition = _recoilRestPosition + Vector3.back * _recoilOffset;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
