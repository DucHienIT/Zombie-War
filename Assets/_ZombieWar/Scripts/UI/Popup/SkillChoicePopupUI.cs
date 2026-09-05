using System;
using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    public sealed class SkillChoicePopupUI : PopupBase
    {
        private const string LogPrefix = "[Popup]";
        private const int NoPick = -1;

        [SerializeField] private SkillCardView[] _cards;
        [SerializeField] private TMP_Text _levelText;

        private Action<int> _onPicked;
        private Action[] _cardCallbacks;
        private SkillCardData[] _offers;
        private int _offerCount;
        private int _battleLevel;
        private int _pickedIndex = NoPick;

        protected override void Awake()
        {
            base.Awake();
            if (_cards == null || _cards.Length == 0 || _levelText == null)
            {
                Debug.LogError($"{LogPrefix} SkillChoicePopupUI has no cards to draw.", this);
                return;
            }

            // One closure per slot, built once: the cards are re-bound on every level-up.
            _cardCallbacks = new Action[_cards.Length];
            for (int i = 0; i < _cards.Length; i++)
            {
                int index = i;
                _cardCallbacks[i] = () => HandleCardChosen(index);
            }
        }

        // Only takes the data. The popup sits inactive between level-ups, so at this point its
        // own Awake has not run yet - drawing here would read fields that do not exist.
        public void Setup(SkillCardData[] cards, int count, int battleLevel, Action<int> onPicked)
        {
            _offers = cards;
            _offerCount = count;
            _battleLevel = battleLevel;
            _onPicked = onPicked;
            _pickedIndex = NoPick;
        }

        protected override void OnShowing()
        {
            _levelText.SetText("{0}", _battleLevel);

            if (_offerCount > _cards.Length)
            {
                Debug.LogError($"{LogPrefix} {_offerCount} offers but only {_cards.Length} authored cards - rebuild the UI root.", this);
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                bool used = i < _offerCount;
                _cards[i].gameObject.SetActive(used);
                if (used)
                {
                    _cards[i].Bind(_offers[i], _cardCallbacks[i]);
                }
            }
        }

        private void HandleCardChosen(int index)
        {
            if (_pickedIndex != NoPick)
            {
                return;
            }

            _pickedIndex = index;
            Close();
        }

        // Deferred to the close so the next level-up can open on a popup that has finished
        // its own animation, the same contract the pause and result panels follow.
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
