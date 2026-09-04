# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## BẮT BUỘC: đọc `CODE-RULE.md` trước

Trước khi viết, sửa, hay review bất kỳ script nào trong project, phải đọc toàn bộ **`CODE-RULE.md`** ở root repo. Đó là bộ quy tắc code bắt buộc (cấu trúc thư mục, naming, kiến trúc data-driven, cấm bootstrap runtime, zero-allocation, SOLID, quy trình compile-check). File CLAUDE.md này chỉ chứa những gì riêng của project và không lặp lại các quy tắc đó. Code vi phạm `CODE-RULE.md` coi như chưa xong.

## Quy tắc ngôn ngữ

- **Mọi thứ trong script** viết bằng **tiếng Anh 100%**: tên class/method/field/biến, string log, comment, tên asset/prefab/scene do code tham chiếu. Không để tiếng Việt lọt vào code, kể cả comment.
- Trả lời người dùng bằng tiếng Việt.

## Project này là gì

**Zombie War** — game bắn súng 3D góc nhìn top-down cho mobile, làm để nộp bài test kỹ thuật, dựng trên một template game casual có sẵn. Toàn bộ asset và code first-party nằm dưới `Assets/_ZombieWar/` (xem Bố cục code first-party); mọi asset pack third-party gom trong `Assets/ThirdParty/`. Gameplay P0 đã dựng xong khung (xem "Hiện trạng hệ thống"). **Game chạy màn hình dọc 9:16** (quyết định của user ngày 2026-09-05, khác với spec docx viết cho landscape). Build list chỉ có hai scene trong `Assets/_ZombieWar/Scenes/`: `MainMenu` và `Gameplay`; **các level không tách scene** — map mỗi level là prefab `Prefabs/Environment/Map_*.prefab` được `LevelMapLoader` instantiate vào scene `Gameplay`.

## Spec (nguồn sự thật cho gameplay)

Spec/GDD đầy đủ (số cân bằng, state machine, wave timeline, acceptance criteria) là **`docs/Zombie_War_SPEC_GDD_V1.0.docx`** (đã thay thế `docs/GAME_SPEC.md` cũ). File docx không đọc trực tiếp được — trích text bằng Python (`zipfile` + parse `word/document.xml`, xuất heading/paragraph/table ra markdown vào scratchpad) rồi đọc. Theo `CODE-RULE.md` §8, logic gameplay phải bám đúng spec — không sáng tạo thêm cơ chế, mọi số cân bằng là nút vặn trong ScriptableObject. Đọc spec trước khi bắt đầu bất kỳ tính năng gameplay nào.

Bản rút gọn 8 yêu cầu bắt buộc:

1. **Camera** — top-down, **Cinemachine** follow soldier; zombie tràn về từ bốn phía.
2. **Control** — virtual joystick.
3. **Soldier** — Animator **layer** tách chạy (thân dưới) và bắn (thân trên); hiệu ứng mất máu nhìn thấy được.
4. **Zombie** — AI tìm soldier; phản hồi khi trúng đạn; chết dissolve bằng **shader**.
5. **Gun** — ít nhất 2 loại súng, nút switch trên HUD, particle nòng súng/đạn chạm, giật súng.
6. **Bom** — nổ gây sát thương **và lực vật lý** lên zombie.
7. **Level** — mỗi level ~3 phút. Level 1: phẳng + vật cản. Level 2 (điểm cộng): dốc + zombie khổng lồ. Nhịp spawn tăng dần.
8. **Âm thanh + particle** xuyên suốt.

Chấm theo Gameplay, Physics, Animation (Blend Trees), Shader/Visual/UI có hỗ trợ multi-resolution. Nộp: source GitHub, **APK Android**, video gameplay. Deadline 7 ngày (mục tiêu 3–4 ngày).

## Phiên bản Unity (quan trọng)

