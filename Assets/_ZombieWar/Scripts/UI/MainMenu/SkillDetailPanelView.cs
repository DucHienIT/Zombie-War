using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // The sheet under the tree for the selected node: what it does, what the next rank adds,
    // what it costs and the one button that buys it. Swapping nodes crossfades the content.
    public sealed class SkillDetailPanelView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Image _iconPlate;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _effectLabel;
        [SerializeField] private TMP_Text _currentValueText;
        [SerializeField] private TMP_Text _nextValueText;
        [SerializeField] private GameObject _nextGroup;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private GameObject _costGroup;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _hintText;

        [Header("Texts")]
        [SerializeField] private string _rankFormat = "RANK {0} / {1}";
        [SerializeField] private string _maxRankLabel = "MAX RANK";
        [SerializeField] private string _maxRankHint = "MAX RANK REACHED";
        [SerializeField] private string _lockedHintFormat = "REQUIRES {0}";
        [SerializeField] private string _coinsHintFormat = "NEEDS {0} MORE COINS";

        [Header("Colors")]
        [SerializeField] private Color _affordableCostColor;
        [SerializeField] private Color _unaffordableCostColor;
        [SerializeField] private Color _hintColor;
        [SerializeField] private Color _lockedHintColor;

        [Header("Animation")]
        [SerializeField] private float _swapDuration = 0.16f;
        [SerializeField] private float _swapSlide = 22f;
        [SerializeField] private Ease _swapEase = Ease.OutCubic;
        [SerializeField] private float _valuePunchScale = 0.3f;
        [SerializeField] private float _valuePunchDuration = 0.4f;
        [SerializeField] private int _valuePunchVibrato = 5;
        [SerializeField] private float _valuePunchElasticity = 0.5f;

        private Action _onUpgrade;
        private SkillNodeData _pending;
        private bool _hasPending;

        private void Awake()
        {
            bool missing = _group == null || _content == null || _iconPlate == null || _icon == null || _nameText == null
                           || _rankText == null || _descriptionText == null || _effectLabel == null || _currentValueText == null
                           || _nextValueText == null || _nextGroup == null || _upgradeButton == null || _costGroup == null
                           || _costText == null || _hintText == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} SkillDetailPanelView has an unassigned reference.", this);
                return;
            }

            _upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        }

        private void OnDisable()
        {
            _group.DOKill();
            _content.DOKill();
            _currentValueText.transform.DOKill();
            _nextValueText.transform.DOKill();
            _group.alpha = 1f;
            _content.anchoredPosition = Vector2.zero;
            _currentValueText.transform.localScale = Vector3.one;
            _nextValueText.transform.localScale = Vector3.one;
            if (_hasPending)
            {
                _hasPending = false;
                Draw(_pending);
            }
        }

        public void Init(Action onUpgrade) => _onUpgrade = onUpgrade;

        // Another node was tapped: the old content slips out, the new one slides in from below.
        public void Show(in SkillNodeData data, bool animate)
        {
            _group.DOKill();
            _content.DOKill();
            if (!animate || !gameObject.activeInHierarchy)
            {
                _group.alpha = 1f;
                _content.anchoredPosition = Vector2.zero;
                Draw(data);
                return;
            }

            _pending = data;
            _hasPending = true;
            _group.DOFade(0f, _swapDuration).SetUpdate(true).SetLink(gameObject).OnComplete(SwapPending);
        }

        // Same node, one rank higher: the numbers jump so the eye lands on what changed.
        public void Refresh(in SkillNodeData data)
        {
            Draw(data);
            Punch(_currentValueText.transform);
            Punch(_nextValueText.transform);
        }

        private void SwapPending()
        {
            if (!_hasPending)
            {
                return;
            }

            _hasPending = false;
            Draw(_pending);
            _content.anchoredPosition = new Vector2(0f, -_swapSlide);
            _content.DOAnchorPosY(0f, _swapDuration).SetEase(_swapEase).SetUpdate(true).SetLink(gameObject);
            _group.DOFade(1f, _swapDuration).SetUpdate(true).SetLink(gameObject);
        }

        private void Draw(in SkillNodeData data)
        {
            _icon.sprite = data.Icon;
            _iconPlate.color = data.Accent;
            _nameText.text = data.DisplayName;
            _nameText.color = data.Accent;
            _descriptionText.text = data.Description;
            _effectLabel.text = data.EffectLabel;
            _currentValueText.SetText(data.ValueFormat, data.CurrentValue);
            _nextValueText.SetText(data.ValueFormat, data.NextValue);
            _nextGroup.SetActive(!data.IsMaxed);

            if (data.IsMaxed)
            {
                _rankText.text = _maxRankLabel;
            }
            else
            {
                _rankText.SetText(_rankFormat, data.Rank, data.MaxRank);
            }

            bool sellable = data.Unlocked && !data.IsMaxed;
            _costGroup.SetActive(sellable);
            _costText.SetText("{0}", data.Cost);
            _costText.color = data.CanAfford ? _affordableCostColor : _unaffordableCostColor;
            _upgradeButton.interactable = data.CanBuy;

            if (!data.Unlocked)
            {
                _hintText.text = string.Format(_lockedHintFormat, data.RequiredNodeName);
                _hintText.color = _lockedHintColor;
            }
            else if (data.IsMaxed)
            {
                _hintText.text = _maxRankHint;
                _hintText.color = _hintColor;
            }
            else if (!data.CanAfford)
            {
                _hintText.SetText(_coinsHintFormat, data.CoinsMissing);
                _hintText.color = _unaffordableCostColor;
            }
            else
            {
                _hintText.text = string.Empty;
            }
        }

        private void Punch(Transform target)
        {
            target.DOKill();
            target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * _valuePunchScale, _valuePunchDuration, _valuePunchVibrato, _valuePunchElasticity).SetUpdate(true).SetLink(gameObject);
        }

        private void HandleUpgradeClicked() => _onUpgrade?.Invoke();
    }
}
