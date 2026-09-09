namespace ZombieWar.Core
{
    public enum GameState
    {
        // Idle: the menu overlay is up and no run exists yet.
        Menu,
        // Opening cinematic: the world exists and the soldier stands ready, but nothing moves yet.
        Intro,
        Playing,
        Paused,
        // Modal upgrade draft: the run is frozen but it is not the pause menu.
        LevelUp,
        Won,
        Lost,
        // Between runs: the loading panel covers the screen while the run that just ended is
        // torn down and the next one (or the menu) is built in place.
        Loading
    }
}
