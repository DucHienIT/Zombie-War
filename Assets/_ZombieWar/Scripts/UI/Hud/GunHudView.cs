using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class GunHudView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _switchButton;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _ammoText;
        [SerializeField] private TMP_Text _nameText;
        // Radial overlay that sweeps while the magazine refills.
        [SerializeField] private Image _reloadFill;
        [SerializeField] private GameObject _reloadBadge;

        [Header("Colors")]
        [SerializeField] private Color _ammoColor;
        [SerializeField] private Color _emptyAmmoColor;

        private void Awake()
        {
            bool missing = _switchButton == null || _icon == null || _ammoText == null || _nameText == null
                           || _reloadFill == null || _reloadBadge == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} GunHudView has an unassigned reference.", this);
            }
        }

        public void Init(UnityAction onSwitchRequested) => _switchButton.onClick.AddListener(onSwitchRequested);

        public void SetGun(Sprite icon, string displayName)
        {
            _icon.sprite = icon;
            _nameText.text = displayName;
            SetReloadProgress(0f);
        }

        public void SetAmmo(int ammo, int magazine)
        {
            _ammoText.SetText("{0}/{1}", ammo, magazine);
            _ammoText.color = ammo > 0 ? _ammoColor : _emptyAmmoColor;
        }

        public void SetReloadProgress(float progress)
        {
            _reloadFill.fillAmount = progress;
            _reloadBadge.SetActive(progress > 0f);
        }
    }
}
