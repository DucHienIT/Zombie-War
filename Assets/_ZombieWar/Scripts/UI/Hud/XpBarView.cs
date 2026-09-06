using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    public sealed class XpBarView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        // Driven by anchors for the same reason the health bar is: it reads right on the very
        // first frame, before the canvas scaler has sized anything.
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private RectTransform _levelPunchTarget;

        [Header("Animation")]
        [SerializeField] private float _fillDuration = 0.18f;
        [SerializeField] private float _punchScale = 0.35f;
        [SerializeField] private float _punchDuration = 0.35f;

        private Tween _fillTween;
        private int _shownLevel = -1;
        private Vector3 _punchRestScale;

        private void Awake()
        {
            if (_fill == null || _levelText == null || _levelPunchTarget == null)
            {
                Debug.LogError($"{LogPrefix} XpBarView has an unassigned reference.", this);
                return;
            }

            _punchRestScale = _levelPunchTarget.localScale;
        }

        public void SetXp(float normalized, int battleLevel)
        {
            _fillTween?.Kill();
            _fillTween = _fill.DOAnchorMax(new Vector2(Mathf.Clamp01(normalized), 1f), _fillDuration).SetLink(gameObject);

            if (battleLevel == _shownLevel)
            {
                return;
            }

            bool isFirstBind = _shownLevel < 0;
            _shownLevel = battleLevel;
            _levelText.SetText("{0}", battleLevel);
            if (!isFirstBind)
            {
                _levelPunchTarget.DOKill();
                _levelPunchTarget.localScale = _punchRestScale;
                _levelPunchTarget.DOPunchScale(_punchRestScale * _punchScale, _punchDuration, 1, 0f).SetLink(gameObject);
            }
        }
    }
}
