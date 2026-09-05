namespace ZombieWar.Core
{
    public enum GameState
    {
        // Idle: the menu overlay is up and no run exists yet.
        Menu,
        Countdown,
        Playing,
        Paused,
        Won,
        Lost
    }
}
