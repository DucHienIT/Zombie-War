using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class CountdownView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _glow;
        [SerializeField] private RectTransform _punchTarget;

        [Header("Punch")]
        [SerializeField] private float _punchScale = 0.4f;
        [SerializeField] private float _punchDuration = 0.35f;
        [SerializeField] private float _glowFadeDuration = 0.35f;

        private void Awake()
        {
            if (_canvas == null || _text == null || _glow == null || _punchTarget == null)
            {
                Debug.LogError($"{LogPrefix} CountdownView has an unassigned reference.", this);
            }
        }

        public void SetVisible(bool visible) => _canvas.enabled = visible;

        public void SetSeconds(int seconds)
        {
            if (seconds <= 0)
            {
                return;
            }

            _text.SetText("{0}", seconds);
            _punchTarget.DOKill(true);
            _punchTarget.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 1, 0f).SetLink(gameObject);
            _glow.DOKill();
            _glow.DOFade(0f, _glowFadeDuration).From(1f).SetLink(gameObject);
        }
    }
}