- Editor là **`2022.3.62f3`** (`ProjectSettings/ProjectVersion.txt`). Project đã chủ động hạ từ Unity 6 xuống; mở đúng phiên bản này để tránh bị ép upgrade. Nếu `README.md` và `ProjectVersion.txt` lệch nhau, `ProjectVersion.txt` là đúng.

## Package cho spec (đã cài)

- **Cinemachine `2.10.7`** — spec bắt buộc, dùng cho camera top-down follow soldier. Không tự viết camera follow.
- **AI Navigation `1.1.7`** — cung cấp `NavMeshSurface` để bake theo component; cần cho dốc ở Level 2 và né vật cản. Module built-in `com.unity.modules.ai` (`NavMeshAgent`) vẫn dùng song song như bình thường.

## Stack chính

- **URP 14.0** với hai renderer trên `Assets/Settings/UniversalRP.asset`: index 0 = `Renderer2D.asset`, index **1 = `UniversalRenderer.asset`** (3D forward) và **index 1 là mặc định** — camera để `Renderer: Default` là đã đi đúng đường 3D, không cần set tay. `UniversalRP.asset` cũng được gán vào Project Settings → Graphics → Scriptable Render Pipeline Settings, ngoài override sẵn ở cả 6 quality level. Cấu hình đang để: MSAA **4x**, HDR bật, `IntermediateTextureMode: Auto`, SRP Batcher bật, shadow distance 50 / 1 cascade. Global settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`.
- **Input System mới 1.14.2** duy nhất (`activeInputHandler: 1`; `Input.*` cũ đã tắt). Asset action `Assets/InputSystem_Actions.inputactions` đã đăng ký trong `EditorBuildSettings` và có sẵn map `Player` (`Move`, `Attack`, `Previous`, `Next`, ...) — nối joystick ảo bằng component **On-Screen Stick/Button** của Input System vào các action này, không dựng đường input song song.
- **DOTween / DOTweenPro** (`Assets/Plugins/Demigiant/`), settings tại `Assets/Resources/DOTweenSettings.asset`. Luôn `SetLink(gameObject)`.
- **Layer Lab GUI Pro-CasualGame** (`Assets/ThirdParty/Layer Lab/GUI Pro-CasualGame/Prefabs/`) — bộ prefab UI (button, frame, popup, slider, label) và một script `UIParticleSystem`. **Hiện HUD/menu chưa tham chiếu file nào của pack này** (rà reference 2026-09-05: 0/3626 file được dùng, 180 MB); dùng làm nguồn sprite/prefab khi cần polish UI, hoặc gỡ hẳn pack nếu quyết định giữ UI thuần code.
- **Toony Colors Pro 2** (`Assets/ThirdParty/JMO Assets/`) — toon shading, có asmdef riêng `ToonyColorsPro.*`; nền tốt cho dissolve zombie và look toon.
- **TextMeshPro**, **Timeline**, **Visual Scripting**, **bộ 2D** đã cài nhưng không phải trọng tâm của game này. TMP Essential Resources **đã import** (`Assets/TextMesh Pro/`); HUD dùng font `LiberationSans SDF`.
- **DOTween modules có asmdef riêng** (`DOTween.Modules.asmdef`, `DOTweenPro.Scripts.asmdef`, tạo bằng chính ASMDEFManager của DOTween). `ZombieWar.asmdef` reference `DOTween.Modules` để dùng `DOFillAmount`/`DOFade` của uGUI; không xoá các asmdef này.

## Bố cục `Assets/` và asset third-party

Root `Assets/` chỉ còn: `_ZombieWar/` (first-party), `ThirdParty/` (mọi asset pack ngoài), `Plugins/` (DOTween DLL — special folder của Unity, không được move), `Resources/` (DOTweenSettings), `Settings/` (URP), `TextMesh Pro/`, cùng 3 asset lẻ (`DefaultVolumeProfile.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `InputSystem_Actions.inputactions`).

