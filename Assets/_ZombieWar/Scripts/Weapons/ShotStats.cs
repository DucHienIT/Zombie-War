namespace ZombieWar.Weapons
{
    // One fired shot after everything has been folded in: the gun's upgrade level from the
    // menu and the passive skills picked during the run. GunStats is what the gun owns,
    // this is what actually leaves the muzzle.
    public readonly struct ShotStats
    {
        public readonly float Damage;
        public readonly float Knockback;
        // Extra zombies the bullet punches through before it is spent.
        public readonly int Pierce;

        public ShotStats(float damage, float knockback, int pierce)
        {
            Damage = damage;
            Knockback = knockback;
            Pierce = pierce;
        }
    }
}
