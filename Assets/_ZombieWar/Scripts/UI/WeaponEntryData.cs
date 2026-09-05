using UnityEngine;

namespace ZombieWar.UI
{
    public readonly struct WeaponStatsData
    {
        public readonly float Damage;
        public readonly float ShotsPerSecond;
        public readonly int Magazine;
        public readonly float Reload;

        public WeaponStatsData(float damage, float shotsPerSecond, int magazine, float reload)
        {
            Damage = damage;
            ShotsPerSecond = shotsPerSecond;
            Magazine = magazine;
            Reload = reload;
        }
    }

    // One gun as the weapon page and its detail sheet see it; neither touches GunDefinitionSO.
    public readonly struct WeaponEntryData
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly Sprite Icon;
        public readonly int Level;
        public readonly int MaxLevel;
        public readonly bool Unlocked;
        public readonly WeaponStatsData Current;
        public readonly WeaponStatsData Next;
        public readonly int Pellets;
        public readonly bool IsSpread;
        public readonly int UpgradeCost;
        public readonly bool CanAfford;
        // Coins still missing for the next upgrade; zero when affordable.
        public readonly int CoinsMissing;

        public WeaponEntryData(string displayName, string description, Sprite icon, int level, int maxLevel, bool unlocked,
            in WeaponStatsData current, in WeaponStatsData next, int pellets, bool isSpread, int upgradeCost, bool canAfford, int coinsMissing)
        {
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            Level = level;
            MaxLevel = maxLevel;
            Unlocked = unlocked;
            Current = current;
            Next = next;
            Pellets = pellets;
            IsSpread = isSpread;
            UpgradeCost = upgradeCost;
            CanAfford = canAfford;
            CoinsMissing = coinsMissing;
        }

        public bool IsMaxLevel => Level >= MaxLevel;
    }
}