**Pack mới import phải move ngay vào `Assets/ThirdParty/`** — move bằng Unity (`AssetDatabase.MoveAsset` / kéo trong Project window) để giữ GUID, đừng move bằng file system khi Editor đang mở.

Đợt dọn 2026-09-05 đã xoá vĩnh viễn (khôi phục được qua git nếu cần): demo scene + demo folder của Epic Toon FX / WarFX / Toony Colors Pro / Low Poly Guns / ToonSoldiers / Survivalist, `Epic Toon FX/Prefabs 2D` + `Upgrade`, `WarFX/_Effects` + `Desktop` (bản desktop), `Survivalist/Materials HDRP`, phần không dùng của `Survivalist/StarterAssets`, `ithappy/Military_Free/Render_Pipeline_Convert` (3 unitypackage convert pipeline), `Low Poly Guns/Scripts` (script demo Assembly-CSharp) và `Assets/Scenes/SampleScene.unity`.

## Asset pack — phân vai

Mỗi pack chỉ đóng **một** vai. Đừng lấy model của pack animation hay ngược lại.

### Model

| Thư mục | Vai | Ghi chú |
|---|---|---|
| `Assets/ThirdParty/ArtStore3D/Zombie/` | Zombie chính | Rig **Humanoid**, `URP/Lit`. `Anim/` chỉ chứa animation camera/đèn của demo scene, **không** phải animation nhân vật |
| `Assets/ThirdParty/Survivalist/` | Soldier người chơi | Rig Humanoid, nhiều skin. Material là `URP/Autodesk Interactive` (pack import từ FBX Autodesk Interactive) — nặng hơn `URP/Lit` nhưng chỉ 1 instance trên màn hình; thư mục `Materials URP` của pack cũng đúng shader đó, không hơn gì `Materials` |
| `Assets/ThirdParty/Low Poly Guns/` | Súng | `URP/Lit`. Nguồn cho ≥2 loại súng của spec §5 |
| `Assets/ThirdParty/ithappy/Military_Free/` | Môi trường, vật cản | `URP/Lit` sẵn từ pack |
| `Assets/ThirdParty/ToonSoldiers_WW2_demo/model/` | (không dùng) | Model đi kèm pack animation, giữ để preview clip |

### Animation

Tất cả rig nhân vật đều **Humanoid**, nên clip retarget chéo giữa các model được.

| Nguồn | Clip | Dùng cho |
|---|---|---|
| `Assets/ThirdParty/ToonSoldiers_WW2_demo/animation/` | `infantry_combat_idle`, `infantry_combat_run`, `infantry_combat_shoot`, `infantry_guard_idle` | Soldier: `combat_run` cho layer thân dưới, `combat_shoot` cho layer thân trên (spec §3) |
| `Assets/ThirdParty/Zombie_Animations/Animations/` | Bộ clip zombie (idle/walk/attack/death/paired) | Zombie: nguồn animation chính, đã nối vào `ZombieAnimator` |
| `Assets/ThirdParty/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/` | `Idle`, `Walk_N`, `Run_N`, `Run_S`, `Jump`, `InAir`, 2 clip land | Locomotion + Blend Tree cho soldier. **Giữ thư mục này** — phần còn lại của StarterAssets (`Editor`, `Environment`, `InputSystem`, `ThirdPersonController/Scripts`) đã xoá |

Animation zombie đến từ pack `Assets/ThirdParty/Zombie_Animations/` (import 2026-09-05). Pack `Zombie 1 Low Poly` chỉ có animation camera/đèn của demo scene; model đã rig Humanoid kèm Avatar nên clip Humanoid retarget thẳng được — spec §4 cần chase / hit-reaction / death.

