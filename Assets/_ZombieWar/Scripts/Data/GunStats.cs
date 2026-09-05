namespace ZombieWar.Data
{
    // Effective ballistics of a gun at one upgrade level; GunDefinitionSO owns the base values.
    public readonly struct GunStats
    {
        public readonly float Damage;
        public readonly float FireInterval;
        public readonly int MagazineSize;
        public readonly float ReloadDuration;

        public GunStats(float damage, float fireInterval, int magazineSize, float reloadDuration)
        {
            Damage = damage;
            FireInterval = fireInterval;
            MagazineSize = magazineSize;
            ReloadDuration = reloadDuration;
        }

        public float ShotsPerSecond => FireInterval > 0f ? 1f / FireInterval : 0f;
    }
}
