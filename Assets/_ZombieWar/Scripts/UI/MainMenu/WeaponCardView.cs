using System;
using DG.Tweening;
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
        // The whole tile, faded on reveal.
        [SerializeField] private CanvasGroup _group;

        [Header("Unlocked")]
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _levelText;
        // Anchor-driven fill so the 9-slice pill keeps its rounded end.
        [SerializeField] private RectTransform _levelFill;
        [SerializeField] private TMP_Text _levelProgressText;

        [Header("Reveal")]
        [SerializeField] private float _revealDuration = 0.35f;
        [SerializeField] private float _revealScaleFrom = 0.8f;
        [SerializeField] private Ease _revealEase = Ease.OutBack;

        [Header("Upgrade")]
        [SerializeField] private float _punchScale = 0.18f;
        [SerializeField] private float _punchDuration = 0.4f;
        [SerializeField] private float _fillDuration = 0.35f;
        [SerializeField] private float _levelPopScale = 1.3f;
        [SerializeField] private float _levelPopDuration = 0.35f;

        private Action _onClicked;
        private RectTransform _rect;
        private Vector3 _restScale;
        private Vector3 _levelRestScale;
        // The page binds while it is still inactive, before this Awake has read the authored
        // rest pose; until then nothing may tween.
        private bool _awake;

        private void Awake()
        {
            bool missing = _button == null || _nameText == null || _unlockedGroup == null || _lockedGroup == null
                           || _icon == null || _levelText == null || _levelFill == null || _levelProgressText == null
                           || _group == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} WeaponCardView has an unassigned reference.", this);
                return;
            }

            _rect = (RectTransform)transform;
            _restScale = _rect.localScale;
            _levelRestScale = _levelText.rectTransform.localScale;
            _awake = true;
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (!_awake)
            {
                return;
            }

            KillTweens();
            _rect.localScale = _restScale;
            _levelText.rectTransform.localScale = _levelRestScale;
            _group.alpha = 1f;
        }

        public void Init(Action onClicked) => _onClicked = onClicked;

        public void Bind(in WeaponEntryData entry) => Draw(entry, false);

        public void PlayReveal(float delay)
        {
            if (!_awake)
            {
                return;
            }

            _rect.DOKill();
            _group.DOKill();
            _group.alpha = 0f;
            _rect.localScale = _restScale * _revealScaleFrom;
            _group.DOFade(1f, _revealDuration).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
            _rect.DOScale(_restScale, _revealDuration).SetEase(_revealEase).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
        }

        public void PlayUpgrade(in WeaponEntryData entry)
        {
            Draw(entry, _awake);
            if (!_awake)
            {
                return;
            }

            _rect.DOKill();
            _rect.localScale = _restScale;
            _rect.DOPunchScale(_restScale * _punchScale, _punchDuration).SetUpdate(true).SetLink(gameObject);
            RectTransform level = _levelText.rectTransform;
            level.DOKill();
            level.localScale = _levelRestScale * _levelPopScale;
            level.DOScale(_levelRestScale, _levelPopDuration).SetEase(_revealEase).SetUpdate(true).SetLink(gameObject);
        }

        private void Draw(in WeaponEntryData entry, bool animate)
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
            _levelFill.DOKill();
            if (animate)
            {
                _levelFill.DOAnchorMax(max, _fillDuration).SetUpdate(true).SetLink(gameObject);
            }
            else
            {
                _levelFill.anchorMax = max;
            }

            _levelProgressText.SetText("{0}/{1}", entry.Level, entry.MaxLevel);
        }

        private void KillTweens()
        {
            _rect.DOKill();
            _group.DOKill();
            _levelText.rectTransform.DOKill();
            // Completed, not killed: a half-played bar would stay wrong until the next rebind.
            _levelFill.DOComplete();
        }

        private void HandleClicked() => _onClicked?.Invoke();
    }
}