Pack `FREE Shirtless Zombie` (`Assets/NewPunch/`) **đã bị gỡ khỏi project**: đối chiếu tận file `.unitypackage` gốc thì nó chỉ có 2 FBX model, 6 prefab, 3 scene demo, 1 script và material/texture — không có animation nào, trong khi vai trò dự kiến của nó là nguồn animation zombie. Đừng import lại.

`Survivalist/StarterAssets/ThirdPersonController/Character/Animations/` còn kèm `StarterAssetsThirdPerson.controller` — Animator controller có sẵn Blend Tree locomotion, dùng làm điểm khởi đầu cho layer thân dưới của soldier.

### Sound

| Thư mục | Vai |
|---|---|
| `Assets/ThirdParty/Tybug Studios/Zombie Voice Pack - Free/` | SFX zombie — 10 wav chia sẵn theo hành vi: Aggressive, Chase, Death, Growl, Grunt, Hiss, Moan |
| `Assets/ThirdParty/PostApocalypseGunsDemo/` | SFX súng — 41 wav: AssaultRifles, Pistols, Shotguns, SniperRifles, Miniguns_loop |

### Particle

| Thư mục | Vai |
|---|---|
| `Assets/ThirdParty/JMO Assets/WarFX/` | VFX súng đạn thực chiến — muzzle flash, bullet impact theo vật liệu, explosion. Chỉ còn bộ **`_Effects (Mobile)`** + `Mobile/`; bản desktop (`_Effects`, `Desktop/`) và scene demo đã xoá |
| `Assets/ThirdParty/Epic Toon FX/` | VFX toon — `Prefabs/Combat`, `Environment`, `Interactive`. Hợp look toon cho nổ bom và dissolve zombie. **Nặng 534 MB nhưng mới dùng đúng 4 file** (`Materials/Basics/circle_AB.mat`, `cloud_2x2_default_AB.mat` + 2 texture) |

Material built-in của pack mới import convert bằng **Tools ▸ Zombie War ▸ Convert Built-in Materials To URP** (`Assets/_ZombieWar/Scripts/Editor/BuiltInToUrpMaterialConverter.cs`) — nó gom material còn shader built-in rồi gọi converter chính chủ của Unity; skybox và material UI cố ý không đụng tới vì chạy tốt dưới URP. URP **không có** upgrader cho `Mobile/Particles/*` và `Legacy Shaders/Particles/*`. 14 material particle của WarFX đã gán tay sang `URP/Particles/Unlit` (`_BaseColor = 2 × _TintColor` vì shader particle đời cũ nhân đôi tint; additive → `_Blend: 2`, alpha blended → `_Blend: 0`).

**35 material trong `Layer Lab/GUI Pro-CasualGame/ResourcesData/Particle/Materials/` cố ý giữ nguyên `Mobile/Particles/*`** — đó là particle UI chạy qua `UIParticleSystem` trên `CanvasRenderer`, không phải `ParticleSystemRenderer`. Shader particle của URP không hỗ trợ masking/stencil của Canvas, đổi sang là hỏng UI. Chúng render đúng dưới URP như hiện tại.

## Bố cục code first-party

Khung thư mục đã dựng sẵn theo `CODE-RULE.md` §1 và phân rã hệ thống ở `docs/GAME_SPEC.md` §4:

```
Assets/_ZombieWar/            # mọi thứ first-party nằm trong đây, tách hẳn khỏi asset pack third-party
├── Scripts/    Core, Player, Weapons, Enemies, Level, UI, Data, Audio, Utils, Editor
├── Data/       Weapons, Zombies, Levels     # instance .asset của ScriptableObject
├── Prefabs/    Player, Enemies, Weapons, VFX, UI, Environment
├── Animation/  Soldier, Zombie              # Animator controller, Avatar Mask, Blend Tree
├── Art/        Materials, Shaders           # shader dissolve zombie, material first-party
└── Scenes/                                  # scene gameplay 3D
```

