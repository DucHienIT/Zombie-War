namespace ZombieWar.Data
{
    // Every value a passive skill is allowed to move. Multiplicative stats rest at 1,
    // additive stats at 0, so a run with no skills picked behaves exactly like the
    // definition assets alone. Skill assets serialize the index, so inserting or removing
    // a member means renumbering every _stat in Data/Roguelike/Skill_*.asset.
    public enum StatId
    {
        WeaponDamage,
        FireRate,
        ProjectilePierce,
        Knockback,
        MoveSpeed,
        MaxHealth,
        HealPerKill,
        // Additive: bombs added to every Auto Bomb volley.
        ExtraBombsPerVolley,
        BombRadius,
        BombDamage,
        XpGain
    }
}
