namespace ZombieWar.Core
{
    public enum GameState
    {
        // Idle: the menu overlay is up and no run exists yet.
        Menu,
        Countdown,
        Playing,
        Paused,
        // Modal upgrade draft: the run is frozen but it is not the pause menu.
        LevelUp,
        Won,
        Lost
    }
}
