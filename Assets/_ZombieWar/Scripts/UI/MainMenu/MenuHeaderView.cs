using System;
using DG.Tweening;
using DG.Tweening.Core;
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
        [SerializeField] private RectTransform _coinChip;
        [SerializeField] private Button _settingsButton;

        [Header("Change")]
        [SerializeField] private float _xpFillDuration = 0.45f;
        [SerializeField] private float _coinCountDuration = 0.5f;
        [SerializeField] private float _coinPunchScale = 0.22f;
        [SerializeField] private float _coinPunchDuration = 0.4f;

        private Action _onSettingsClicked;
        private Vector3 _coinRestScale;
        private int _shownCoins;
        private DOGetter<int> _coinGetter;
        private DOSetter<int> _coinSetter;
        // The first value the header ever draws has nothing to count up from.
        private bool _drawn;

        private void Awake()
        {
            bool missing = _levelText == null || _xpFill == null || _xpText == null || _coinsText == null
                           || _coinChip == null || _settingsButton == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} MenuHeaderView has an unassigned reference.", this);
                return;
            }

            _coinRestScale = _coinChip.localScale;
            _coinGetter = GetShownCoins;
            _coinSetter = SetShownCoins;
            _settingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        private void OnDisable()
        {
            _xpFill.DOComplete();
            _coinChip.DOKill();
            _coinChip.localScale = _coinRestScale;
            // Completed, not killed: the strip must read the real coin total when it comes back.
            DOTween.Complete(this);
        }

        public void Init(Action onSettingsClicked) => _onSettingsClicked = onSettingsClicked;

        public void Set(in MenuHeaderData data)
        {
            _levelText.SetText("LEVEL {0}", data.Level);
            _xpText.SetText("{0}/{1}", data.Xp, data.XpToNext);

            float normalized = data.XpToNext > 0 ? Mathf.Clamp01((float)data.Xp / data.XpToNext) : 1f;
            Vector2 max = _xpFill.anchorMax;
            max.x = normalized;
            _xpFill.DOKill();
            if (_drawn)
            {
                _xpFill.DOAnchorMax(max, _xpFillDuration).SetUpdate(true).SetLink(gameObject);
            }
            else
            {
                _xpFill.anchorMax = max;
            }

            SetCoins(data.Coins);
            _drawn = true;
        }

        private void SetCoins(int coins)
        {
            DOTween.Kill(this);
            if (!_drawn || coins == _shownCoins)
            {
                SetShownCoins(coins);
                return;
            }

            DOTween.To(_coinGetter, _coinSetter, coins, _coinCountDuration).SetTarget(this).SetUpdate(true).SetLink(gameObject);
            _coinChip.DOKill();
            _coinChip.localScale = _coinRestScale;
            _coinChip.DOPunchScale(_coinRestScale * _coinPunchScale, _coinPunchDuration).SetUpdate(true).SetLink(gameObject);
        }

        private int GetShownCoins() => _shownCoins;

        private void SetShownCoins(int value)
        {
            _shownCoins = value;
            _coinsText.SetText("{0}", value);
        }

        private void HandleSettingsClicked() => _onSettingsClicked?.Invoke();
    }
}
