using UnityEngine;

namespace ZombieWar.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;

        private Rect _appliedSafeArea;
        private int _appliedWidth;
        private int _appliedHeight;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            bool unchanged = _appliedSafeArea == Screen.safeArea && _appliedWidth == Screen.width && _appliedHeight == Screen.height;
            if (!unchanged)
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safeArea = Screen.safeArea;
            int width = Screen.width;
            int height = Screen.height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= width;
            anchorMin.y /= height;
            anchorMax.x /= width;
            anchorMax.y /= height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            _appliedSafeArea = safeArea;
            _appliedWidth = width;
            _appliedHeight = height;
        }
    }
}
