using System;
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

        private Action _onClicked;

        public bool IsLocked => _locked;

        private void Awake()
        {
            if (_button == null || _highlight == null || _icon == null || _label == null)
            {
                Debug.LogError($"{LogPrefix} MenuTabButtonView has an unassigned reference.", this);
                return;
            }

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

        public void Init(Action onClicked) => _onClicked = onClicked;

        public void SetSelected(bool selected)
        {
            if (_locked)
            {
                return;
            }

            _highlight.enabled = selected;
            Tint(selected ? _selectedColor : _normalColor);
        }

        private void Tint(Color color)
        {
            _icon.color = color;
            _label.color = color;
        }

        private void HandleClicked() => _onClicked?.Invoke();
    }
}
