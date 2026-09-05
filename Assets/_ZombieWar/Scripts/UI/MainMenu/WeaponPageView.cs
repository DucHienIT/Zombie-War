using System;
using UnityEngine;

namespace ZombieWar.UI
{
    // Grid of weapon tiles. Tapping a tile only reports its index; the detail sheet is a popup
    // owned by UIManager. Slots are authored; data fills them.
    public sealed class WeaponPageView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private WeaponCardView[] _cards;

        private WeaponEntryData[] _entries;
        private Action<int> _onWeaponSelected;
        private Action _onTap;

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

        public void Bind(WeaponEntryData[] entries, Action<int> onWeaponSelected, Action onTap)
        {
            if (entries == null || entries.Length == 0)
            {
                Debug.LogError($"{LogPrefix} WeaponPageView received no weapons.", this);
                return;
            }

            if (entries.Length > _cards.Length)
            {
                Debug.LogError($"{LogPrefix} {entries.Length} weapons but only {_cards.Length} authored slots - rebuild the UI root.", this);
            }

            _onWeaponSelected = onWeaponSelected;
            _onTap = onTap;
            Refresh(entries);
        }

        public void Refresh(WeaponEntryData[] entries)
        {
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
                if (active)
                {
                    card.Bind(entries[i]);
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
