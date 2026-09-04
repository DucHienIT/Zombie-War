using DG.Tweening;
using TMPro;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    public sealed class CountdownView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _text;

        [Header("Punch")]
        [SerializeField] private float _punchScale = 0.4f;
        [SerializeField] private float _punchDuration = 0.35f;

        private void Awake()
        {
            if (_flow == null || _canvas == null || _text == null)
            {
                Debug.LogError($"{LogPrefix} CountdownView has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _flow.OnCountdownChanged += HandleCountdownChanged;
            _flow.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            _flow.OnCountdownChanged -= HandleCountdownChanged;
            _flow.OnStateChanged -= HandleStateChanged;
        }

        private void HandleCountdownChanged(int seconds)
        {
            if (seconds <= 0)
            {
                return;
            }

            _text.SetText("{0}", seconds);
            _text.transform.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 1, 0f).SetLink(gameObject);
        }

        private void HandleStateChanged(GameState state)
        {
            _canvas.enabled = state == GameState.Countdown;
        }
    }
}
