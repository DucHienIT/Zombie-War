# Zombie War

A top-down 3D zombie shooter for mobile, built in Unity with the Universal Render Pipeline (URP) and the new Input System. The game runs in portrait (9:16): the soldier stands in the middle of an arena while zombies pour in from every side, and each level is a roughly three-minute survival run with an in-run roguelike upgrade loop on top.

> Branch `build` is the stripped submission branch: it only carries the assets and packages the game actually ships with. The spare third-party asset packs (`Assets/ThirdParty`), design documents and development notes live on branch `dev`. Branch `main` mirrors `build`.

## Features

- **Camera** — top-down Cinemachine camera following the soldier, with look-ahead toward the joystick, impulse-based screen shake (toggleable) and a short orbiting intro cinematic at the start of every run.
- **Controls** — a floating virtual joystick (custom `OnScreenControl` feeding the Input System's `Player/Move` action) that appears wherever the thumb lands in the lower half of the screen; aiming and firing are automatic.
- **Soldier** — humanoid rig with a layered Animator: a 2D locomotion blend tree on the lower body, a shooting layer on the upper body driven by an avatar mask, plus hit-reaction and death layers. Two-hand IK pins the hands to whichever gun is equipped. Damage feedback: material flash, blood VFX, a body jolt, a vignette pulse and camera impulse.
- **Zombies** — Walker, Runner, Brute, Elite, Giant and a chapter boss (Bloater King), all NavMesh-driven with a Spawning / Chase / Attack / Hit-stun / Knockback / Dying state machine. Hit reactions pick a directional flinch clip from the bullet's side, four randomized gait sets keep crowds from marching in lockstep, and deaths dissolve through a custom HLSL shader (with shadow and depth passes) before the body is returned to the pool.
- **Guns** — four weapons (assault rifle, shotgun, SMG, sniper rifle) authored as ScriptableObjects: damage, fire interval, range, spread, pellet count, pierce, knockback, recoil. A starting weapon is chosen before the run and the HUD gun button cycles through unlocked guns mid-run. Muzzle flash, tracers, impact particles, procedural recoil and per-gun camera impulse.
- **Bombs** — the AUTO GRENADE ability throws grenades on a cooldown; the explosion deals falloff damage and applies a physics impulse, so corpses fly with the blast on a dedicated `Corpse` layer. A pulsing telegraph ring marks the impact point.
- **Levels** — four chapters loaded as map prefabs inside a single gameplay scene, each with its own baked NavMesh: a flat sandbag outpost, a hill map with a raised plateau and four ramps, a city district and a hill district. Waves are phase-based with rising spawn rate and alive caps, scripted Elite / Giant / boss spawns and an anti-spike limiter. Chapter 1 only ends when the boss is down.
- **Audio & VFX** — polyphonic world audio with per-clip voice caps and separate gates for zombie attack / hit / spawn voices, gun shots, explosions and UI taps; particle effects from WarFX and Epic Toon FX for muzzle, tracers, impacts, blood, explosions and abilities; URP post-processing (bloom, tonemapping, vignette).
- **Roguelike layer** — kills drop XP orbs; filling the bar offers a choice of three skills: eight stat passives and six auto-firing actives (Auto Grenade, Orbit Blades, Molotov, Chain Lightning, Shockwave aura, Sentry Drone).
- **Meta progression** — account level, coins, per-gun upgrade levels and a 13-node permanent skill tree, all persisted through PlayerPrefs.
- **UI** — uGUI + TextMeshPro + DOTween, a single `UIRoot` prefab with menu, HUD, intro overlay, popup stack and loading panel. Canvas scaler matches width at 1080×1920 with a safe-area fitter, so it adapts to 9:16, 9:19.5 and 3:4 screens. Sprites are packed into two sprite atlases.

## Requirements

- **Unity** `2022.3.62f3` (Unity 2022 LTS). Open the project with exactly this editor version.
- **Android Build Support** (with SDK / NDK / OpenJDK) installed through Unity Hub to build the APK.

## Main packages

- **Universal RP** (`com.unity.render-pipelines.universal`) — pipeline asset at `Assets/Settings/UniversalRP.asset`; the default renderer is `UniversalRenderer.asset` (3D forward).
- **Input System** (`com.unity.inputsystem`) — the virtual joystick goes through an on-screen device; action asset at `Assets/InputSystem_Actions.inputactions`.
- **Cinemachine** — top-down follow camera and intro cinematic camera.
- **AI Navigation** — `NavMeshSurface` baked per map prefab.
- **TextMeshPro**, **uGUI** — UI.
- **DOTween / DOTweenPro** — tweening (`Assets/Plugins/Demigiant`).
- **Toony Colors Pro 2 Hybrid** — toon shading; only the shader files are kept, at `Assets/_ZombieWar/Art/Shaders/`.

## Getting started

1. Clone the repository on the `build` branch:
   ```bash
   git clone -b build https://github.com/DucHienIT/Zombie-War.git
   ```
2. Open the project through **Unity Hub** with editor `2022.3.62f3`.
3. Let Unity import the packages and regenerate the `Library/` folder on the first launch.
4. Open `Assets/_ZombieWar/Scenes/Loading.unity` and press Play, or build the APK as described below.

## Building the APK

Builds go through an editor menu instead of File → Build Settings:

| Menu | Output |
|---|---|
| `Tools ▸ Zombie War ▸ Build ▸ Android APK (Release)` | `Builds/Android/Release/ZombieWar-<version>-vc<code>.apk` |
| `Tools ▸ Zombie War ▸ Build ▸ Android APK (Development)` | `Builds/Android/Development/…-dev.apk` (profiler attached) |

The build tool (`Assets/_ZombieWar/Scripts/Editor/AndroidBuilder.cs`) owns every player setting that affects the package — IL2CPP, ARM64 only, min SDK 25, portrait lock, ASTC texture subtarget, APK instead of AAB — and reapplies them on every build, so nothing has to be ticked by hand. Manual edits in the Inspector are overwritten by the next build; change the constants in the script instead. Each build also bumps the patch component of `bundleVersion` and the Android version code.

Textures and audio have Android import rules enforced automatically at import time (`TextureImportRules` / `AudioImportRules`): ASTC compression with a max size per content type, mono Vorbis audio. Run `Tools ▸ Zombie War ▸ Assets ▸ Reapply Texture Import Rules (Android)` once before a release build.

By default the APK is signed with Unity's debug keystore, which installs and runs on real devices but is rejected by the Play Store. To sign with a real key, set the four environment variables `ZW_ANDROID_KEYSTORE`, `ZW_ANDROID_KEYSTORE_PASS`, `ZW_ANDROID_KEYALIAS` and `ZW_ANDROID_KEYALIAS_PASS` before launching Unity.

Headless build:

```bash
Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod ZombieWar.EditorTools.AndroidBuilder.BuildReleaseFromCommandLine -logFile build.log
```

## Project layout

```
Assets/_ZombieWar/   # All first-party content: Scripts, Data, Prefabs, Scenes, Models, Animation, Audio, Art
Assets/Plugins/      # DOTween
Assets/Settings/     # URP asset and renderers
Assets/TextMesh Pro/ # TMP Essential Resources
Packages/            # Package manifest and lock file
ProjectSettings/     # Unity project configuration
```

Inside `Assets/_ZombieWar/Scripts/` the code is split by system: `Core` (game flow, loading, profile, settings, feel), `Player`, `Weapons`, `Enemies`, `Level` (map loading, wave director, spawning), `Roguelike`, `UI`, `Data` (ScriptableObject definitions), `Audio`, `VFX`, `Utils`, plus an Editor-only assembly for the build tool and import rules. All balance numbers live in ScriptableObject instances under `Assets/_ZombieWar/Data/`, not in code.

The game has exactly two scenes: `Assets/_ZombieWar/Scenes/Loading.unity` (build index 0) and `Gameplay.unity`. The whole runtime lives in `Prefabs/GameplayRoot.prefab`; each level's map is a prefab under `Prefabs/Environment/Map_*.prefab`, instantiated in place when a run starts, so switching levels, retrying and returning to the menu never reload the scene.

> Generated folders (`Library/`, `Temp/`, `obj/`, `Logs/`, `Builds/`, IDE and solution files) are excluded through `.gitignore`.