`CODE-RULE.md` §1 viết đường dẫn là `Assets/Scripts/`; project này đặt cả cây dưới root `Assets/_ZombieWar/`, cấu trúc con và luật chia hệ thống giữ nguyên. Scene gameplay tạo trong `Assets/_ZombieWar/Scenes/`.

Thư mục còn rỗng giữ bằng `.gitkeep` (Unity bỏ qua file bắt đầu bằng dấu chấm, không sinh `.meta`) — xoá khi thư mục có asset thật.

Hai assembly definition tách code first-party khỏi `Assembly-CSharp` của các asset pack: `ZombieWar` (`Assets/_ZombieWar/Scripts/ZombieWar.asmdef`, runtime, reference Unity.InputSystem / Cinemachine / Unity.AI.Navigation / Unity.TextMeshPro / UnityEngine.UI) và `ZombieWar.Editor` (`Assets/_ZombieWar/Scripts/Editor/`, chỉ platform Editor, reference `ZombieWar`). DOTween là DLL trong `Assets/Plugins/` nên auto-reference, không cần khai báo. Thêm package mới cần dùng trong script thì bổ sung vào `references` của `ZombieWar.asmdef`. Không bao giờ đặt code vào `Assets/ThirdParty/JMO Assets`, `Assets/ThirdParty/Layer Lab`, `Assets/Plugins`.

`Assets/_Recovery/0.unity` là file recovery sau crash của Unity, không phải scene thật — không dựng gì trên nó; xoá khi tiện.

## Hiện trạng hệ thống (cập nhật 2026-09-05)

Toàn bộ runtime nằm trong một prefab **`Assets/_ZombieWar/Prefabs/GameplayRoot.prefab`** (Systems/Managers, Pools, Map, Player, CameraRig, UI); scene `Gameplay` chỉ chứa một instance của nó. Luồng chọn level: `MainMenuView` → `LevelLoader.LoadLevel(level)` ghi vào `Data/Levels/LevelSelection.asset` (`LevelSelectionSO`, kênh runtime giữa hai scene) rồi load scene `Gameplay`; ở đó `LevelMapLoader` (`DefaultExecutionOrder(-100)`, trên `Systems/Managers`) đọc selection (fallback Level 1 khi mở scene trực tiếp), instantiate `LevelDefinitionSO.MapPrefab` vào `Map/` và đặt player tại `LevelMap.PlayerSpawn`. `GameFlowController`/`WaveDirector` lấy level từ `LevelMapLoader.Level`, không giữ reference level riêng. Retry/Next = reload scene `Gameplay`.

Map prefab (`Prefabs/Environment/Map_FlatOutpost.prefab`, `Map_BurningHills.prefab`) có root `LevelMap` chứa: `Environment` (đất, tường biên, vật cản), `Directional Light`, `PlayerSpawn`, và `NavMeshSurface` (collect **Children**, data bake lưu ở `Prefabs/Environment/NavMesh_Map_*.asset`). Sửa map xong phải bake lại: đặt prefab vào scene, `BuildNavMesh()`, `AssetDatabase.CreateAsset` đè lên file data cũ, save prefab. Thêm level mới = thêm map prefab + `LevelDefinitionSO` + entry trong `MainMenuView`, không đụng code.

Camera portrait: vcam pitch 72°, follow offset (0, 14, −4.5); `CameraAspectAdapter` giữ **FOV ngang** cố định (40°) và suy ra FOV dọc theo aspect (clamp 45–70°), nên 9:16 / 9:19.5 / 3:4 đều thấy cùng bề rộng làn. Spawn ring đã nới lên 14–20 m (min 12) vì màn dọc nhìn xa về phía trước; `_navMeshSampleRadius = 5` ở cả hai level để spawn được khi player đứng trên plateau. UI: Canvas Scaler reference 1080×1920, match 0.5.

