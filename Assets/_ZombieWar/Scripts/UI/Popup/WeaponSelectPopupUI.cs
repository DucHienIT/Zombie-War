using System;
using UnityEngine;

namespace ZombieWar.UI
{
    // Shown right after a level is picked from the Battle page: a run has no in-match weapon
    // switching, so the gun for the whole run is locked in here before the countdown starts.
    // Reuses WeaponCardView as-is (icon, name, level, locked "COMING SOON" state) - tapping a
    // card picks it immediately instead of opening the upgrade sheet the weapon page shows.
    public sealed class WeaponSelectPopupUI : PopupBase
    {
        private const string LogPrefix = "[Popup]";
        private const int NoPick = -1;

        [SerializeField] private WeaponCardView[] _cards;

        private Action<int> _onPicked;
        private Action[] _cardCallbacks;
        private WeaponEntryData[] _weapons;
        private int _pickedIndex = NoPick;

        protected override void Awake()
        {
            base.Awake();
            if (_cards == null || _cards.Length == 0)
            {
                Debug.LogError($"{LogPrefix} WeaponSelectPopupUI has no cards to draw.", this);
                return;
            }

            // One closure per slot, built once: the cards are re-bound every time a level is picked.
            _cardCallbacks = new Action[_cards.Length];
            for (int i = 0; i < _cards.Length; i++)
            {
                int index = i;
                _cardCallbacks[i] = () => HandleCardChosen(index);
                _cards[i].Init(_cardCallbacks[i]);
            }
        }

        // Only takes the data. The popup sits inactive between runs, so at this point its own
        // Awake has not run yet - drawing here would read fields that do not exist.
        public void Setup(WeaponEntryData[] weapons, Action<int> onPicked)
        {
            _weapons = weapons;
            _onPicked = onPicked;
            _pickedIndex = NoPick;
        }

        protected override void OnShowing()
        {
            if (_weapons.Length > _cards.Length)
            {
                Debug.LogError($"{LogPrefix} {_weapons.Length} weapons but only {_cards.Length} authored slots - duplicate a card slot in UIRoot.prefab and wire it into _cards.", this);
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                bool used = i < _weapons.Length;
                _cards[i].gameObject.SetActive(used);
                if (used)
                {
                    _cards[i].Bind(_weapons[i]);
                }
            }
        }

        // A locked gun ignores the tap instead of closing the popup on an invalid pick.
        private void HandleCardChosen(int index)
        {
            if (_pickedIndex != NoPick || !_weapons[index].Unlocked)
            {
                return;
            }

            _pickedIndex = index;
            Close();
        }

        // Deferred to the close so StartRun only fires once this popup has finished animating out.
        protected override void OnClosed()
        {
            int picked = _pickedIndex;
            Action<int> callback = _onPicked;
            _pickedIndex = NoPick;
            _onPicked = null;
            callback?.Invoke(picked);
        }
    }
}
