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

        // A step to the next chapter brings the card in from the right, a step back from the left.
        private const int StepForward = 1;
        private const int StepBack = -1;
        private const int NoStep = 0;

        [SerializeField] private LevelCardView _card;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _chapterText;
        // Seven quick taps on the HARD label unlock the chapter on screen; it ships in the build on purpose.
        [SerializeField] private SecretTapTrigger _hardTapCheat;

        private LevelCardData[] _levels;
        private Action<int> _onLevelSelected;
        private Action<int> _onUnlockCheat;
        private Action _onTap;
        private Action _playAction;
        private int _index;

        private void Awake()
        {
            if (_card == null || _prevButton == null || _nextButton == null || _chapterText == null || _hardTapCheat == null)
            {
                Debug.LogError($"{LogPrefix} BattlePageView has an unassigned reference.", this);
                return;
            }

            _playAction = HandlePlay;
            _prevButton.onClick.AddListener(HandlePrev);
            _nextButton.onClick.AddListener(HandleNext);
            _hardTapCheat.Init(HandleUnlockCheat);
        }

        // The page is switched on by the tab bar, so every visit replays the card coming in.
        private void OnEnable()
        {
            if (_levels != null)
            {
                _card.PlayEnter(NoStep);
            }
        }

        public void Bind(LevelCardData[] levels, Action<int> onLevelSelected, Action<int> onUnlockCheat, Action onTap)
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError($"{LogPrefix} BattlePageView received no levels.", this);
                return;
            }

            _levels = levels;
            _onLevelSelected = onLevelSelected;
            _onUnlockCheat = onUnlockCheat;
            _onTap = onTap;
            _index = LastUnlockedIndex();
            Refresh(NoStep);
        }

        // Redraws the chapter in view with fresh data (after the unlock cheat) without moving off it.
        public void Refresh(LevelCardData[] levels)
        {
            if (_levels == null || levels == null || levels.Length == 0)
            {
                return;
            }

            _levels = levels;
            _index = Mathf.Min(_index, _levels.Length - 1);
            Refresh(NoStep);
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

        private void Refresh(int step)
        {
            _card.Bind(_levels[_index], _playAction);
            _chapterText.SetText("{0} / {1}", _index + 1, _levels.Length);
            _prevButton.interactable = _index > 0;
            _nextButton.interactable = _index < _levels.Length - 1;
            _prevButton.gameObject.SetActive(_prevButton.interactable);
            _nextButton.gameObject.SetActive(_nextButton.interactable);
            _card.PlayEnter(step);
        }

        private void HandlePrev()
        {
            if (_index <= 0)
            {
                return;
            }

            _onTap?.Invoke();
            _index--;
            Refresh(StepBack);
        }

        private void HandleNext()
        {
            if (_index >= _levels.Length - 1)
            {
                return;
            }

            _onTap?.Invoke();
            _index++;
            Refresh(StepForward);
        }

        private void HandlePlay()
        {
            _onTap?.Invoke();
            _onLevelSelected?.Invoke(_index);
        }

        private void HandleUnlockCheat()
        {
            if (_levels == null || _levels[_index].Unlocked)
            {
                return;
            }

            _onTap?.Invoke();
            _onUnlockCheat?.Invoke(_index);
        }
    }
}
