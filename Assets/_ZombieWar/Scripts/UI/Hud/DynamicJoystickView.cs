using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace ZombieWar.UI
{
    // Floating (dynamic) joystick: the stick sets up wherever the thumb lands inside the touch
    // area instead of being nailed to one spot on the HUD.
    //
    // It only moves the visual root - the On-Screen Stick underneath keeps driving
    // <Gamepad>/leftStick exactly as before, so there is no second input path. That works
    // because the stick runs in RelativePositionWithStaticOrigin mode: the handle offset is a
    // delta from the press point, which is independent of where this root sits.
    //
    // The component has to live on the same object as the On-Screen Stick: the EventSystem
    // hands every handler on that object the same pointer events, while a parent would never
    // see them (the stick consumes them).
    [RequireComponent(typeof(OnScreenStick))]
    public sealed class DynamicJoystickView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private RectTransform _root;
        [SerializeField] private CanvasGroup _visuals;

        [Header("Placement")]
        // Keeps the ring fully on screen when the thumb lands near an edge.
        [SerializeField] private float _edgePadding = 150f;
        [SerializeField] private float _returnDuration = 0.18f;

        [Header("Fade")]
        [SerializeField] private float _idleAlpha = 0.5f;
        [SerializeField] private float _activeAlpha = 1f;
        [SerializeField] private float _pressFadeDuration = 0.08f;
        [SerializeField] private float _releaseFadeDuration = 0.18f;

        private RectTransform _area;
        private Vector2 _home;
        private Tween _fadeTween;
        private Tween _moveTween;

        private void Awake()
        {
            if (_root == null || _visuals == null || !(_root.parent is RectTransform area))
            {
                Debug.LogError($"{LogPrefix} DynamicJoystickView has an unassigned reference.", this);
                enabled = false;
                return;
            }

            _area = area;
            _home = _root.anchoredPosition;
            _visuals.alpha = _idleAlpha;
        }

        // A canvas that goes away mid-drag never delivers the pointer up, so park the stick here.
        private void OnDisable()
        {
            KillTweens();
            if (_root != null)
            {
                _root.anchoredPosition = _home;
            }

            if (_visuals != null)
            {
                _visuals.alpha = _idleAlpha;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            MoveToPointer(eventData);
            Fade(_activeAlpha, _pressFadeDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            KillTweens();
            _moveTween = _root.DOAnchorPos(_home, _returnDuration).SetEase(Ease.OutQuad).SetLink(gameObject).SetUpdate(true);
            _fadeTween = _visuals.DOFade(_idleAlpha, _releaseFadeDuration).SetLink(gameObject).SetUpdate(true);
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Rect rect = _area.rect;
            local.x = Mathf.Clamp(local.x, rect.xMin + _edgePadding, rect.xMax - _edgePadding);
            local.y = Mathf.Clamp(local.y, rect.yMin + _edgePadding, rect.yMax - _edgePadding);

            // anchoredPosition is measured from the anchor, not from the middle of the area, so
            // the stick keeps its bottom-centre anchor - and with it a home that reads the same
            // on every aspect ratio.
            Vector2 anchor = (_root.anchorMin + _root.anchorMax) * 0.5f;
            var anchorLocal = new Vector2(rect.xMin + rect.width * anchor.x, rect.yMin + rect.height * anchor.y);

            _moveTween?.Kill();
            _moveTween = null;
            _root.anchoredPosition = local - anchorLocal;
        }

        private void Fade(float alpha, float duration)
        {
            _fadeTween?.Kill();
            _fadeTween = _visuals.DOFade(alpha, duration).SetLink(gameObject).SetUpdate(true);
        }

        private void KillTweens()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
            _moveTween?.Kill();
            _moveTween = null;
        }
    }
}
