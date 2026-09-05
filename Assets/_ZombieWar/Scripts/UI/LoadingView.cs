using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    public sealed class LoadingView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";
        private const int PercentScale = 100;

        // Anchor-driven like the health and XP bars: a 9-slice pill would lose its rounded cap
        // under Image.Type.Filled, and anchors read right on the very first frame.
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _percentText;
        [SerializeField] private TMP_Text _hintText;

        [Header("Hint Pulse")]
        [SerializeField] private float _hintMinAlpha = 0.35f;
        [SerializeField] private float _hintPulseDuration = 0.7f;

        private int _shownPercent = -1;

        private void Awake()
        {
            if (_fill == null || _percentText == null || _hintText == null)
            {
                Debug.LogError($"{LogPrefix} LoadingView has an unassigned reference.", this);
                return;
            }

            _hintText.DOFade(_hintMinAlpha, _hintPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void SetProgress(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            _fill.anchorMax = new Vector2(clamped, 1f);

            int percent = Mathf.RoundToInt(clamped * PercentScale);
            if (percent == _shownPercent)
            {
                return;
            }

            _shownPercent = percent;
            _percentText.SetText("{0}%", percent);
        }
    }
}
