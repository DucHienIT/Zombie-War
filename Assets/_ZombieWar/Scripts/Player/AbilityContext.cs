using UnityEngine;
using ZombieWar.Weapons;

namespace ZombieWar.Player
{
    // The capabilities an auto-triggered ability is allowed to reach. AbilityRunner builds it
    // once from its own Inspector references, so a skill asset stays free of scene references
    // and stays safe to share between every instance of the game.
    //
    // This is the one file that grows when a new ability needs a system nobody used before.
    public readonly struct AbilityContext
    {
        public readonly Transform Player;
        public readonly BombThrower Bombs;
        public readonly OrbitBladesController Blades;
        public readonly MolotovThrower Molotovs;
        public readonly ChainLightningCaster Lightning;
        public readonly ShockwaveEmitter Shockwave;
        public readonly SentryDroneController Drone;

        public AbilityContext(Transform player, BombThrower bombs, OrbitBladesController blades, MolotovThrower molotovs,
            ChainLightningCaster lightning, ShockwaveEmitter shockwave, SentryDroneController drone)
        {
            Player = player;
            Bombs = bombs;
            Blades = blades;
            Molotovs = molotovs;
            Lightning = lightning;
            Shockwave = shockwave;
            Drone = drone;
        }
    }
}
