using System;
using UnityEngine;

namespace ZombieWar.UI
{
    public sealed class MenuScreenView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Canvas _canvas;
        // One authored slot per level; extra data is an error, not a silent truncation.
        [SerializeField] private LevelCardView[] _cards;

        private void Awake()
        {
            if (_canvas == null || _cards == null || _cards.Length == 0)
            {
                Debug.LogError($"{LogPrefix} MenuScreenView has an unassigned reference.", this);
            }
        }

        public void SetVisible(bool visible) => _canvas.enabled = visible;

        public void Bind(LevelCardData[] cards, Action<int> onSelected, Action onTap)
        {
            if (cards == null)
            {
                Debug.LogError($"{LogPrefix} MenuScreenView received no level data.", this);
                return;
            }

            if (cards.Length > _cards.Length)
            {
                Debug.LogError($"{LogPrefix} {cards.Length} levels but only {_cards.Length} authored cards - rebuild the UI root.", this);
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                LevelCardView card = _cards[i];
                if (card == null)
                {
                    Debug.LogError($"{LogPrefix} Empty level card slot {i}.", this);
                    continue;
                }

                if (i >= cards.Length)
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                int slot = i;
                card.gameObject.SetActive(true);
                card.Bind(cards[i], () =>
                {
                    onTap?.Invoke();
                    onSelected?.Invoke(slot);
                });
            }
        }
    }
}
