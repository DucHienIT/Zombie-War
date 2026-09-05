using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    public sealed class TimerView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private TMP_Text _text;
        [SerializeField] private RectTransform _pulseTarget;

        [Header("Alerts")]
        [SerializeField] private int _pulseAtSeconds = 30;
        [SerializeField] private int _warningAtSeconds = 10;
        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _warningColor;
        [SerializeField] private float _pulseScale = 0.25f;
        [SerializeField] private float _pulseDuration = 0.4f;

        private readonly TimeTextFormatter _formatter = new TimeTextFormatter();
        private int _shownSeconds = -1;

        private void Awake()
        {
            if (_text == null || _pulseTarget == null)
            {
                Debug.LogError($"{LogPrefix} TimerView has an unassigned reference.", this);
            }
        }

        public void SetRemaining(float remaining)
        {
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = seconds;
            _formatter.Apply(_text, seconds);
            _text.color = seconds <= _warningAtSeconds ? _warningColor : _normalColor;

            if (seconds == _pulseAtSeconds || seconds <= _warningAtSeconds)
            {
                _pulseTarget.DOPunchScale(Vector3.one * _pulseScale, _pulseDuration, 1, 0f).SetLink(gameObject);
            }
        }
    }
}
