using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class BombButtonView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _chargesText;
        [SerializeField] private Image _icon;
        // Radial cover that empties on throw and refills over the cooldown.
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private RectTransform _readyPunchTarget;

        [Header("Colors")]
        [SerializeField] private Color _readyIconColor;
        [SerializeField] private Color _spentIconColor;

        [Header("Ready Punch")]
        [SerializeField] private float _punchScale = 0.18f;
        [SerializeField] private float _punchDuration = 0.3f;

        private void Awake()
        {
            bool missing = _button == null || _chargesText == null || _icon == null || _cooldownFill == null
                           || _readyPunchTarget == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} BombButtonView has an unassigned reference.", this);
            }
        }

        public void Init(UnityAction onThrowRequested) => _button.onClick.AddListener(onThrowRequested);

        public void SetCharges(int charges)
        {
            _chargesText.SetText("{0}", charges);
            bool available = charges > 0;
            _button.interactable = available;
            _icon.color = available ? _readyIconColor : _spentIconColor;
        }

        public void SetCooldown(float progress) => _cooldownFill.fillAmount = 1f - progress;

        public void PlayReady()
        {
            _readyPunchTarget.DOKill(true);
            _readyPunchTarget.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 1, 0f).SetLink(gameObject);
        }
    }
}
