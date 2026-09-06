using System;
using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Core
{
    // Runtime copy of the player's persistent progress: level, XP, coins, per-gun upgrades and
    // skill tree ranks. Static numbers stay in the definition assets; this only holds state.
    public sealed class ProfileService : MonoBehaviour
    {
        private const string LogPrefix = "[Profile]";

        [SerializeField] private ProgressionRulesSO _rules;
        // Every gun the menu can show, in display order.
        [SerializeField] private GunDefinitionSO[] _guns;
        [SerializeField] private SkillTreeSO _skillTree;

        private readonly SaveService _save = new SaveService();
        private int[] _gunLevels;
        private bool[] _gunUnlocked;
        private int _equippedGunIndex;
        private SkillTreeProgress _skills;
        private bool _loaded;

        public event Action OnChanged;

        public GunDefinitionSO[] Guns => _guns;
        public int Level { get; private set; }
        public int Xp { get; private set; }
        public int Coins { get; private set; }
        public int XpToNextLevel => _rules.XpToLevelUp(Level);
        public bool IsMaxLevel => Level >= _rules.MaxLevel;

        public SkillTreeProgress Skills
        {
            get
            {
                EnsureLoaded();
                return _skills;
            }
        }

        // The gun a run starts with; chosen on the pre-battle weapon-select screen and kept
        // for Retry/Next Level, since those skip that screen and jump straight into Countdown.
        public GunDefinitionSO EquippedGun
        {
            get
            {
                EnsureLoaded();
                return _guns[_equippedGunIndex];
            }
        }

        private void Awake()
        {
            if (_rules == null || _guns == null || _guns.Length == 0 || _skillTree == null)
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

        // Only an unlocked gun may be equipped; the weapon-select screen never offers a locked one anyway.
        public void SetEquippedGun(GunDefinitionSO gun)
        {
            EnsureLoaded();
            int index = IndexOf(gun);
            if (index < 0 || !_gunUnlocked[index])
            {
                return;
            }

            _equippedGunIndex = index;
            _save.SetEquippedGunId(gun.Id);
        }

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

        public bool CanUpgradeSkill(int index)
        {
            EnsureLoaded();
            bool inTree = index >= 0 && index < _skills.Count;
            return inTree && _skills.IsUnlocked(index) && !_skills.IsMaxed(index) && Coins >= _skills.CostAt(index);
        }

        public bool TryUpgradeSkill(int index)
        {
            if (!CanUpgradeSkill(index))
            {
                return false;
            }

            Coins -= _skills.CostAt(index);
            _skills.Advance(index);
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
            _skills = new SkillTreeProgress(_skillTree, _save);
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

            string equippedId = _save.GetEquippedGunId(_guns[0].Id);
            _equippedGunIndex = 0;
            for (int i = 0; i < _guns.Length; i++)
            {
                if (_guns[i] != null && _guns[i].Id == equippedId)
                {
                    _equippedGunIndex = i;
                    break;
                }
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
