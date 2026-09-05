using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // One tile of the weapon grid: name pill, icon, level and a level progress bar.
    // A locked gun shows a lock and "coming soon" instead of the icon block.
    public sealed class WeaponCardView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private GameObject _unlockedGroup;
        [SerializeField] private GameObject _lockedGroup;

        [Header("Unlocked")]
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _levelText;
        // Anchor-driven fill so the 9-slice pill keeps its rounded end.
        [SerializeField] private RectTransform _levelFill;
        [SerializeField] private TMP_Text _levelProgressText;

        private Action _onClicked;

        private void Awake()
        {
            bool missing = _button == null || _nameText == null || _unlockedGroup == null || _lockedGroup == null
                           || _icon == null || _levelText == null || _levelFill == null || _levelProgressText == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} WeaponCardView has an unassigned reference.", this);
                return;
            }

            _button.onClick.AddListener(HandleClicked);
        }

        public void Init(Action onClicked) => _onClicked = onClicked;

        public void Bind(in WeaponEntryData entry)
        {
            _nameText.text = entry.DisplayName;
            _unlockedGroup.SetActive(entry.Unlocked);
            _lockedGroup.SetActive(!entry.Unlocked);
            if (!entry.Unlocked)
            {
                return;
            }

            _icon.sprite = entry.Icon;
            if (entry.IsMaxLevel)
            {
                _levelText.text = "MAX LEVEL";
            }
            else
            {
                _levelText.SetText("LEVEL {0}", entry.Level);
            }

            float normalized = entry.MaxLevel > 0 ? Mathf.Clamp01((float)entry.Level / entry.MaxLevel) : 1f;
            Vector2 max = _levelFill.anchorMax;
            max.x = normalized;
            _levelFill.anchorMax = max;
            _levelProgressText.SetText("{0}/{1}", entry.Level, entry.MaxLevel);
        }

        private void HandleClicked() => _onClicked?.Invoke();
    }
}
