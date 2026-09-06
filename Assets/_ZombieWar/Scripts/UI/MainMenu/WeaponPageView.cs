using System;
using DG.Tweening;
using UnityEngine;

namespace ZombieWar.UI
{
    // Grid of weapon tiles. Tapping a tile only reports its index; the detail sheet is a popup
    // owned by UIManager. Slots are authored; data fills them.
    public sealed class WeaponPageView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private WeaponCardView[] _cards;

        [Header("Reveal")]
        [SerializeField] private float _revealStagger = 0.07f;

        private WeaponEntryData[] _entries;
        private Action<int> _onWeaponSelected;
        private Action _onTap;
        private Tween _revealCall;

        private void Awake()
        {
            if (_cards == null || _cards.Length == 0)
            {
                Debug.LogError($"{LogPrefix} WeaponPageView has no card slots.", this);
                return;
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null)
                {
                    Debug.LogError($"{LogPrefix} Empty weapon slot {i}.", this);
                    continue;
                }

                int index = i;
                _cards[i].Init(() => HandleCardClicked(index));
            }
        }

        // The page is authored inactive and switched on by the tab bar, so every visit replays the
        // grid. The reveal waits one tick: on the activation that triggers this OnEnable, the card
        // views below have not run their Awake yet.
        private void OnEnable()
        {
            if (_entries != null)
            {
                _revealCall = DOVirtual.DelayedCall(0f, PlayReveal).SetUpdate(true).SetLink(gameObject);
            }
        }

        private void OnDisable()
        {
            _revealCall?.Kill();
            _revealCall = null;
        }

        public void Bind(WeaponEntryData[] entries, Action<int> onWeaponSelected, Action onTap)
        {
            if (entries == null || entries.Length == 0)
            {
                Debug.LogError($"{LogPrefix} WeaponPageView received no weapons.", this);
                return;
            }

            if (entries.Length > _cards.Length)
            {
                Debug.LogError($"{LogPrefix} {entries.Length} weapons but only {_cards.Length} authored slots - duplicate a card slot in UIRoot.prefab and wire it into _cards.", this);
            }

            _onWeaponSelected = onWeaponSelected;
            _onTap = onTap;
            Draw(entries, false);
        }

        // Fresh data after a purchase: only the tile whose level moved punches.
        public void Refresh(WeaponEntryData[] entries) => Draw(entries, _entries != null);

        private void Draw(WeaponEntryData[] entries, bool animateUpgrades)
        {
            WeaponEntryData[] previous = _entries;
            _entries = entries;
            int shown = Mathf.Min(entries.Length, _cards.Length);
            for (int i = 0; i < _cards.Length; i++)
            {
                WeaponCardView card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                bool active = i < shown;
                card.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                bool ranked = animateUpgrades && previous != null && i < previous.Length && entries[i].Level > previous[i].Level;
                if (ranked)
                {
                    card.PlayUpgrade(entries[i]);
                }
                else
                {
                    card.Bind(entries[i]);
                }
            }
        }

        private void PlayReveal()
        {
            _revealCall = null;
            if (!isActiveAndEnabled || _entries == null)
            {
                return;
            }

            int shown = Mathf.Min(_entries.Length, _cards.Length);
            for (int i = 0; i < shown; i++)
            {
                if (_cards[i] != null)
                {
                    _cards[i].PlayReveal(i * _revealStagger);
                }
            }
        }

        private void HandleCardClicked(int index)
        {
            if (_entries == null || index >= _entries.Length)
            {
                return;
            }

            _onTap?.Invoke();
            _onWeaponSelected?.Invoke(index);
        }
    }
}
