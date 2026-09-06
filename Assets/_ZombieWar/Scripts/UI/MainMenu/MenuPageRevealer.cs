using DG.Tweening;
using UnityEngine;

namespace ZombieWar.UI
{
    // Staggered entrance for the authored blocks of one menu page. Pages are switched on by the
    // tab bar, so every visit replays it; the rest pose is read from the prefab at Awake and
    // restored on the way out, which keeps a killed tween from parking a block off screen.
    public sealed class MenuPageRevealer : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        // Revealed in array order.
        [SerializeField] private CanvasGroup[] _items;

        [Header("Reveal")]
        [SerializeField] private float _duration = 0.32f;
        [SerializeField] private float _stagger = 0.06f;
        // Distance below the rest position each block starts at.
        [SerializeField] private float _rise = 60f;
        [SerializeField] private float _scaleFrom = 0.94f;
        [SerializeField] private Ease _ease = Ease.OutCubic;

        private RectTransform[] _rects;
        private Vector2[] _restPositions;
        private Vector3[] _restScales;

        private void Awake()
        {
            if (_items == null || _items.Length == 0)
            {
                Debug.LogError($"{LogPrefix} MenuPageRevealer has no items.", this);
                return;
            }

            _rects = new RectTransform[_items.Length];
            _restPositions = new Vector2[_items.Length];
            _restScales = new Vector3[_items.Length];
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null)
                {
                    Debug.LogError($"{LogPrefix} Empty reveal slot {i}.", this);
                    continue;
                }

                _rects[i] = (RectTransform)_items[i].transform;
                _restPositions[i] = _rects[i].anchoredPosition;
                _restScales[i] = _rects[i].localScale;
            }
        }

        private void OnEnable()
        {
            if (_rects == null)
            {
                return;
            }

            for (int i = 0; i < _items.Length; i++)
            {
                CanvasGroup group = _items[i];
                if (group == null)
                {
                    continue;
                }

                RectTransform rect = _rects[i];
                group.DOKill();
                rect.DOKill();
                group.alpha = 0f;
                rect.anchoredPosition = _restPositions[i] - new Vector2(0f, _rise);
                rect.localScale = _restScales[i] * _scaleFrom;

                float delay = i * _stagger;
                group.DOFade(1f, _duration).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
                rect.DOAnchorPos(_restPositions[i], _duration).SetEase(_ease).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
                rect.DOScale(_restScales[i], _duration).SetEase(_ease).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
            }
        }

        private void OnDisable()
        {
            if (_rects == null)
            {
                return;
            }

            for (int i = 0; i < _items.Length; i++)
            {
                CanvasGroup group = _items[i];
                if (group == null)
                {
                    continue;
                }

                group.DOKill();
                _rects[i].DOKill();
                group.alpha = 1f;
                _rects[i].anchoredPosition = _restPositions[i];
                _rects[i].localScale = _restScales[i];
            }
        }
    }
}
