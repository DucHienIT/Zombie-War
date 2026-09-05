namespace ZombieWar.Weapons
{
    // What one thrown bottle turns into on landing, fixed at throw time so a level-up taken
    // while it is in the air cannot change a fire the player has already seen leave the hand.
    public readonly struct MolotovBurn
    {
        public readonly float Radius;
        public readonly float Duration;
        public readonly float DamagePerTick;
        public readonly float TickInterval;

        public MolotovBurn(float radius, float duration, float damagePerTick, float tickInterval)
        {
            Radius = radius;
            Duration = duration;
            DamagePerTick = damagePerTick;
            TickInterval = tickInterval;
        }
    }
}
