using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZombieWar.UI
{
    // Press feedback shared by every button. The rest pose is read from the prefab so the
    // tween never drags an authored scale back to one.
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _pressedScale = 0.92f;
        [SerializeField] private float _pressDuration = 0.08f;
        [SerializeField] private float _releaseDuration = 0.18f;
        [SerializeField] private Ease _releaseEase = Ease.OutBack;

        private Vector3 _restScale;

        private void Awake()
        {
            if (_target == null)
            {
                _target = (RectTransform)transform;
            }

            _restScale = _target.localScale;
        }

        private void OnDisable()
        {
            _target.DOKill();
            _target.localScale = _restScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _target.DOKill();
            _target.DOScale(_restScale * _pressedScale, _pressDuration).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _target.DOKill();
            _target.DOScale(_restScale, _releaseDuration).SetEase(_releaseEase).SetUpdate(true).SetLink(gameObject);
        }
    }
}
