using System;
using UnityEngine;

namespace ZombieWar.UI
{
    public sealed class MenuTabBarView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        // Index matches MenuTab.
        [SerializeField] private MenuTabButtonView[] _tabs;

        private Action<int> _onTabSelected;
        private int _selected = -1;

        public int Selected => _selected;

        private void Awake()
        {
            if (_tabs == null || _tabs.Length == 0)
            {
                Debug.LogError($"{LogPrefix} MenuTabBarView has no tabs.", this);
                return;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null)
                {
                    Debug.LogError($"{LogPrefix} Empty tab slot {i}.", this);
                    continue;
                }

                int index = i;
                _tabs[i].Init(() => Select(index));
            }
        }

        public void Init(Action<int> onTabSelected) => _onTabSelected = onTabSelected;

        // Selecting the current tab again is a no-op, so the page does not flicker or replay a tap.
        public void Select(int tab)
        {
            if (tab == _selected || tab < 0 || tab >= _tabs.Length || _tabs[tab] == null || _tabs[tab].IsLocked)
            {
                return;
            }

            if (_selected >= 0)
            {
                _tabs[_selected].SetSelected(false);
            }

            _selected = tab;
            _tabs[tab].SetSelected(true);
            _onTabSelected?.Invoke(tab);
        }
    }
}
