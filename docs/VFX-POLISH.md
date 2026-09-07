# Combat VFX — 2026-09-07

Presentation changes only; skill damage, radius, cooldown and targeting are unchanged.

| Effect | Authoring location | Change |
| --- | --- | --- |
| Chain lightning | GameplayRoot / ChainLightningCaster | 12 segments, 0.035 s shape refresh, 0.24 s width envelope. Endpoints stay fixed at the strike positions. Visual noise does not consume gameplay Random. |
| Electrical hit | Vfx_LightningImpact.prefab | Cyan flash and seven short sparks; dedicated pool prewarmed to 16. Replaces flesh particles only for lightning. |
| Shockwave | GameplayRoot / ShockwaveEmitter | Hollow annulus mesh, blue color/alpha gradient over 0.48 s. Outer diameter still matches the damage radius. |
| Orbit blades | GameplayRoot / OrbitBladesController | Six tapered mint trails, 0.16 s lifetime; cleared on reconfiguration and disable. |
| Molotov | MolotovDefinition / MolotovFireZone.prefab | Dedicated fire prefab: smaller flames, fine embers, ignition flash and low-opacity rising smoke. Original environmental FireZone remains unchanged. |
| Sentry drone | GameplayRoot / Drone; Gun_Drone.asset | Two small cyan thrusters and dedicated half-size muzzle flash; flash pool prewarmed to 8. |
| Bomb / Auto Bomb | Vfx_BombExplosion.prefab | Shorter flash, reduced smoke size/lifetime, varied sparks and bounded particle capacity. |
| Rifle / shotgun / prop hit | Existing VFX prefabs | Smaller flash glow and shorter, smaller impact smoke. Muzzle orientation retained. |

All materials and meshes are first-party assets. No runtime object/component construction or permanent editor generator was added. New pooled effects use the existing VfxService lifecycle. Particle capacities are authoring limits, not measured performance claims.

Validation: Unity script compilation and text-only checks of prefab references, materials, particle budgets and pool lifetimes. No Play Mode, particle simulation, screenshot or visual preview was used. Appearance and frame time still need manual evaluation in gameplay on the target device.
