using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class SkillCardView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private GameObject _newBadge;

        [Header("Stars")]
        // One authored slot per stack a skill can ever have; a skill with a lower cap hides
        // the extras and the row slides back to stay centred.
        [SerializeField] private Image[] _stars;
        [SerializeField] private RectTransform _starRow;
        [SerializeField] private float _starSpacing = 48f;
        [SerializeField] private Color _starOnColor;
        [SerializeField] private Color _starOffColor;

        private Action _onChosen;
        private Vector2 _starRowRestPosition;

        private void Awake()
        {
            bool missing = _button == null || _icon == null || _nameText == null || _descriptionText == null
                           || _newBadge == null || _stars == null || _stars.Length == 0 || _starRow == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} SkillCardView has an unassigned reference.", this);
                return;
            }

            _starRowRestPosition = _starRow.anchoredPosition;
            _button.onClick.AddListener(HandleClicked);
        }

        public void Bind(in SkillCardData data, Action onChosen)
        {
            _onChosen = onChosen;
            // The plate art is near black, so the accent has to land on the white line icon
            // itself to read as this skill's colour.
            _icon.sprite = data.Icon;
            _icon.color = data.AccentColor;
            _nameText.text = data.DisplayName;
            _descriptionText.text = data.Description;
            _newBadge.SetActive(data.IsNew);
            DrawStars(data.NextStack, data.MaxStacks);
        }

        private void DrawStars(int filled, int maxStacks)
        {
            if (maxStacks > _stars.Length)
            {
                Debug.LogError($"{LogPrefix} a skill caps at {maxStacks} but the card has {_stars.Length} star slots.", this);
            }

            _starRow.anchoredPosition = _starRowRestPosition
                                        + new Vector2((_stars.Length - maxStacks) * _starSpacing * 0.5f, 0f);
            for (int i = 0; i < _stars.Length; i++)
            {
                bool exists = i < maxStacks;
                _stars[i].enabled = exists;
                if (exists)
                {
                    _stars[i].color = i < filled ? _starOnColor : _starOffColor;
                }
            }
        }

        private void HandleClicked() => _onChosen?.Invoke();
    }
}
