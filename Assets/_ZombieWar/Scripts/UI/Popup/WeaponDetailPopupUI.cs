using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // Detail sheet for one gun: description, six-cell stat grid with upgrade preview,
    // cost and the upgrade button. Stays open across an upgrade and redraws with new data.
    public sealed class WeaponDetailPopupUI : PopupBase
    {
        private const string LogPrefix = "[Popup]";

        [Header("Header")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Stats")]
        [SerializeField] private WeaponStatCellView _attackCell;
        [SerializeField] private WeaponStatCellView _rateCell;
        [SerializeField] private WeaponStatCellView _magazineCell;
        [SerializeField] private WeaponStatCellView _reloadCell;
        [SerializeField] private WeaponStatCellView _shotsCell;
        [SerializeField] private WeaponStatCellView _typeCell;

        [Header("Upgrade")]
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private GameObject _costGroup;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private Color _affordableCostColor = Color.white;
        [SerializeField] private Color _unaffordableCostColor = Color.red;

        [Header("Formats")]
        [SerializeField] private string _damageFormat = "{0:1}";
        [SerializeField] private string _fireRateFormat = "{0:1}/s";
        [SerializeField] private string _magazineFormat = "{0:0}";
        [SerializeField] private string _reloadFormat = "{0:2}s";
        [SerializeField] private string _singleShotLabel = "SINGLE";
        [SerializeField] private string _spreadShotLabel = "SPREAD";
        [SerializeField] private string _lockedHint = "LOCKED";
        [SerializeField] private string _maxLevelHint = "MAX LEVEL REACHED";

        private WeaponEntryData _entry;
        private Action _onUpgrade;

        public bool IsOpen { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            bool missing = _closeButton == null || _icon == null || _nameText == null || _levelText == null || _descriptionText == null
                           || _attackCell == null || _rateCell == null || _magazineCell == null || _reloadCell == null
                           || _shotsCell == null || _typeCell == null || _upgradeButton == null || _costGroup == null
                           || _costText == null || _hintText == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} WeaponDetailPopupUI has an unassigned reference.", this);
                return;
            }

            _closeButton.onClick.AddListener(Close);
            _upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        }

        public void Setup(in WeaponEntryData entry, Action onUpgrade)
        {
            _entry = entry;
            _onUpgrade = onUpgrade;
            if (IsOpen)
            {
                Draw();
            }
        }

        // Called after an upgrade while the sheet is still up.
        public void Refresh(in WeaponEntryData entry)
        {
            _entry = entry;
            if (IsOpen)
            {
                Draw();
            }
        }

        protected override void OnShowing()
        {
            IsOpen = true;
            Draw();
        }

        protected override void OnClosed()
        {
            IsOpen = false;
        }

        private void Draw()
        {
            _icon.sprite = _entry.Icon;
            _nameText.text = _entry.DisplayName;
            _descriptionText.text = _entry.Description;
            if (_entry.IsMaxLevel)
            {
                _levelText.text = "MAX LEVEL";
            }
            else
            {
                _levelText.SetText("LEVEL {0} / {1}", _entry.Level, _entry.MaxLevel);
            }

            bool showNext = _entry.Unlocked && !_entry.IsMaxLevel;
            _attackCell.Set(_damageFormat, _entry.Current.Damage, _entry.Next.Damage, showNext);
            _rateCell.Set(_fireRateFormat, _entry.Current.ShotsPerSecond, _entry.Next.ShotsPerSecond, showNext);
            _magazineCell.Set(_magazineFormat, _entry.Current.Magazine, _entry.Next.Magazine, showNext);
            _reloadCell.Set(_reloadFormat, _entry.Current.Reload, _entry.Next.Reload, showNext);
            _shotsCell.SetCount(_entry.Pellets);
            _typeCell.SetLabel(_entry.IsSpread ? _spreadShotLabel : _singleShotLabel);

            _costGroup.SetActive(showNext);
            _upgradeButton.interactable = showNext && _entry.CanAfford;
            _costText.SetText("{0}", _entry.UpgradeCost);
            _costText.color = _entry.CanAfford ? _affordableCostColor : _unaffordableCostColor;

            if (!_entry.Unlocked)
            {
                _hintText.text = _lockedHint;
            }
            else if (_entry.IsMaxLevel)
            {
                _hintText.text = _maxLevelHint;
            }
            else if (!_entry.CanAfford)
            {
                _hintText.SetText("NEEDS {0} MORE COINS", _entry.CoinsMissing);
            }
            else
            {
                _hintText.text = string.Empty;
            }
        }

        private void HandleUpgradeClicked() => _onUpgrade?.Invoke();
    }
}
