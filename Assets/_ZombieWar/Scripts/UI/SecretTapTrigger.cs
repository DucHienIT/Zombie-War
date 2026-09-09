using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZombieWar.UI
{
    // Fires once after a run of quick taps on this object. A pause longer than the interval
    // between two taps restarts the count, so a stray tap never carries over.
    public sealed class SecretTapTrigger : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int _requiredTaps = 7;
        [SerializeField] private float _maxTapInterval = 1f;

        private Action _onTriggered;
        private int _taps;
        private float _lastTapTime;

        public void Init(Action onTriggered)
        {
            _onTriggered = onTriggered;
        }

        private void OnDisable()
        {
            _taps = 0;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            float now = Time.unscaledTime;
            bool sequenceBroken = _taps > 0 && now - _lastTapTime > _maxTapInterval;
            if (sequenceBroken)
            {
                _taps = 0;
            }

            _lastTapTime = now;
            _taps++;
            if (_taps < _requiredTaps)
            {
                return;
            }

            _taps = 0;
            _onTriggered?.Invoke();
        }
    }
}