| Hệ thống | Script chính | Ghi chú |
|---|---|---|
| Flow | `Core/GameFlowController` (Countdown→Playing→Paused/Won/Lost), `LevelTimer`, `ScoreTracker`, `SaveService` (PlayerPrefs) | Entry point duy nhất mỗi scene; UI chỉ subscribe event |
| Player | `Player/PlayerMotor` (Rigidbody), `PlayerAim` (OverlapSphereNonAlloc + Linecast LOS, hold 0.35 s), `PlayerHealth`, `PlayerAnimationPresenter`, `PlayerHitFlash` (MPB, property `_Color` vì material Autodesk Interactive), `PlayerDeathPresenter` (tween ngã, vì không có clip death) | Input: `PlayerInputReader` đọc action `Player/Move`; joystick là On-Screen Stick `<Gamepad>/leftStick` |
| Weapons | `Weapons/WeaponController` (FSM Ready/Firing/Cooldown/Reloading/Switching), `Gun`, `ProjectileManager` (SphereCast, pool 180), `BombThrower` + `Bomb` (Rigidbody, fuse, telegraph ring, falloff) | Gun model gắn dưới `hand_r/GunSocket`; hướng nòng = hướng nhân vật |
| Enemies | `Enemies/ZombieManager` (pool theo `ZombieDefinitionSO`, registry Collider→zombie, tick tập trung), `ZombieController` (FSM Spawning/Chase/Attack/HitStun/Knockback/Dying), `ZombieMaterialFx` (MPB `_HitAmount`/`_DissolveAmount`) | Shader `Art/Shaders/ZombieDissolve.shader` (HLSL URP, có ShadowCaster/DepthOnly) |
| Level | `Level/LevelMapLoader` + `LevelMap` (map prefab, spawn, NavMesh), `WaveDirector` (phase, cap, weighted pick, scripted Giant @145 s, anti-spike), `SpawnPointResolver`, `FireHazardSpawner` + `FireZone` (P1, Level 2 @75 s/@120 s) | Level 2: plateau 3.5 m + 4 dốc 22° |
| UI | `UI/*View` (HealthBar, Timer, Score, GunHud, BombButton, Countdown, PauseMenu, ResultPanel, MainMenu), `SafeAreaFitter`, `TimeTextFormatter` (zero-alloc) | 4 Canvas riêng theo tần suất: HUD / Countdown / Pause / Result |
| Data | `Data/*SO` + instance trong `Assets/_ZombieWar/Data/{Player,Rules,Weapons,Zombies,Waves,Levels}` | Số liệu chép đúng spec §5–§10; chỉnh ở đây, không sửa code |

Editor tool: **Tools ▸ Zombie War ▸ Animation ▸ 1. Create Default Animator Recipe / 2. Assign Zombie Animation Pack / 3. Build Animators** đọc `Animation/AnimatorBuildRecipe.asset` (clip theo vai + ngưỡng blend + tốc độ phát) và dựng lại tại chỗ `SoldierAnimator.controller` (4 layer: Base Locomotion blend tree 2D, Upper Combat với `UpperBodyMask`, Hit Reaction, Full Body) và `ZombieAnimator.controller` (Base: blend 1D theo **m/s** Idle/Walk/WalkFast/Run + Attack/Knockback/Death; Hit Reaction thân trên), giữ nguyên GUID. Thiếu clip thì state để trống (log info, không phải lỗi). Layer Hit Reaction dùng Override (không Additive) vì clip flinch của pack là full pose. Soldier chưa có clip reload/hit/death trong pack.

Zombie animation lấy từ pack **`Assets/ThirdParty/Zombie_Animations/`** (Humanoid, avatar `T_pose`): `Zombie_Idle_01`, `Zombie_Walk_01/Walk_Fast01/Run_01_Forward_InPlace` (ngưỡng 1.4/2.3/3.6 m/s), `Zombie_Attack01` (state speed 2 vì clip dài 2.33 s), `Zombie_HitReact_Head` cho Hit và Knockback (speed 3), `Zombie_Idle_Death`. Menu 2 tắt Loop Time cho ba clip one-shot đó. Không dùng `Paired_*` (cần victim) và `Crawl_*`. `ZombieController` đẩy tốc độ agent thật (m/s) vào tham số `Speed`; `_attackWindup` 0.45 s cho Walker/Runner/Brute khớp thời điểm vung tay, `FeedbackProfile._deathPoseDuration` 0.6 s để clip ngã đọc được trước khi dissolve.

