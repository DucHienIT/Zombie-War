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
        // Chapter order on the battle page.
        [SerializeField] private LevelDefinitionSO[] _levels;

        private readonly SaveService _save = new SaveService();
        private LevelCardData[] _cards;
        private WeaponEntryData[] _weapons;
        private bool _menuShown;

        private void Awake()
        {
            bool missing = _ui == null || _flow == null || _profile == null || _cameraShake == null
                           || _levels == null || _levels.Length == 0;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} MenuUiBinder has an unassigned reference - no level would be selectable.", this);
                return;
            }

            _cards = new LevelCardData[_levels.Length];
            _weapons = new WeaponEntryData[_profile.Guns.Length];
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
            _menuShown = true;
            _ui.ShowMenuScreen(BuildHeader(), _cards, _weapons, HandleLevelSelected, HandleUpgradeRequested, HandleSettingsRequested);
        }

        // Rewards land at the end of a run while the menu is hidden; only a live menu redraws.
        private void HandleProfileChanged()
        {
            if (!_menuShown)
            {
                return;
            }

            RefreshWeapons();
            _ui.RefreshMenu(BuildHeader(), _weapons);
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
            return new WeaponStatsData(stats.Damage, stats.ShotsPerSecond, stats.MagazineSize, stats.ReloadDuration);
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
            _ui.ShowSettingsPopup(_cameraShake.IsEnabled, _cameraShake.SetEnabled);
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

            _flow.StartRun(level);
        }
    }
}
