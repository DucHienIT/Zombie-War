using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Audio;
using ZombieWar.Weapons;

namespace ZombieWar.UI
{
    public sealed class GunHudView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private WeaponController _weapons;
        [SerializeField] private AudioService _audio;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _ammoText;
        [SerializeField] private Image _reloadFill;
        [SerializeField] private Button _switchButton;
        [SerializeField] private AudioClip _switchClip;

        private void Awake()
        {
            bool missing = _weapons == null || _audio == null || _icon == null || _ammoText == null || _reloadFill == null || _switchButton == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} GunHudView has an unassigned reference.", this);
                return;
            }

            _switchButton.onClick.AddListener(HandleSwitchClicked);
        }

        private void OnEnable()
        {
            _weapons.OnGunChanged += HandleGunChanged;
            _weapons.OnAmmoChanged += HandleAmmoChanged;
            _weapons.OnReloadStarted += HandleReloadStarted;
            _weapons.OnReloadProgress += HandleReloadProgress;
            _weapons.OnReloadEnded += HandleReloadEnded;
        }

        private void OnDisable()
        {
            _weapons.OnGunChanged -= HandleGunChanged;
            _weapons.OnAmmoChanged -= HandleAmmoChanged;
            _weapons.OnReloadStarted -= HandleReloadStarted;
            _weapons.OnReloadProgress -= HandleReloadProgress;
            _weapons.OnReloadEnded -= HandleReloadEnded;
        }

        private void HandleSwitchClicked()
        {
            _audio.PlayUi(_switchClip);
            _weapons.RequestSwitch();
        }

        private void HandleGunChanged(Gun gun)
        {
            _icon.sprite = gun.Definition.Icon;
            _reloadFill.fillAmount = 0f;
        }

        private void HandleAmmoChanged(int ammo, int magazine)
        {
            _ammoText.SetText("{0}/{1}", ammo, magazine);
        }

        private void HandleReloadStarted() => _reloadFill.fillAmount = 0f;
        private void HandleReloadProgress(float progress) => _reloadFill.fillAmount = progress;
        private void HandleReloadEnded() => _reloadFill.fillAmount = 0f;
    }
}