Layer vật lý đã thêm: `Player`(8) `Enemy`(9) `Obstacle`(10) `Ground`(11) `Prop`(12) `Bomb`(13); ma trận đã tắt va chạm Player–Bomb. `PlayerSettings.runInBackground = true` để Play mode trong Editor không đứng hình khi cửa sổ mất focus (cần cho test qua MCP).

Còn thiếu / biết trước: nhạc nền (project không có track nào), vignette khi trúng đòn, IK tay trái cầm súng, âm nổ bom đang mượn `AntiMaterialRifle_far_01`, icon súng render từ AssetPreview (`Art/Sprites`). Thư mục `Captures/` (ảnh chụp qua MCP) đã gitignore.

## Build Android (sản phẩm APK)

Player settings đã có: scripting backend **IL2CPP**, kiến trúc **chỉ ARM64**, min SDK **25**, `productName: Zombie War`, `companyName: DucHien`, application id `com.duchien.zombiewar`, orientation **Portrait** khoá cứng. Build scene list: `MainMenu`, `Gameplay`.

**Chưa có build script hay CLI**; build trong Editor (File → Build Settings → Android). `*.apk`, `*.aab`, `Builds/` bị gitignore — đưa APK lên **GitHub Release**, đừng cố commit. Nếu thêm method build trong Editor, dạng headless là:

```bash
Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod <Namespace.Class.Method> -logFile build.log
```

(dùng binary editor 2022.3.62f3). Skill `unity-build-tool` nhắm WebGL; phải chỉnh lại chứ đừng chép nguyên.

## Test

`com.unity.test-framework` đã cài, chưa có test assembly. Khi có asmdef EditMode/PlayMode:

```bash
Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml
```

Đổi `EditMode` thành `PlayMode` khi cần. Logic C# thuần (công thức damage, nhịp wave, timer level) nên là class thường để test EditMode không cần scene.

## Unity MCP

`com.coplaydev.unity-mcp` là package dependency, nên điều khiển được Editor qua MCP (dựng scene, `validate_script`, `read_console`). Nếu thiếu tool `mcp__UnityMCP__*` hoặc sai port, dùng skill `unity-mcp-connect` — mỗi editor đang mở bind một port riêng và port đổi giữa các lần mở editor. `.mcp.json` đã nằm trong `.gitignore`. Compile-check sau mỗi lần sửa script (`CODE-RULE.md` §8).

## Git

- **Không tự ý thao tác git.** Không commit, push, tạo/xoá nhánh, merge, reset, stash hay bất kỳ lệnh ghi nào khi người dùng chưa yêu cầu và cho phép rõ ràng. Chỉ được đọc trạng thái (`git status`, `git log`, `git diff`).
- **Commit message ngắn gọn, bằng tiếng Anh**, một dòng theo dạng mệnh lệnh (ví dụ `Add zombie dissolve shader`). **Không** thêm dòng ghi nguồn hay tác giả (không `Co-Authored-By`, không "Generated with", không tên tool).
- Nhánh mặc định và release là **`main`**; làm việc hằng ngày trên **`dev`**.
- `Assembly-CSharp*.csproj` và `.sln` ở root do Unity sinh ra và bị gitignore — không sửa tay.
- `*.unitypackage` bị gitignore, nên các dòng xoá `.unitypackage.meta` lạc (ví dụ "Cat Demo URP" của Toony Colors) là nhiễu bình thường.
