using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class MenuTabButtonView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _button;
        [SerializeField] private Image _highlight;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        // Optional: a tab whose icon already is a lock draws no extra badge.
        [SerializeField] private GameObject _lockBadge;
        // A locked tab (feature not shipped yet) never becomes selectable.
        [SerializeField] private bool _locked;

        [Header("Colors")]
        [SerializeField] private Color _selectedColor = Color.white;
        [SerializeField] private Color _normalColor = Color.gray;
        [SerializeField] private Color _lockedColor = Color.gray;

        [Header("Select")]
        [SerializeField] private float _tintDuration = 0.18f;
        [SerializeField] private float _highlightDuration = 0.2f;
        [SerializeField] private float _iconPunchScale = 0.3f;
        [SerializeField] private float _iconPunchDuration = 0.35f;
        [SerializeField] private float _iconLift = 8f;
        [SerializeField] private Ease _liftEase = Ease.OutBack;

        private Action _onClicked;
        private RectTransform _iconRect;
        private Vector3 _iconRestScale;
        private Vector2 _iconRestPosition;
        private float _highlightRestAlpha;

        public bool IsLocked => _locked;

        private void Awake()
        {
            if (_button == null || _highlight == null || _icon == null || _label == null)
            {
                Debug.LogError($"{LogPrefix} MenuTabButtonView has an unassigned reference.", this);
                return;
            }

            _iconRect = _icon.rectTransform;
            _iconRestScale = _iconRect.localScale;
            _iconRestPosition = _iconRect.anchoredPosition;
            _highlightRestAlpha = _highlight.color.a;
            _button.onClick.AddListener(HandleClicked);
            _button.interactable = !_locked;
            if (_lockBadge != null)
            {
                _lockBadge.SetActive(_locked);
            }
            if (_locked)
            {
                Tint(_lockedColor);
                _highlight.enabled = false;
            }
        }

        private void OnDisable()
        {
            if (_iconRect == null)
            {
                return;
            }

            _iconRect.DOKill();
            _highlight.DOKill();
            _icon.DOKill();
            _label.DOKill();
            _iconRect.localScale = _iconRestScale;
            _iconRect.anchoredPosition = _iconRestPosition;
        }

        public void Init(Action onClicked) => _onClicked = onClicked;

        public void SetSelected(bool selected)
        {
            if (_locked)
            {
                return;
            }

            Color tint = selected ? _selectedColor : _normalColor;
            _icon.DOKill();
            _label.DOKill();
            _icon.DOColor(tint, _tintDuration).SetUpdate(true).SetLink(gameObject);
            _label.DOColor(tint, _tintDuration).SetUpdate(true).SetLink(gameObject);

            _highlight.DOKill();
            _highlight.enabled = true;
            Color highlight = _highlight.color;
            highlight.a = selected ? 0f : _highlightRestAlpha;
            _highlight.color = highlight;
            Tween fade = _highlight.DOFade(selected ? _highlightRestAlpha : 0f, _highlightDuration)
                .SetUpdate(true).SetLink(gameObject);
            if (!selected)
            {
                fade.OnComplete(DisableHighlight);
            }

            _iconRect.DOKill();
            _iconRect.localScale = _iconRestScale;
            Vector2 target = selected ? _iconRestPosition + new Vector2(0f, _iconLift) : _iconRestPosition;
            _iconRect.DOAnchorPos(target, _highlightDuration).SetEase(_liftEase).SetUpdate(true).SetLink(gameObject);
            if (!selected)
            {
                return;
            }

            _iconRect.DOPunchScale(_iconRestScale * _iconPunchScale, _iconPunchDuration).SetUpdate(true).SetLink(gameObject);
        }

        private void DisableHighlight() => _highlight.enabled = false;

        private void Tint(Color color)
        {
            _icon.color = color;
            _label.color = color;
        }

        private void HandleClicked() => _onClicked?.Invoke();
    }
}
