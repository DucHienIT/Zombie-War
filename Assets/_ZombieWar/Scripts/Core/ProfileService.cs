using System;
using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Core
{
    // Runtime copy of the player's persistent progress: level, XP, coins and per-gun upgrades.
    // Static numbers stay in ProgressionRulesSO and GunDefinitionSO; this only holds state.
    public sealed class ProfileService : MonoBehaviour
    {
        private const string LogPrefix = "[Profile]";

        [SerializeField] private ProgressionRulesSO _rules;
        // Every gun the menu can show, in display order.
        [SerializeField] private GunDefinitionSO[] _guns;

        private readonly SaveService _save = new SaveService();
        private int[] _gunLevels;
        private bool[] _gunUnlocked;
        private bool _loaded;

        public event Action OnChanged;

        public GunDefinitionSO[] Guns => _guns;
        public int Level { get; private set; }
        public int Xp { get; private set; }
        public int Coins { get; private set; }
        public int XpToNextLevel => _rules.XpToLevelUp(Level);
        public bool IsMaxLevel => Level >= _rules.MaxLevel;

        private void Awake()
        {
            if (_rules == null || _guns == null || _guns.Length == 0)
            {
                Debug.LogError($"{LogPrefix} ProfileService has an unassigned reference.", this);
                return;
            }

            EnsureLoaded();
        }

        public int GetGunLevel(GunDefinitionSO gun)
        {
            EnsureLoaded();
            int index = IndexOf(gun);
            return index < 0 ? 0 : _gunLevels[index];
        }

        public bool IsGunUnlocked(GunDefinitionSO gun)
        {
            EnsureLoaded();
            int index = IndexOf(gun);
            return index >= 0 && _gunUnlocked[index];
        }

        public int GetUpgradeCost(GunDefinitionSO gun) => gun.GetUpgradeCost(GetGunLevel(gun));

        public bool CanUpgrade(GunDefinitionSO gun)
        {
            int level = GetGunLevel(gun);
            return IsGunUnlocked(gun) && level < gun.MaxUpgradeLevel && Coins >= gun.GetUpgradeCost(level);
        }

        public bool TryUpgradeGun(GunDefinitionSO gun)
        {
            EnsureLoaded();
            int index = IndexOf(gun);
            if (index < 0 || !CanUpgrade(gun))
            {
                return false;
            }

            Coins -= gun.GetUpgradeCost(_gunLevels[index]);
            _gunLevels[index]++;
            _save.SetGunLevel(gun.Id, _gunLevels[index]);
            _save.SetProgress(Level, Xp, Coins);
            OnChanged?.Invoke();
            return true;
        }

        // Banks a finished run and reports what it paid so the result panel can show it.
        public void GrantRunRewards(int kills, int totalScore, bool won, out int coinsEarned, out int xpEarned)
        {
            EnsureLoaded();
            coinsEarned = _rules.CoinsForRun(totalScore, won);
            xpEarned = _rules.XpForRun(kills, won);
            Coins += coinsEarned;
            Xp += xpEarned;

            while (!IsMaxLevel && Xp >= XpToNextLevel)
            {
                Xp -= XpToNextLevel;
                Level++;
            }

            if (IsMaxLevel)
            {
                Xp = Mathf.Min(Xp, XpToNextLevel);
            }

            _save.SetProgress(Level, Xp, Coins);
            OnChanged?.Invoke();
        }

        // Other systems may read the profile from their own Awake, before this one has run.
        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            Level = Mathf.Clamp(_save.PlayerLevel, 1, _rules.MaxLevel);
            Xp = _save.PlayerXp;
            Coins = _save.GetCoins(_rules.StartingCoins);
            _gunLevels = new int[_guns.Length];
            _gunUnlocked = new bool[_guns.Length];
            for (int i = 0; i < _guns.Length; i++)
            {
                GunDefinitionSO gun = _guns[i];
                if (gun == null)
                {
                    Debug.LogError($"{LogPrefix} Empty gun slot {i} on ProfileService.", this);
                    continue;
                }

                _gunLevels[i] = Mathf.Clamp(_save.GetGunLevel(gun.Id), 0, gun.MaxUpgradeLevel);
                _gunUnlocked[i] = _save.IsGunUnlocked(gun.Id, gun.UnlockedByDefault);
            }
        }

        private int IndexOf(GunDefinitionSO gun)
        {
            for (int i = 0; i < _guns.Length; i++)
            {
                if (_guns[i] == gun)
                {
                    return i;
                }
            }

            Debug.LogError($"{LogPrefix} {gun.name} is not in the ProfileService gun list.", this);
            return -1;
        }
    }
}
