using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // Shared top strip of every menu page: player level with XP bar, coin count, settings gear.
    public sealed class MenuHeaderView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private TMP_Text _levelText;
        // Anchor-driven fill, like the health bar, so the 9-slice pill keeps its rounded end.
        [SerializeField] private RectTransform _xpFill;
        [SerializeField] private TMP_Text _xpText;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private Button _settingsButton;

        private Action _onSettingsClicked;

        private void Awake()
        {
            if (_levelText == null || _xpFill == null || _xpText == null || _coinsText == null || _settingsButton == null)
            {
                Debug.LogError($"{LogPrefix} MenuHeaderView has an unassigned reference.", this);
                return;
            }

            _settingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        public void Init(Action onSettingsClicked) => _onSettingsClicked = onSettingsClicked;

        public void Set(in MenuHeaderData data)
        {
            _levelText.SetText("LEVEL {0}", data.Level);
            float normalized = data.XpToNext > 0 ? Mathf.Clamp01((float)data.Xp / data.XpToNext) : 1f;
            Vector2 max = _xpFill.anchorMax;
            max.x = normalized;
            _xpFill.anchorMax = max;
            _xpText.SetText("{0}/{1}", data.Xp, data.XpToNext);
            _coinsText.SetText("{0}", data.Coins);
        }

        private void HandleSettingsClicked() => _onSettingsClicked?.Invoke();
    }
}
