using DG.Tweening;
using TMPro;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    public sealed class TimerView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private TMP_Text _text;

        [Header("Alerts")]
        [SerializeField] private int _pulseAtSeconds = 30;
        [SerializeField] private int _warningAtSeconds = 10;
        [SerializeField] private Color _warningColor;
        [SerializeField] private float _pulseScale = 1.25f;
        [SerializeField] private float _pulseDuration = 0.4f;

        private readonly TimeTextFormatter _formatter = new TimeTextFormatter();
        private Color _baseColor;
        private int _shownSeconds = -1;

        private void Awake()
        {
            if (_flow == null || _text == null)
            {
                Debug.LogError($"{LogPrefix} TimerView has an unassigned reference.", this);
                return;
            }

            _baseColor = _text.color;
        }

        private void OnEnable()
        {
            _flow.OnRemainingTimeChanged += HandleRemainingTimeChanged;
        }

        private void OnDisable()
        {
            _flow.OnRemainingTimeChanged -= HandleRemainingTimeChanged;
        }

        private void HandleRemainingTimeChanged(float remaining)
        {
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = seconds;
            _formatter.Apply(_text, seconds);
            _text.color = seconds <= _warningAtSeconds ? _warningColor : _baseColor;

            if (seconds == _pulseAtSeconds || seconds <= _warningAtSeconds)
            {
                _text.transform.DOPunchScale(Vector3.one * (_pulseScale - 1f), _pulseDuration, 1, 0f).SetLink(gameObject);
            }
        }
    }
}
