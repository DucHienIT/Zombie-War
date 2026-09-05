namespace ZombieWar.Data
{
    public enum StatModifierKind
    {
        // Adds the value: +15 max health, +1 bomb charge.
        Additive,
        // Scales the stat, so 1.2 reads as +20%. Stacks compound: two levels give 1.44.
        Multiplicative
    }
}
