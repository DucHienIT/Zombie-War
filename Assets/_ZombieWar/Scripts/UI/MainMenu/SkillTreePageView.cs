using System;
using DG.Tweening;
using UnityEngine;

namespace ZombieWar.UI
{
    // The permanent skill tree: authored hexagon slots in tree order, one detail sheet for the
    // selected node. Tapping a node only selects it; the sheet's button is what buys a rank.
    public sealed class SkillTreePageView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        // Slot order is the node order of the skill tree asset.
        [SerializeField] private SkillTreeNodeView[] _nodes;
        [SerializeField] private SkillDetailPanelView _detail;
        // Everything below the title, scaled down as one block when the screen is too short.
        [SerializeField] private RectTransform _content;
        [SerializeField] private CanvasGroup _detailGroup;

        [Header("Reveal")]
        [SerializeField] private float _revealStagger = 0.06f;
        [SerializeField] private float _detailRevealDelay = 0.3f;
        [SerializeField] private float _detailRevealDuration = 0.3f;
        [SerializeField] private float _detailRevealSlide = 40f;
        [SerializeField] private Ease _detailRevealEase = Ease.OutCubic;

        private SkillNodeData[] _data;
        private Action<int> _onUpgrade;
        private Action _onTap;
        private Action _upgradeAction;
        private Tween _revealCall;
        private int _selected = -1;

        private void Awake()
        {
            bool missing = _nodes == null || _nodes.Length == 0 || _detail == null || _content == null || _detailGroup == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} SkillTreePageView has an unassigned reference.", this);
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] == null)
                {
                    Debug.LogError($"{LogPrefix} Empty skill node slot {i}.", this);
                    continue;
                }

                int index = i;
                _nodes[i].Init(() => HandleNodeClicked(index));
            }

            _upgradeAction = HandleUpgradeClicked;
            _detail.Init(_upgradeAction);
        }

        // The page is authored inactive and switched on by the tab bar, so every visit replays
        // the tree growing in from its root. The reveal waits one tick: on the activation that
        // triggers this OnEnable, the node views below have not run their Awake yet.
        private void OnEnable()
        {
            FitToPage();
            if (_data != null)
            {
                _revealCall = DOVirtual.DelayedCall(0f, PlayReveal).SetUpdate(true).SetLink(gameObject);
            }
        }

        private void OnDisable()
        {
            _revealCall?.Kill();
            _revealCall = null;
            _detailGroup.DOKill();
            ((RectTransform)_detailGroup.transform).DOKill();
        }

        public void Bind(SkillNodeData[] nodes, Action<int> onUpgrade, Action onTap)
        {
            if (nodes == null || nodes.Length == 0)
            {
                Debug.LogError($"{LogPrefix} SkillTreePageView received no nodes.", this);
                return;
            }

            if (nodes.Length != _nodes.Length)
            {
                Debug.LogError($"{LogPrefix} {nodes.Length} skill nodes but {_nodes.Length} authored slots - the tree asset and the page disagree.", this);
            }

            _onUpgrade = onUpgrade;
            _onTap = onTap;
            _data = nodes;
            int shown = Mathf.Min(nodes.Length, _nodes.Length);
            for (int i = 0; i < shown; i++)
            {
                _nodes[i].Bind(nodes[i]);
            }

            Select(DefaultSelection(), false);
        }

        // Fresh data after a purchase (or after coins changed): only the nodes whose state moved animate.
        public void Refresh(SkillNodeData[] nodes)
        {
            if (_data == null)
            {
                return;
            }

            int shown = Mathf.Min(nodes.Length, _nodes.Length);
            bool selectedRanked = false;
            for (int i = 0; i < shown; i++)
            {
                SkillNodeData previous = _data[i];
                SkillNodeData next = nodes[i];
                if (next.Rank > previous.Rank)
                {
                    _nodes[i].PlayUpgrade(next);
                    selectedRanked |= i == _selected;
                }
                else if (!previous.Unlocked && next.Unlocked)
                {
                    _nodes[i].PlayUnlocked(next);
                }
                else
                {
                    _nodes[i].Bind(next);
                }
            }

            _data = nodes;
            if (_selected < 0 || _selected >= shown)
            {
                return;
            }

            if (selectedRanked)
            {
                _detail.Refresh(_data[_selected]);
            }
            else
            {
                _detail.Show(_data[_selected], false);
            }
        }

        // Land on the cheapest thing to do next: the first open node that still has ranks.
        private int DefaultSelection()
        {
            int shown = Mathf.Min(_data.Length, _nodes.Length);
            for (int i = 0; i < shown; i++)
            {
                if (_data[i].Unlocked && !_data[i].IsMaxed)
                {
                    return i;
                }
            }

            return 0;
        }

        private void Select(int index, bool animate)
        {
            if (_selected >= 0 && _selected < _nodes.Length && _selected != index)
            {
                _nodes[_selected].SetSelected(false, animate);
            }

            _selected = index;
            _nodes[index].SetSelected(true, animate);
            _detail.Show(_data[index], animate);
        }

        private void HandleNodeClicked(int index)
        {
            if (_data == null || index >= _data.Length)
            {
                return;
            }

            _onTap?.Invoke();
            if (!_data[index].Unlocked)
            {
                _nodes[index].PlayDenied();
            }

            if (index != _selected)
            {
                Select(index, true);
            }
        }

        private void HandleUpgradeClicked()
        {
            if (_selected < 0 || _data == null)
            {
                return;
            }

            _onUpgrade?.Invoke(_selected);
        }

        private void PlayReveal()
        {
            _revealCall = null;
            if (!isActiveAndEnabled || _data == null)
            {
                return;
            }

            int shown = Mathf.Min(_data.Length, _nodes.Length);
            for (int i = 0; i < shown; i++)
            {
                _nodes[i].PlayReveal(i * _revealStagger);
            }

            var detailRect = (RectTransform)_detailGroup.transform;
            _detailGroup.DOKill();
            detailRect.DOKill();
            _detailGroup.alpha = 0f;
            Vector2 rest = detailRect.anchoredPosition;
            detailRect.anchoredPosition = rest - new Vector2(0f, _detailRevealSlide);
            _detailGroup.DOFade(1f, _detailRevealDuration).SetDelay(_detailRevealDelay).SetUpdate(true).SetLink(gameObject);
            detailRect.DOAnchorPos(rest, _detailRevealDuration).SetEase(_detailRevealEase).SetDelay(_detailRevealDelay)
                .SetUpdate(true).SetLink(gameObject);
        }

        // The canvas matches width, so a low-aspect screen has less height than the tree was
        // laid out for; shrinking the whole block keeps the sheet clear of the tab bar.
        private void FitToPage()
        {
            float available = ((RectTransform)transform).rect.height;
            // The block hangs from the page top, so the title strip above it counts too.
            float needed = _content.rect.height - _content.anchoredPosition.y;
            float scale = needed > 0f && available > 0f && needed > available ? available / needed : 1f;
            _content.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
