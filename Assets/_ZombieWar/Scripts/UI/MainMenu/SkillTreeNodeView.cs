using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // One hexagon of the skill tree: tinted by its branch, a pip per rank, a lock while its
    // parent is unowned, and the link that ties it to that parent. Every colour it shows is
    // either authored here or the accent the data carries; the code only picks between them.
    public sealed class SkillTreeNodeView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _button;
        // Everything that scales together on select, reveal and punch.
        [SerializeField] private RectTransform _body;
        [SerializeField] private Image _hex;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _glow;
        [SerializeField] private Image _burst;
        [SerializeField] private Image _ring;
        [SerializeField] private GameObject _lock;
        [SerializeField] private Image[] _pips;
        [SerializeField] private GameObject _maxTag;
        // The line from the parent node; empty on a root.
        [SerializeField] private Image _link;

        [Header("Colors")]
        [SerializeField] private Color _lockedHexColor;
        [SerializeField] private Color _lockedIconColor;
        [SerializeField] private Color _unlockedIconColor;
        [SerializeField] private Color _emptyPipColor;
        [SerializeField] private Color _lockedLinkColor;

        [Header("Pips")]
        [SerializeField] private float _pipSpacing = 20f;

        [Header("Glow")]
        [SerializeField] private float _glowMinAlpha = 0.18f;
        [SerializeField] private float _glowMaxAlpha = 0.55f;
        [SerializeField] private float _glowPulseDuration = 0.9f;

        [Header("Selection")]
        [SerializeField] private float _selectedScale = 1.1f;
        [SerializeField] private float _selectDuration = 0.25f;
        [SerializeField] private Ease _selectEase = Ease.OutBack;

        [Header("Feedback")]
        [SerializeField] private float _upgradePunchScale = 0.25f;
        [SerializeField] private float _upgradeDuration = 0.45f;
        [SerializeField] private float _burstScale = 1.8f;
        [SerializeField] private float _burstAlpha = 0.9f;
        [SerializeField] private float _burstDuration = 0.5f;
        [SerializeField] private float _pipPopScale = 1.8f;
        [SerializeField] private float _pipPopDuration = 0.35f;
        [SerializeField] private float _colorTweenDuration = 0.35f;
        [SerializeField] private float _deniedShakeStrength = 12f;
        [SerializeField] private float _deniedShakeDuration = 0.3f;
        [SerializeField] private float _revealDuration = 0.4f;
        [SerializeField] private Ease _revealEase = Ease.OutBack;

        private Action _onClicked;
        private Vector3 _restScale;
        private bool _selected;
        // The page binds and selects while it is still inactive, before this Awake has read the
        // authored rest scale; until then only flags are stored and the scale is left alone.
        private bool _awake;

        private void Awake()
        {
            bool missing = _button == null || _body == null || _hex == null || _icon == null || _glow == null || _burst == null
                           || _ring == null || _lock == null || _pips == null || _pips.Length == 0 || _maxTag == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} SkillTreeNodeView has an unassigned reference.", this);
                return;
            }

            _restScale = _body.localScale;
            _awake = true;
            _body.localScale = SelectionScale();
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (!_awake)
            {
                return;
            }

            KillTweens();
            _body.localScale = SelectionScale();
            _body.anchoredPosition = Vector2.zero;
            _burst.enabled = false;
            if (_link != null)
            {
                _link.rectTransform.localScale = Vector3.one;
            }
        }

        public void Init(Action onClicked) => _onClicked = onClicked;

        public void Bind(in SkillNodeData data)
        {
            _icon.sprite = data.Icon;
            _icon.color = data.Unlocked ? _unlockedIconColor : _lockedIconColor;
            _hex.color = data.Unlocked ? data.Accent : _lockedHexColor;
            _lock.SetActive(!data.Unlocked);
            _maxTag.SetActive(data.IsMaxed);
            _ring.color = data.Accent;
            if (_link != null)
            {
                _link.color = data.Unlocked ? data.Accent : _lockedLinkColor;
            }

            DrawPips(data);
            SetGlow(data);
        }

        public void SetSelected(bool selected, bool animate)
        {
            _selected = selected;
            if (!_awake)
            {
                _ring.enabled = selected;
                return;
            }

            _ring.DOKill();
            _body.DOKill();
            Vector3 scale = SelectionScale();
            if (!animate)
            {
                _ring.enabled = selected;
                SetAlpha(_ring, 1f);
                _body.localScale = scale;
                return;
            }

            _ring.enabled = true;
            SetAlpha(_ring, selected ? 0f : 1f);
            _ring.DOFade(selected ? 1f : 0f, _selectDuration).SetUpdate(true).SetLink(gameObject)
                .OnComplete(HideRingIfDeselected);
            _body.DOScale(scale, _selectDuration).SetEase(_selectEase).SetUpdate(true).SetLink(gameObject);
        }

        // A rank just landed on this node: the new pip pops and light bursts out of the hexagon.
        public void PlayUpgrade(in SkillNodeData data)
        {
            Bind(data);
            if (!_awake)
            {
                return;
            }

            _body.DOKill();
            _body.localScale = SelectionScale();
            _body.DOPunchScale(Vector3.one * _upgradePunchScale, _upgradeDuration, 6, 0.6f).SetUpdate(true).SetLink(gameObject);
            PlayBurst(data.Accent);

            int newest = data.Rank - 1;
            if (newest >= 0 && newest < _pips.Length)
            {
                RectTransform pip = _pips[newest].rectTransform;
                pip.DOKill();
                pip.localScale = Vector3.one * _pipPopScale;
                pip.DOScale(Vector3.one, _pipPopDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
        }

        // The parent just got its first rank: fade this node from locked to its branch colour.
        public void PlayUnlocked(in SkillNodeData data)
        {
            Bind(data);
            if (!_awake)
            {
                return;
            }

            _hex.DOKill();
            _icon.DOKill();
            _hex.color = _lockedHexColor;
            _icon.color = _lockedIconColor;
            _hex.DOColor(data.Accent, _colorTweenDuration).SetUpdate(true).SetLink(gameObject);
            _icon.DOColor(_unlockedIconColor, _colorTweenDuration).SetUpdate(true).SetLink(gameObject);
            if (_link != null)
            {
                _link.DOKill();
                _link.color = _lockedLinkColor;
                _link.DOColor(data.Accent, _colorTweenDuration).SetUpdate(true).SetLink(gameObject);
            }

            _body.DOKill();
            _body.localScale = SelectionScale();
            _body.DOPunchScale(Vector3.one * _upgradePunchScale * 0.5f, _upgradeDuration, 4, 0.6f).SetUpdate(true).SetLink(gameObject);
        }

        public void PlayDenied()
        {
            if (!_awake)
            {
                return;
            }

            _body.DOKill();
            _body.anchoredPosition = Vector2.zero;
            _body.DOShakeAnchorPos(_deniedShakeDuration, _deniedShakeStrength, 14, 90f, false, true).SetUpdate(true).SetLink(gameObject);
        }

        // The tree grows in from the root: the link draws first, then the hexagon pops.
        public void PlayReveal(float delay)
        {
            if (!_awake)
            {
                return;
            }

            _body.DOKill();
            Vector3 target = SelectionScale();
            _body.localScale = Vector3.zero;
            _body.DOScale(target, _revealDuration).SetEase(_revealEase).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
            if (_link == null)
            {
                return;
            }

            RectTransform link = _link.rectTransform;
            link.DOKill();
            link.localScale = new Vector3(0f, 1f, 1f);
            link.DOScaleX(1f, _revealDuration * 0.6f).SetEase(Ease.OutQuad).SetDelay(Mathf.Max(0f, delay - _revealDuration * 0.3f))
                .SetUpdate(true).SetLink(gameObject);
        }

        private void DrawPips(in SkillNodeData data)
        {
            int shown = Mathf.Min(data.MaxRank, _pips.Length);
            if (data.MaxRank > _pips.Length)
            {
                Debug.LogError($"{LogPrefix} {data.DisplayName} has {data.MaxRank} ranks but only {_pips.Length} pips are authored.", this);
            }

            float first = -(shown - 1) * 0.5f * _pipSpacing;
            for (int i = 0; i < _pips.Length; i++)
            {
                Image pip = _pips[i];
                bool active = i < shown;
                pip.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                pip.color = i < data.Rank ? data.Accent : _emptyPipColor;
                Vector2 position = pip.rectTransform.anchoredPosition;
                position.x = first + i * _pipSpacing;
                pip.rectTransform.anchoredPosition = position;
            }
        }

        // Only a node the player can buy right now breathes, so the glow reads as "tap me".
        private void SetGlow(in SkillNodeData data)
        {
            _glow.DOKill();
            if (!data.CanBuy)
            {
                _glow.enabled = false;
                return;
            }

            _glow.enabled = true;
            Color color = data.Accent;
            color.a = _glowMinAlpha;
            _glow.color = color;
            _glow.DOFade(_glowMaxAlpha, _glowPulseDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true).SetLink(gameObject);
        }

        private void PlayBurst(Color accent)
        {
            RectTransform burst = _burst.rectTransform;
            _burst.DOKill();
            burst.DOKill();
            _burst.enabled = true;
            accent.a = _burstAlpha;
            _burst.color = accent;
            burst.localScale = Vector3.one;
            burst.DOScale(_burstScale, _burstDuration).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
            _burst.DOFade(0f, _burstDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject).OnComplete(HideBurst);
        }

        private Vector3 SelectionScale() => _selected ? _restScale * _selectedScale : _restScale;

        private void HideBurst() => _burst.enabled = false;

        private void HideRingIfDeselected()
        {
            if (!_selected)
            {
                _ring.enabled = false;
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private void KillTweens()
        {
            _body.DOKill();
            _hex.DOKill();
            _icon.DOKill();
            _glow.DOKill();
            _burst.DOKill();
            _burst.rectTransform.DOKill();
            _ring.DOKill();
            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i].rectTransform.DOKill();
            }

            if (_link != null)
            {
                _link.DOKill();
                _link.rectTransform.DOKill();
            }
        }

        private void HandleClicked() => _onClicked?.Invoke();
    }
}
