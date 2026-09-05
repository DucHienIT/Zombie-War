namespace ZombieWar.Data
{
    // Every value a passive skill is allowed to move. Multiplicative stats rest at 1,
    // additive stats at 0, so a run with no skills picked behaves exactly like the
    // definition assets alone.
    public enum StatId
    {
        WeaponDamage,
        FireRate,
        ReloadSpeed,
        ProjectilePierce,
        Knockback,
        MoveSpeed,
        MaxHealth,
        HealPerKill,
        BombCharges,
        BombRadius,
        BombDamage,
        XpGain
    }
}
