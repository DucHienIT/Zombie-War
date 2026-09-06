using UnityEngine;
using ZombieWar.Data;
using ZombieWar.UI;

namespace ZombieWar.Core
{
    // Menu-side counterpart of GameplayUiBinder. The menu is an overlay in the one scene the
    // game ships with, so this only reacts to the flow sitting in its Menu state.
    public sealed class MenuUiBinder : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private UIManager _ui;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ProfileService _profile;
        [SerializeField] private CameraShakeController _cameraShake;
        [SerializeField] private SettingsService _settings;
        // Chapter order on the battle page.
        [SerializeField] private LevelDefinitionSO[] _levels;

        private readonly SaveService _save = new SaveService();
        private LevelCardData[] _cards;
        private WeaponEntryData[] _weapons;
        private SkillNodeData[] _skills;
        private bool _menuShown;
        private LevelDefinitionSO _pendingLevel;

        private void Awake()
        {
            bool missing = _ui == null || _flow == null || _profile == null || _cameraShake == null || _settings == null
                           || _levels == null || _levels.Length == 0;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} MenuUiBinder has an unassigned reference - no level would be selectable.", this);
                return;
            }

            _cards = new LevelCardData[_levels.Length];
            _weapons = new WeaponEntryData[_profile.Guns.Length];
            _skills = new SkillNodeData[_profile.Skills.Count];
        }

        private void OnEnable()
        {
            _flow.OnStateChanged += HandleStateChanged;
            _profile.OnChanged += HandleProfileChanged;
        }

        private void OnDisable()
        {
            _flow.OnStateChanged -= HandleStateChanged;
            _profile.OnChanged -= HandleProfileChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Menu)
            {
                _menuShown = false;
                return;
            }

            RefreshCards();
            RefreshWeapons();
            RefreshSkills();
            _menuShown = true;
            _ui.ShowMenuScreen(BuildHeader(), _cards, _weapons, _skills, HandleLevelSelected, HandleUpgradeRequested,
                HandleSkillUpgradeRequested, HandleSettingsRequested);
        }

        // Rewards land at the end of a run while the menu is hidden; only a live menu redraws.
        private void HandleProfileChanged()
        {
            if (!_menuShown)
            {
                return;
            }

            RefreshWeapons();
            RefreshSkills();
            _ui.RefreshMenu(BuildHeader(), _weapons, _skills);
        }

        // The page gets a fresh array each time so it can diff ranks against the one it holds.
        private void RefreshSkills()
        {
            SkillTreeProgress skills = _profile.Skills;
            if (_skills.Length != skills.Count)
            {
                _skills = new SkillNodeData[skills.Count];
            }

            for (int i = 0; i < skills.Count; i++)
            {
                SkillTreeNodeSO node = skills.NodeAt(i);
                if (node == null)
                {
                    continue;
                }

                int rank = skills.RankAt(i);
                int cost = skills.CostAt(i);
                SkillTreeNodeSO missing = skills.FirstMissingPrerequisite(i);
                _skills[i] = new SkillNodeData(
                    node.DisplayName,
                    node.Description,
                    node.EffectLabel,
                    node.ValueFormat,
                    node.Icon,
                    node.AccentColor,
                    rank,
                    node.MaxRank,
                    missing == null,
                    missing != null ? missing.DisplayName : string.Empty,
                    cost,
                    _profile.Coins >= cost,
                    Mathf.Max(0, cost - _profile.Coins),
                    node.EffectValueAt(rank),
                    node.EffectValueAt(Mathf.Min(rank + 1, node.MaxRank)));
            }
        }

        private void HandleSkillUpgradeRequested(int index)
        {
            // A refused purchase (locked, maxed, short on coins) is already reflected by the disabled button.
            _profile.TryUpgradeSkill(index);
        }

        private MenuHeaderData BuildHeader()
        {
            return new MenuHeaderData(_profile.Level, _profile.Xp, _profile.XpToNextLevel, _profile.Coins);
        }

        private void RefreshWeapons()
        {
            GunDefinitionSO[] guns = _profile.Guns;
            for (int i = 0; i < guns.Length; i++)
            {
                GunDefinitionSO gun = guns[i];
                if (gun == null)
                {
                    continue;
                }

                int level = _profile.GetGunLevel(gun);
                int cost = gun.GetUpgradeCost(level);
                _weapons[i] = new WeaponEntryData(
                    gun.DisplayName,
                    gun.Description,
                    gun.Icon,
                    level,
                    gun.MaxUpgradeLevel,
                    _profile.IsGunUnlocked(gun),
                    ToStatsData(gun.GetStats(level)),
                    ToStatsData(gun.GetStats(level + 1)),
                    gun.PelletCount,
                    gun.IsSpread,
                    cost,
                    _profile.CanUpgrade(gun),
                    Mathf.Max(0, cost - _profile.Coins));
            }
        }

        private static WeaponStatsData ToStatsData(in GunStats stats)
        {
            return new WeaponStatsData(stats.Damage, stats.ShotsPerSecond);
        }

        private void HandleUpgradeRequested(int slot)
        {
            GunDefinitionSO[] guns = _profile.Guns;
            if (slot < 0 || slot >= guns.Length || guns[slot] == null)
            {
                Debug.LogError($"{LogPrefix} Upgrade requested for unknown weapon slot {slot}.", this);
                return;
            }

            // A refused upgrade (not enough coins, max level) is already reflected by the disabled button.
            _profile.TryUpgradeGun(guns[slot]);
        }

        private void HandleSettingsRequested()
        {
            SettingsData data = new SettingsData(_settings.SoundEnabled, _settings.MusicEnabled, _settings.HapticsEnabled,
                _cameraShake.IsEnabled);
            _ui.ShowSettingsPopup(data, HandleSettingChanged);
        }

        // The sheet only says which row moved; which service owns that row is decided here.
        private void HandleSettingChanged(SettingId id, bool enabled)
        {
            switch (id)
            {
                case SettingId.Sound:
                    _settings.SetSoundEnabled(enabled);
                    break;
                case SettingId.Music:
                    _settings.SetMusicEnabled(enabled);
                    break;
                case SettingId.Haptics:
                    _settings.SetHapticsEnabled(enabled);
                    break;
                case SettingId.CameraShake:
                    _cameraShake.SetEnabled(enabled);
                    break;
            }
        }

        private void RefreshCards()
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                LevelDefinitionSO level = _levels[i];
                if (level == null)
                {
                    Debug.LogError($"{LogPrefix} Empty level slot {i} on MenuUiBinder.", this);
                    continue;
                }

                _cards[i] = new LevelCardData(
                    level.LevelIndex,
                    level.DisplayName,
                    level.Artwork,
                    _save.IsLevelUnlocked(level.LevelIndex),
                    _save.GetBestScore(level.LevelIndex));
            }
        }

        private void HandleLevelSelected(int slot)
        {
            LevelDefinitionSO level = _levels[slot];
            if (level == null)
            {
                Debug.LogError($"{LogPrefix} Level slot {slot} has no definition.", this);
                return;
            }

            // The run only actually starts once a gun is picked; Retry/Next Level skip this
            // screen entirely and keep whatever was equipped here.
            _pendingLevel = level;
            _ui.ShowWeaponSelectPopup(_weapons, HandleWeaponPicked);
        }

        private void HandleWeaponPicked(int index)
        {
            GunDefinitionSO[] guns = _profile.Guns;
            if (index < 0 || index >= guns.Length || guns[index] == null)
            {
                Debug.LogError($"{LogPrefix} Weapon select closed without a valid pick.", this);
                return;
            }

            _profile.SetEquippedGun(guns[index]);
            _flow.StartRun(_pendingLevel);
        }
    }
}
