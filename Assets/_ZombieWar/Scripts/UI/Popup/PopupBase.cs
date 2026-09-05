using DG.Tweening;
using UnityEngine;

namespace ZombieWar.UI
{
    [DisallowMultipleComponent]
    public class PopupBase : MonoBehaviour
    {
        private const string LogPrefix = "[Popup]";

        [Header("Parts")]
        [SerializeField] private CanvasGroup _canvasGroup;
        // Left empty scales the whole popup instead of an inner panel.
        [SerializeField] private RectTransform _content;

        [Header("Behaviour")]
        [SerializeField] private bool _dimBackground = true;
        [SerializeField] private bool _closeOnBackdropClick;
        [SerializeField] private bool _closeOnBackKey = true;

        // 0 disables the shrink-to-fit pass used on short screens.
        [SerializeField] private float _fitPadding = 48f;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.18f;
        [SerializeField] private float _scaleDuration = 0.32f;
        [SerializeField] private Vector3 _closedScale = new Vector3(0.8f, 0.8f, 1f);
        [SerializeField] private Ease _scaleEase = Ease.OutBack;

        private PopupManager _manager;
        private RectTransform _rectTransform;

        public bool DimBackground => _dimBackground;
        public bool CloseOnBackdropClick => _closeOnBackdropClick;
        public bool CloseOnBackKey => _closeOnBackKey;

        private RectTransform ScaleTarget => _content != null ? _content : _rectTransform;

        protected virtual void Awake()
        {
            _rectTransform = (RectTransform)transform;
            if (_canvasGroup == null)
            {
                Debug.LogError($"{LogPrefix} {name} has no CanvasGroup - it cannot fade or block input.", this);
            }
        }

        public void BindManager(PopupManager manager) => _manager = manager;

        public void Close()
        {
            if (_manager == null)
            {
                Debug.LogError($"{LogPrefix} {name} is not registered in a PopupManager - Close would strand the caller.", this);
                NotifyClosed();
                return;
            }

            _manager.Close(this);
        }

        public void PlayShow()
        {
            gameObject.SetActive(true);
            OnShowing();

            float fit = FitScale();
            RectTransform target = ScaleTarget;
            DOTween.Kill(target);
            // Completing instead of dropping a close that is still fading keeps its OnClosed
            // callback from being swallowed by a re-open.
            DOTween.Kill(_canvasGroup, true);

            target.localScale = _closedScale * fit;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            target.DOScale(Vector3.one * fit, _scaleDuration).SetEase(_scaleEase).SetUpdate(true).SetLink(gameObject);
            _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true).SetLink(gameObject);
        }

        public void PlayClose()
        {
            RectTransform target = ScaleTarget;
            DOTween.Kill(target);
            DOTween.Kill(_canvasGroup);

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            float fit = FitScale();
            target.DOScale(_closedScale * fit, _fadeDuration).SetUpdate(true).SetLink(gameObject);
            _canvasGroup.DOFade(0f, _fadeDuration).SetUpdate(true).SetLink(gameObject)
                .OnComplete(HandleCloseFinished);
        }

        // Every way out of a popup lands here, so a bound callback can never be skipped.
        protected virtual void OnShowing() { }

        protected virtual void OnClosed() { }

        private void HandleCloseFinished()
        {
            gameObject.SetActive(false);
            NotifyClosed();
        }

        private void NotifyClosed() => OnClosed();

        // Canvas scaler matches width, so a tall popup overflows a low-aspect screen.
        private float FitScale()
        {
            if (_fitPadding <= 0f)
            {
                return 1f;
            }

            float available = _rectTransform.rect.height - _fitPadding * 2f;
            float needed = ScaleTarget.rect.height;
            if (needed <= 0f || available <= 0f || needed <= available)
            {
                return 1f;
            }

            return available / needed;
        }
    }
}
