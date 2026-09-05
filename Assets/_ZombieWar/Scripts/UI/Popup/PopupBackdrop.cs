using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopupBackdrop : MonoBehaviour, IPointerClickHandler
    {
        private const string LogPrefix = "[Popup]";

        [SerializeField] private PopupManager _manager;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _image;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.18f;
        [SerializeField] private float _shownAlpha = 1f;

        private bool _isShown;

        private void Awake()
        {
            if (_manager == null || _canvasGroup == null || _image == null)
            {
                Debug.LogError($"{LogPrefix} PopupBackdrop has an unassigned reference.", this);
                return;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        // Guarded by _isShown so a second popup opening does not restart the fade.
        public void Show()
        {
            if (_isShown)
            {
                return;
            }

            _isShown = true;
            _canvasGroup.blocksRaycasts = true;
            DOTween.Kill(_canvasGroup);
            _canvasGroup.DOFade(_shownAlpha, _fadeDuration).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide()
        {
            if (!_isShown)
            {
                return;
            }

            _isShown = false;
            _canvasGroup.blocksRaycasts = false;
            DOTween.Kill(_canvasGroup);
            _canvasGroup.DOFade(0f, _fadeDuration).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData) => _manager.HandleBackdropClicked();
    }
}
