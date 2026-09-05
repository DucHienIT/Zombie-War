namespace ZombieWar.Player
{
    // The two sinks a skill can write itself into during a rebuild. Passed by the director so
    // a skill asset never has to hold a scene reference of its own.
    public readonly struct SkillApplyContext
    {
        public readonly PlayerStatSheet Stats;
        public readonly AbilityRunner Abilities;

        public SkillApplyContext(PlayerStatSheet stats, AbilityRunner abilities)
        {
            Stats = stats;
            Abilities = abilities;
        }
    }
}
