using System;
using UnityEngine;

namespace ZombieWar.UI
{
    // The whole main menu: shared header, one page per tab, tab bar. It only knows
    // presentation structs and the callbacks the binder hands over.
    public sealed class MenuScreenView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Canvas _canvas;
        [SerializeField] private MenuHeaderView _header;
        [SerializeField] private MenuTabBarView _tabBar;
        // Index matches MenuTab.
        [SerializeField] private GameObject[] _pages;
        [SerializeField] private BattlePageView _battlePage;
        [SerializeField] private WeaponPageView _weaponPage;
        [SerializeField] private SkillTreePageView _skillTreePage;
        [SerializeField] private int _defaultTab = (int)MenuTab.Battle;

        private Action _onTap;
        private Action _onSettings;

        private void Awake()
        {
            bool missing = _canvas == null || _header == null || _tabBar == null || _pages == null || _pages.Length == 0
                           || _battlePage == null || _weaponPage == null || _skillTreePage == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} MenuScreenView has an unassigned reference.", this);
                return;
            }

            _tabBar.Init(HandleTabSelected);
            _header.Init(HandleSettingsClicked);
        }

        public void SetVisible(bool visible) => _canvas.enabled = visible;

        public void Bind(in MenuHeaderData header, LevelCardData[] levels, WeaponEntryData[] weapons, SkillNodeData[] skills,
            Action<int> onLevelSelected, Action<int> onWeaponSelected, Action<int> onSkillUpgrade, Action onSettings, Action onTap)
        {
            _onTap = onTap;
            _onSettings = onSettings;
            _header.Set(header);
            _battlePage.Bind(levels, onLevelSelected, onTap);
            _weaponPage.Bind(weapons, onWeaponSelected, onTap);
            _skillTreePage.Bind(skills, onSkillUpgrade, onTap);
            ShowPage(_defaultTab);
            _tabBar.Select(_defaultTab);
        }

        public void Refresh(in MenuHeaderData header, WeaponEntryData[] weapons, SkillNodeData[] skills)
        {
            _header.Set(header);
            _weaponPage.Refresh(weapons);
            _skillTreePage.Refresh(skills);
        }

        private void HandleTabSelected(int tab)
        {
            _onTap?.Invoke();
            ShowPage(tab);
        }

        private void HandleSettingsClicked() => _onSettings?.Invoke();

        private void ShowPage(int tab)
        {
            for (int i = 0; i < _pages.Length; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].SetActive(i == tab);
                }
            }
        }
    }
}
