using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // One chapter card at a time with arrows to step through the level list.
    public sealed class BattlePageView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private LevelCardView _card;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _chapterText;

        private LevelCardData[] _levels;
        private Action<int> _onLevelSelected;
        private Action _onTap;
        private Action _playAction;
        private int _index;

        private void Awake()
        {
            if (_card == null || _prevButton == null || _nextButton == null || _chapterText == null)
            {
                Debug.LogError($"{LogPrefix} BattlePageView has an unassigned reference.", this);
                return;
            }

            _playAction = HandlePlay;
            _prevButton.onClick.AddListener(HandlePrev);
            _nextButton.onClick.AddListener(HandleNext);
        }

        public void Bind(LevelCardData[] levels, Action<int> onLevelSelected, Action onTap)
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError($"{LogPrefix} BattlePageView received no levels.", this);
                return;
            }

            _levels = levels;
            _onLevelSelected = onLevelSelected;
            _onTap = onTap;
            _index = LastUnlockedIndex();
            Refresh();
        }

        // Land on the furthest chapter the player can enter, which is the one they most likely want.
        private int LastUnlockedIndex()
        {
            int last = 0;
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i].Unlocked)
                {
                    last = i;
                }
            }

            return last;
        }

        private void Refresh()
        {
            _card.Bind(_levels[_index], _playAction);
            _chapterText.SetText("{0} / {1}", _index + 1, _levels.Length);
            _prevButton.interactable = _index > 0;
            _nextButton.interactable = _index < _levels.Length - 1;
        }

        private void HandlePrev()
        {
            if (_index <= 0)
            {
                return;
            }

            _onTap?.Invoke();
            _index--;
            Refresh();
        }

        private void HandleNext()
        {
            if (_index >= _levels.Length - 1)
            {
                return;
            }

            _onTap?.Invoke();
            _index++;
            Refresh();
        }

        private void HandlePlay()
        {
            _onTap?.Invoke();
            _onLevelSelected?.Invoke(_index);
        }
    }
}
