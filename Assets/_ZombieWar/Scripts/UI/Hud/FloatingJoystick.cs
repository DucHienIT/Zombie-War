using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace ZombieWar.UI
{
    // Floating joystick: the stick sets up wherever the thumb lands inside the touch area, and the
    // base drags along once the thumb slides past the rim, so reversing direction never needs the
    // thumb to travel back through the origin first.
    //
    // It drives <Gamepad>/leftStick through the Input System's on-screen device exactly like the
    // package's OnScreenStick, so the Move action and everything downstream stay untouched. It is
    // written in-house because OnScreenStick accepts every pointer (a second finger resting on the
    // lower half re-centres the stick, and lifting either finger zeroes it) and keeps its origin
    // nailed to the press point with no way to make the base follow.
    //
    // Lives on the object the touch area bubbles up to (Handle): uGUI delivers drag and release to
    // whichever object took the press, so the whole gesture lands on this one component.
    public sealed class FloatingJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const string LogPrefix = "[UI]";
        private const int NoPointer = int.MinValue;

        [InputControl(layout = "Vector2")]
        [SerializeField] private string _controlPath;
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private CanvasGroup _visuals;

        [Header("Stick")]
        // Thumb travel from the origin, in canvas units, that reads as full deflection.
        [SerializeField] private float _movementRange = 110f;
        [SerializeField] private bool _baseFollowsThumb = true;

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
        // Stick origin in the touch area's local space; slides after the thumb while following.
        private Vector2 _origin;
        private int _pointerId = NoPointer;
        private Tween _fadeTween;
        private Tween _moveTween;

        protected override string controlPathInternal
        {
            get => _controlPath;
            set => _controlPath = value;
        }

        private void Awake()
        {
            if (_root == null || _handle == null || _visuals == null || _movementRange <= 0f
                || !(_root.parent is RectTransform area))
            {
                Debug.LogError($"{LogPrefix} FloatingJoystick has an unassigned reference or a non-positive movement range.", this);
                enabled = false;
                return;
            }

            _area = area;
            _home = _root.anchoredPosition;
            _visuals.alpha = _idleAlpha;
        }

        // A canvas that goes away mid-drag never delivers the release, so park everything here; the
        // base class already resets the control value.
        protected override void OnDisable()
        {
            base.OnDisable();
            _pointerId = NoPointer;
            KillTweens();
            if (_root != null)
            {
                _root.anchoredPosition = _home;
            }

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }

            if (_visuals != null)
            {
                _visuals.alpha = _idleAlpha;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != NoPointer || !TryGetAreaPoint(eventData, out Vector2 local))
            {
                return;
            }

            _pointerId = eventData.pointerId;
            _origin = ClampToArea(local);
            _moveTween?.Kill();
            _moveTween = null;
            PlaceRoot();
            _handle.anchoredPosition = Vector2.zero;
            Fade(_activeAlpha, _pressFadeDuration);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId || !TryGetAreaPoint(eventData, out Vector2 local))
            {
                return;
            }

            Vector2 delta = local - _origin;
            if (_baseFollowsThumb && delta.sqrMagnitude > _movementRange * _movementRange)
            {
                _origin = ClampToArea(local - delta.normalized * _movementRange);
                PlaceRoot();
                delta = local - _origin;
            }

            delta = Vector2.ClampMagnitude(delta, _movementRange);
            _handle.anchoredPosition = delta;
            SendValueToControl(delta / _movementRange);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId)
            {
                return;
            }

            _pointerId = NoPointer;
            SendValueToControl(Vector2.zero);
            _handle.anchoredPosition = Vector2.zero;
            KillTweens();
            _moveTween = _root.DOAnchorPos(_home, _returnDuration).SetEase(Ease.OutQuad).SetLink(gameObject).SetUpdate(true);
            _fadeTween = _visuals.DOFade(_idleAlpha, _releaseFadeDuration).SetLink(gameObject).SetUpdate(true);
        }

        private bool TryGetAreaPoint(PointerEventData eventData, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out local);
        }

        private Vector2 ClampToArea(Vector2 local)
        {
            Rect rect = _area.rect;
            local.x = Mathf.Clamp(local.x, rect.xMin + _edgePadding, rect.xMax - _edgePadding);
            local.y = Mathf.Clamp(local.y, rect.yMin + _edgePadding, rect.yMax - _edgePadding);
            return local;
        }

        // anchoredPosition is measured from the root's anchor, not from the middle of the area, so
        // the root keeps its bottom-centre anchor - and with it a home that reads the same on every
        // aspect ratio.
        private void PlaceRoot()
        {
            Rect rect = _area.rect;
            Vector2 anchor = (_root.anchorMin + _root.anchorMax) * 0.5f;
            var anchorLocal = new Vector2(rect.xMin + rect.width * anchor.x, rect.yMin + rect.height * anchor.y);
            _root.anchoredPosition = _origin - anchorLocal;
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
