# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## BẮT BUỘC: đọc `CODE-RULE.md` trước

Trước khi viết, sửa, hay review bất kỳ script nào trong project, phải đọc toàn bộ **`CODE-RULE.md`** ở root repo. Đó là bộ quy tắc code bắt buộc (cấu trúc thư mục, naming, kiến trúc data-driven, cấm bootstrap runtime, zero-allocation, SOLID, quy trình compile-check). File CLAUDE.md này chỉ chứa những gì riêng của project và không lặp lại các quy tắc đó. Code vi phạm `CODE-RULE.md` coi như chưa xong.

## Quy tắc ngôn ngữ

- **Mọi thứ trong script** viết bằng **tiếng Anh 100%**: tên class/method/field/biến, string log, comment, tên asset/prefab/scene do code tham chiếu. Không để tiếng Việt lọt vào code, kể cả comment.
- Trả lời người dùng bằng tiếng Việt.

## Project này là gì

**Zombie War** — game bắn súng 3D góc nhìn top-down cho mobile, làm để nộp bài test kỹ thuật, dựng trên một template game casual có sẵn. Toàn bộ asset và code first-party nằm dưới `Assets/_ZombieWar/` (xem Bố cục code first-party); mọi asset pack third-party gom trong `Assets/ThirdParty/`. Gameplay P0 đã dựng xong khung (xem "Hiện trạng hệ thống"). **Game chạy màn hình dọc 9:16** (quyết định của user ngày 2026-09-05, khác với spec docx viết cho landscape). **Game chỉ có đúng MỘT scene**: `Assets/_ZombieWar/Scenes/Gameplay.unity` (build list cũng chỉ có nó). Main menu là overlay trong chính scene đó, **các level không tách scene** — map mỗi level là prefab `Prefabs/Environment/Map_*.prefab` được `LevelMapLoader` instantiate khi bắt đầu một lượt chơi.

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
- **GUI PRO Kit - Sci-Fi Survival** (`Assets/ThirdParty/GUI PRO Kit - Sci-Fi Survival/`) — **nguồn art UI duy nhất của game** (quyết định user 2026-09-05). Button/frame/popup/slider/label/toggle, bộ pictogram `Icon_FunctionIcons(x2)/64` (~250 icon), item icon 128/256/512, art màn chơi `Demo_Play` (joystick, nút hành động, minimap) và background. 83 asset đã copy sang `Art/UI/` — xem "Kiến trúc UI".
- **Layer Lab GUI Pro-CasualGame** (`Assets/ThirdParty/Layer Lab/GUI Pro-CasualGame/`) — pack UI casual cũ, **đã bị thay hoàn toàn bởi Sci-Fi Survival**; 0 file được tham chiếu (rà GUID 2026-09-05), 180 MB. Chỉ còn giữ vì có script `UIParticleSystem`; gỡ được nếu cần chỗ.
- **Toony Colors Pro 2** (`Assets/ThirdParty/JMO Assets/`) — toon shading, có asmdef riêng `ToonyColorsPro.*`; nền tốt cho dissolve zombie và look toon.
- **TextMeshPro**, **Timeline**, **Visual Scripting**, **bộ 2D** đã cài nhưng không phải trọng tâm của game này. TMP Essential Resources **đã import** (`Assets/TextMesh Pro/`); font UI của game là **Oxanium ExtraBold** (title) + **Ubuntu Bold** (body) copy từ Sci-Fi Survival sang `Art/UI/Fonts/` (xem "Kiến trúc UI"), `LiberationSans SDF` chỉ còn là fallback mặc định của TMP.
- **DOTween modules có asmdef riêng** (`DOTween.Modules.asmdef`, `DOTweenPro.Scripts.asmdef`, tạo bằng chính ASMDEFManager của DOTween). `ZombieWar.asmdef` reference `DOTween.Modules` để dùng `DOFillAmount`/`DOFade` của uGUI; không xoá các asmdef này.

## Bố cục `Assets/` và asset third-party

Root `Assets/` chỉ còn: `_ZombieWar/` (first-party), `ThirdParty/` (kho asset pack ngoài), `Plugins/` (DOTween DLL — special folder của Unity, không được move), `Resources/` (DOTweenSettings), `Settings/` (URP), `TextMesh Pro/`, cùng 3 asset lẻ (`DefaultVolumeProfile.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `InputSystem_Actions.inputactions`).

**Luật vàng: asset nào game thật sự dùng thì nằm trong `Assets/_ZombieWar/`, phân theo loại** (`Models/`, `Animation/`, `Audio/`, `Art/Materials`, `Art/Textures`, `Art/Shaders`, `Prefabs/`) — không tham chiếu xuyên qua `ThirdParty/`. Đợt gom 2026-09-05 đã kéo 183 file (~200 MB) từ 11 pack vào `_ZombieWar`, nên **`ThirdParty/` hiện là kho dự phòng thuần: 0 file trong đó được scene/prefab nào tham chiếu**.

Quy trình khi cần thêm asset từ pack: import pack vào `Assets/ThirdParty/<tên pack>/`, lấy đúng file cần dùng move sang thư mục loại tương ứng trong `_ZombieWar/`, phần còn lại để nguyên trong `ThirdParty/`. Move bằng Unity (`AssetDatabase.MoveAsset` / kéo trong Project window) để giữ GUID, đừng move bằng file system khi Editor đang mở.

Kiểm tra lại bất cứ lúc nào bằng cách rà GUID: quét chuỗi `guid:` trong mọi asset dưới `_ZombieWar/` + `Settings/` + `ProjectSettings/`, lấy bao đóng bắc cầu (transitive closure) rồi đối chiếu với `.meta` toàn project — file nào ngoài tập đó là không dùng.

Đợt dọn 2026-09-05 đã xoá vĩnh viễn (khôi phục được qua git nếu cần): demo scene + demo folder của Epic Toon FX / WarFX / Toony Colors Pro / Low Poly Guns / ToonSoldiers / Survivalist, `Epic Toon FX/Prefabs 2D` + `Upgrade`, `WarFX/_Effects` + `Desktop` (bản desktop), `Survivalist/Materials HDRP`, phần không dùng của `Survivalist/StarterAssets`, `ithappy/Military_Free/Render_Pipeline_Convert` (3 unitypackage convert pipeline), `Low Poly Guns/Scripts` (script demo Assembly-CSharp) và `Assets/Scenes/SampleScene.unity`.

## Asset pack — phân vai

Mỗi pack chỉ đóng **một** vai. Đừng lấy model của pack animation hay ngược lại.

### Model

Model đang dùng nằm trong `Assets/_ZombieWar/Models/`; cột "Pack gốc" chỉ để biết lấy thêm ở đâu và ghi công.

| File trong `_ZombieWar/Models/` | Vai | Pack gốc | Ghi chú |
|---|---|---|---|
| `Characters/Zombie.fbx` | Zombie chính | ArtStore3D | Rig **Humanoid**, `URP/Lit`. Material `Art/Materials/Characters/Zombie_Mat.mat` |
| `Characters/SK_Military_Survivalist.fbx` + `Armature.fbx` | Soldier người chơi | Survivalist | Rig Humanoid. 13 material ở `Art/Materials/Characters/` dùng `URP/Autodesk Interactive` (pack import từ FBX Autodesk Interactive) — nặng hơn `URP/Lit` nhưng chỉ 1 instance trên màn hình |
| `Weapons/assault1.fbx`, `Weapons/shotgun1.fbx` | 2 loại súng của spec §5 | Low Poly Guns | `URP/Lit` |
| `Environment/*.fbx` (14 mesh) | Môi trường, vật cản | ithappy Military_Free | Prefab tương ứng ở `Prefabs/Environment/Props/`, dùng chung `Art/Materials/Environment/Military_base.mat` |
| `Characters/ToonSoldier_WW2_demo.FBX` | Model preview clip | ToonSoldiers_WW2_demo | Đi kèm pack animation, giữ để preview |
| `VFX/Plane1x1.FBX` | Mesh phẳng cho particle | WarFX | |

### Animation

Tất cả rig nhân vật đều **Humanoid**, nên clip retarget chéo giữa các model được.

| File trong `_ZombieWar/Animation/` | Clip | Pack gốc |
|---|---|---|
| `Soldier/infantry_combat_idle.FBX`, `infantry_combat_shoot.FBX` | Idle + bắn cho layer thân trên (spec §3) | ToonSoldiers_WW2_demo |
| `Soldier/Locomotion--Run_N/Run_S/Walk_N.anim.fbx`, `Stand--Idle.anim.fbx` | Locomotion + Blend Tree thân dưới | Survivalist StarterAssets |
| `Zombie/Zombie_Idle_01`, `Walk_01`, `Walk_Fast01`, `Run_01`, `Attack01`, `HitReact_Head`, `HitReact_LeftArm`, `HitReact_RightArm`, `Idle_Death`, `T_pose` | Chase / attack / hit-reaction theo hướng đạn / death (spec §4) | Zombie_Animations |

Animation zombie đến từ pack `Zombie_Animations` (import 2026-09-05); 10 clip đang dùng đã nằm trong `_ZombieWar/Animation/Zombie/`, phần còn lại vẫn ở `ThirdParty/Zombie_Animations/` nếu cần lấy thêm. Pack `Zombie 1 Low Poly` chỉ có animation camera/đèn của demo scene; model đã rig Humanoid kèm Avatar nên clip Humanoid retarget thẳng được — spec §4 cần chase / hit-reaction / death.

Pack `FREE Shirtless Zombie` (`Assets/NewPunch/`) **đã bị gỡ khỏi project**: đối chiếu tận file `.unitypackage` gốc thì nó chỉ có 2 FBX model, 6 prefab, 3 scene demo, 1 script và material/texture — không có animation nào, trong khi vai trò dự kiến của nó là nguồn animation zombie. Đừng import lại.

`ThirdParty/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/` còn kèm `StarterAssetsThirdPerson.controller` — Animator controller có sẵn Blend Tree locomotion, để lại đó làm tham khảo cho layer thân dưới của soldier.

### Sound

| Thư mục | Vai | Pack gốc |
|---|---|---|
| `_ZombieWar/Audio/Zombies/` | 8 wav zombie đang dùng (aggressive, death, growl, grunt, hiss, moan) | Tybug Studios Zombie Voice Pack |
| `_ZombieWar/Audio/Weapons/` | 6 wav súng đang dùng (rifle, shotgun, reload; nổ bom đang mượn `AntiMaterialRifle_far_01`) | PostApocalypseGunsDemo |

Muốn thêm tiếng: 2 wav zombie còn lại ở `ThirdParty/Tybug Studios/`, 35 wav súng còn lại ở `ThirdParty/PostApocalypseGunsDemo/`. Project vẫn **chưa có nhạc nền** — không pack nào có track.

### Particle

Prefab particle của game là first-party trong `_ZombieWar/Prefabs/VFX/`, ăn 18 material ở `Art/Materials/VFX/` + 16 texture ở `Art/Textures/VFX/` + 2 shader `Art/Shaders/WFX_S Particle *.shader`.

| Kho dự phòng | Vai |
|---|---|
| `ThirdParty/JMO Assets/WarFX/` | VFX súng đạn thực chiến. Chỉ còn bộ **`_Effects (Mobile)`** + `Mobile/`; bản desktop (`_Effects`, `Desktop/`) và scene demo đã xoá |
| `ThirdParty/Epic Toon FX/` | VFX toon — `Prefabs/Combat`, `Environment`, `Interactive`. **534 MB mà game mới lấy đúng 4 file** (2 material + 2 texture, đã chuyển sang `_ZombieWar/Art/`) |
| `ThirdParty/GUI PRO Kit - Sci-Fi Survival/` | Sprite/font UI sci-fi — **nguồn art UI duy nhất**, 250 MB, đã lấy 83 file vào `Art/UI/` |
| `ThirdParty/Layer Lab/GUI Pro-CasualGame/` | Pack UI casual cũ — **180 MB, 0 file được dùng** sau khi đổi skin sang Sci-Fi Survival |

Material built-in của pack mới import convert bằng converter chính chủ của Unity (chọn material → **Edit ▸ Rendering ▸ Materials ▸ Convert Selected Built-in Materials to URP**); skybox và material UI cố ý không đụng tới vì chạy tốt dưới URP. Tool gom-material tự viết trước đây đã xoá (xem "Không giữ editor tool sinh object" ở mục Bố cục code). URP **không có** upgrader cho `Mobile/Particles/*` và `Legacy Shaders/Particles/*`. 14 material particle của WarFX đã gán tay sang `URP/Particles/Unlit` (`_BaseColor = 2 × _TintColor` vì shader particle đời cũ nhân đôi tint; additive → `_Blend: 2`, alpha blended → `_Blend: 0`).

**35 material trong `Layer Lab/GUI Pro-CasualGame/ResourcesData/Particle/Materials/` cố ý giữ nguyên `Mobile/Particles/*`** — đó là particle UI chạy qua `UIParticleSystem` trên `CanvasRenderer`, không phải `ParticleSystemRenderer`. Shader particle của URP không hỗ trợ masking/stencil của Canvas, đổi sang là hỏng UI. Chúng render đúng dưới URP như hiện tại.

## Bố cục code first-party

Khung thư mục đã dựng sẵn theo `CODE-RULE.md` §1 và phân rã hệ thống ở `docs/GAME_SPEC.md` §4:

```
Assets/_ZombieWar/            # mọi thứ first-party nằm trong đây, tách hẳn khỏi asset pack third-party
├── Scripts/    Core, Player, Weapons, Enemies, Level, Roguelike, UI, Data, Audio, VFX, Utils   # không có Editor/
├── Data/       Weapons, Zombies, Levels     # instance .asset của ScriptableObject
├── Prefabs/    Player, Enemies, Weapons, VFX, UI, Environment
├── Animation/  Soldier, Zombie              # Animator controller, Avatar Mask, Blend Tree, clip FBX
├── Models/     Characters, Weapons, Environment, VFX   # FBX đang dùng, kéo từ pack về
├── Audio/      Weapons, Zombies             # wav đang dùng
├── Art/        Materials, Textures, Shaders, Sprites   # material/texture/shader đang dùng
└── Scenes/                                  # scene gameplay 3D
```

`CODE-RULE.md` §1 viết đường dẫn là `Assets/Scripts/`; project này đặt cả cây dưới root `Assets/_ZombieWar/`, cấu trúc con và luật chia hệ thống giữ nguyên. Scene gameplay tạo trong `Assets/_ZombieWar/Scenes/`.

Thư mục còn rỗng giữ bằng `.gitkeep` (Unity bỏ qua file bắt đầu bằng dấu chấm, không sinh `.meta`) — xoá khi thư mục có asset thật.

Một assembly definition duy nhất tách code first-party khỏi `Assembly-CSharp` của các asset pack: `ZombieWar` (`Assets/_ZombieWar/Scripts/ZombieWar.asmdef`, runtime, reference Unity.InputSystem / Cinemachine / Unity.AI.Navigation / Unity.TextMeshPro / UnityEngine.UI). DOTween là DLL trong `Assets/Plugins/` nên auto-reference, không cần khai báo. Thêm package mới cần dùng trong script thì bổ sung vào `references` của `ZombieWar.asmdef`. Không bao giờ đặt code vào `Assets/ThirdParty/JMO Assets`, `Assets/ThirdParty/Layer Lab`, `Assets/Plugins`.

**Không giữ editor tool sinh object** (quyết định user 2026-09-05): project **không có** thư mục `Scripts/Editor/` hay assembly `ZombieWar.Editor`. Mọi script kiểu builder/installer/importer/converter (dựng `UIRoot.prefab` + prefab popup, wire `GameplayRoot`, sinh `Skill_*.asset`, build Animator controller từ recipe, convert material, copy asset UI từ pack) đã xoá cùng `Animation/AnimatorBuildRecipe.asset`; muốn tra lại cách chúng dựng thì xem lịch sử git. **Prefab và `.asset` đang có là nguồn sự thật** — chỉnh trực tiếp trong Editor hoặc qua Unity MCP. Khi thật sự cần dựng hàng loạt, viết tool tạm, chạy xong thì xoá trước khi kết thúc task, không commit tool. Chỉ khi cần một editor script *thường trực* (ví dụ build APK headless) mới tạo lại `Scripts/Editor/` + asmdef riêng cho đúng thứ đó.

`Assets/_Recovery/0.unity` là file recovery sau crash của Unity, không phải scene thật — không dựng gì trên nó; xoá khi tiện.

## Hiện trạng hệ thống (cập nhật 2026-09-05)

Toàn bộ runtime nằm trong một prefab **`Assets/_ZombieWar/Prefabs/GameplayRoot.prefab`** (Systems/Managers, Pools, Map, Player, CameraRig, và một instance `UIRoot.prefab`); scene `Gameplay` chỉ chứa một instance của nó.

**Vòng đời một lượt chơi (một scene duy nhất):**

1. Vào scene → `GameFlowController` ở state **`Menu`**: chưa có map, `Player` **tắt** (`LevelMapLoader.Awake` tắt nó), `MenuUiBinder` hiện menu overlay.
2. Bấm PLAY một thẻ level → `MenuUiBinder` → `GameFlowController.StartRun(level)`: `LevelMapLoader.Load(level)` instantiate `LevelDefinitionSO.MapPrefab` vào `Map/`, bật `Player` và đặt tại `LevelMap.PlayerSpawn`, cắt blend camera (`PreviousStateIsValid = false`); phát `OnRunStarted` để `WaveDirector` nạp phase và `ZombieManager` prewarm pool; chuyển state `Countdown`.
3. Retry / Next / Main Menu → `LevelLoader` **reload chính scene đó**. `Data/Levels/LevelSelection.asset` (`LevelSelectionSO`) mang level + cờ `AutoStart` qua lần reload: có cờ thì `GameFlowController.Start()` vào thẳng `StartRun`, không có thì về menu. `ConsumeAutoStart()` xoá cờ ngay khi đọc để lần Play sau trong Editor không tự nhảy vào trận.

Reload scene là cách rẻ nhất để pool, physics và map chắc chắn sạch giữa hai lượt; chỉ "bấm PLAY từ menu" là khởi động tại chỗ vì lúc đó chưa có gì để dọn.

⚠️ **Prewarm pool zombie phải xảy ra sau khi map tồn tại** — `NavMeshAgent` bật lên khi chưa có NavMesh sẽ log "Failed to create agent because there is no valid NavMesh" và không bao giờ bám mesh. Vì vậy `ZombieManager` tạo pool trong `OnRunStarted`, không phải `Awake`.

Map prefab (`Prefabs/Environment/Map_FlatOutpost.prefab`, `Map_BurningHills.prefab`) có root `LevelMap` chứa: `Environment` (đất, tường biên, vật cản), `Directional Light`, `PlayerSpawn`, và `NavMeshSurface` (collect **Children**, data bake lưu ở `Prefabs/Environment/NavMesh_Map_*.asset`). Sửa map xong phải bake lại: đặt prefab vào scene, `BuildNavMesh()`, `AssetDatabase.CreateAsset` đè lên file data cũ, save prefab. Thêm level mới = thêm map prefab + `LevelDefinitionSO` rồi kéo nó vào mảng `_levels` của `MenuUiBinder` trong `GameplayRoot.prefab` — trang Battle là carousel một thẻ nên không cần thêm slot UI, không đụng code.

Camera portrait: vcam pitch 72°, follow offset (0, 14, −4.5); `CameraAspectAdapter` giữ **FOV ngang** cố định (40°) và suy ra FOV dọc theo aspect (clamp 45–70°), nên 9:16 / 9:19.5 / 3:4 đều thấy cùng bề rộng làn. Spawn ring đã nới lên 14–20 m (min 12) vì màn dọc nhìn xa về phía trước; `_navMeshSampleRadius = 5` ở cả hai level để spawn được khi player đứng trên plateau. UI: Canvas Scaler reference 1080×1920, match 0.5. Vcam **follow `Player/CameraTarget`** (child rỗng) chứ không follow `Player`: `Core/CameraLookAhead` trên `CameraRig` đẩy target về phía joystick tối đa `_maxOffset` 1 m (SmoothDamp 0.4 s) — xem "Game feel".

Rung camera: Player có **ba** `CinemachineImpulseSource` — súng dùng source shape **Recoil** 0.12 s (`WeaponController._impulseSource`), bom dùng source shape Bump 0.2 s (`BombThrower._impulseSource`), bị cắn dùng source Bump 0.15 s (`PlayerHitReactionPresenter._impulseSource`); lực lấy từ `_cameraImpulse` trong `GunDefinitionSO` (rifle 0.035, shotgun 0.07) / `BombDefinitionSO` (0.55) / `PlayerDefinitionSO._hitCameraImpulse` (0.2). Mọi impulse đổ về một `CinemachineImpulseListener` trên `VCam_Follow`; `Core/CameraShakeController` (trên `CameraRig`) là công tắc duy nhất: đọc `SaveService.CameraShakeEnabled` (PlayerPrefs `zw_camera_shake`) và ghi `listener.m_Gain` = `_enabledGain` hoặc 0. Toggle "SCREEN SHAKE" nằm trong popup Pause (`SwitchToggleView`), nối qua `GameplayUiBinder` → `UIManager.ShowPausePopup(..., shakeEnabled, onShakeChanged)`.

| Hệ thống | Script chính | Ghi chú |
|---|---|---|
| Flow | `Core/GameFlowController` (Countdown→Playing→Paused/LevelUp/Won/Lost), `LevelTimer`, `ScoreTracker`, `SaveService` (PlayerPrefs) | Entry point duy nhất mỗi scene; UI chỉ subscribe event |
| Player | `Player/PlayerMotor` (Rigidbody), `PlayerAim` (OverlapSphereNonAlloc + Linecast LOS, hold 0.35 s, đổi target chỉ khi ứng viên tốt hơn `_targetSwitchMargin` 25%), `PlayerHealth`, `PlayerAnimationPresenter`, `PlayerHitFlash` (MPB, property `_Color` vì material Autodesk Interactive), `PlayerDeathPresenter` (tween ngã, vì không có clip death), `PlayerHitReactionPresenter` (máu + impulse + jolt khi bị cắn) | Input: `PlayerInputReader` đọc action `Player/Move`; joystick là On-Screen Stick `<Gamepad>/leftStick` |
| Weapons | `Weapons/WeaponController` (FSM Ready/Firing/Cooldown/Reloading/Switching), `Gun` (stats + motion recoil/reload/switch thủ tục), `ProjectileManager` (SphereCast, pool 180), `BombThrower` + `Bomb` (Rigidbody, fuse, telegraph ring thở, trail, falloff, event `OnExploded`) | Gun model gắn dưới `hand_r/GunSocket`. **Nòng được nắn lại mỗi frame** trong `WeaponController.LateUpdate` (sau Animator): `Gun.AlignBarrelAt` xoay root súng cho nòng nằm ngang và chĩa vào target trong giới hạn `_aimYawLimitDegrees` ±25° quanh hướng thân (không target thì theo hướng thân) — vì chu kỳ chạy lắc xương chậu làm tay cầm súng lệch tới 13° yaw / −10° pitch so với thân (đo 2026-09-05; đứng yên lệch 0°). Đạn vẫn bay theo `Muzzle.forward` đúng spec §5.2, giờ nòng đã đúng chỗ |
| Enemies | `Enemies/ZombieManager` (pool theo `ZombieDefinitionSO`, registry Collider→zombie, tick tập trung, tên layer xác), `ZombieController` (FSM Spawning/Chase/Attack/HitStun/Knockback/Dying; chết vì bom thì xác bay trên layer `Corpse`), `ZombieAnimationPresenter` (`HitSide`), `ZombieMaterialFx` (MPB `_HitAmount`/`_DissolveAmount`) | Shader `Art/Shaders/ZombieDissolve.shader` (HLSL URP, có ShadowCaster/DepthOnly) |
| Feel | `Core/HitStopController`, `Core/HitVignettePresenter`, `Core/CameraLookAhead`, `Volume` + `Art/Rendering/GameplayPostProcess.asset` | Xem "Game feel" bên dưới |
| Level | `Level/LevelMapLoader` + `LevelMap` (map prefab, spawn, NavMesh), `WaveDirector` (phase, cap, weighted pick, scripted Giant @145 s, anti-spike), `SpawnPointResolver`, `FireHazardSpawner` + `FireZone` (P1, Level 2 @75 s/@120 s) | Level 2: plateau 3.5 m + 4 dốc 22° |
| UI | `UI/UIManager` (hub), `UI/Hud/*View`, `UI/Popup/{PopupBase,PopupManager,PopupBackdrop,PausePopupUI,ResultPopupUI,SkillChoicePopupUI,SkillCardView}`, `UI/MainMenu/{MenuScreenView,MenuHeaderView,MenuTabBarView,MenuTabButtonView,BattlePageView,LevelCardView,WeaponPageView,WeaponListItemView,WeaponDetailView,WeaponStatRowView}`, `SwitchToggleView`, `SafeAreaFitter`, `UiButtonFx`, `TimeTextFormatter` (zero-alloc); ref gameplay nằm ở `Core/GameplayUiBinder` + `Core/MenuUiBinder` | Mọi màn hình trong `Prefabs/UI/UIRoot.prefab` — xem "Kiến trúc UI" |
| Roguelike | `Roguelike/RoguelikeDirector` (XP → battle level → draft), `BattleXpTracker`, `SkillDraft`, `Player/PlayerStatSheet`, `Data/{PassiveSkillSO,RoguelikeSettingsSO,StatId,StatModifier}` | Xem "Roguelike trong trận" bên dưới |
| Meta | `Core/ProfileService` (level/XP/coin + cấp nâng cấp từng súng, lưu qua `SaveService`), `Data/ProgressionRulesSO`, phần `Upgrade` của `GunDefinitionSO` (`GetStats(level)` → struct `GunStats`, `GetUpgradeCost`) | Xem "Meta progression" bên dưới |
| Data | `Data/*SO` + instance trong `Assets/_ZombieWar/Data/{Player,Rules,Weapons,Zombies,Waves,Levels,Roguelike}` | Số liệu gốc chép từ spec §5–§10; chỉnh ở đây, không sửa code. **Đã lệch spec theo yêu cầu user 2026-09-05:** cap zombie mỗi phase +50% và spawn interval −25% (L1 18/33/48/66/80, L2 24/39/57/75/90), `_moveSpeed` giảm 25% (Walker 1.7, Runner 2.7, Brute 1.25, Giant 1.1; ngưỡng blend animator 1.4/2.3/3.6 m/s giữ nguyên nên Walker chạy clip Walk, Runner blend WalkFast→Run), pool prewarm Walker/Runner/Brute 64/48/22, và `PlayerDefinition._anglePenaltyWeight` 0.35 → 0.02 để auto-aim ưu tiên zombie gần nhất (góc chỉ phá hoà); bù lại thêm `_targetSwitchMargin` 0.25 (ngoài spec) để trong đám đông nhân vật không quay qua lại giữa hai con gần ngang nhau |

### Roguelike trong trận (ngoài spec P0, thêm 2026-09-05 theo yêu cầu user)

Tài liệu đầy đủ: **`docs/ROGUELIKE-SYSTEM.md`** (thiết kế 6 skill, bảng stat → nơi tiêu thụ, trường hợp biên, quy trình thêm skill mới). Phần dưới đây là bản rút gọn.

Giết quái nhận XP → đầy thanh thì lên **battle level** (chỉ sống trong một lượt chơi, khác hẳn level tài khoản của Meta progression) → hiện popup chọn 1 trong 3 passive skill. Data ở `Data/Roguelike/` (`RoguelikeSettings.asset` + 6 `Skill_*.asset`), XP mỗi loại quái ở `ZombieDefinitionSO._xpReward` (Walker 10 / Runner 14 / Brute 30 / Giant 90).

**Passive = thuần stat modifier.** `PassiveSkillSO` chỉ chứa danh sách `StatModifier { StatId, Additive|Multiplicative, giá trị mỗi cấp }`. `PlayerStatSheet` (trên `Player`) gộp mọi stack đang sở hữu thành 12 stat sống: multiplicative nghỉ ở 1, additive nghỉ ở 0. Vì vậy **thêm skill thứ 7 = thêm một `.asset`, không sửa dòng code nào** (`CODE-RULE.md` §7 Open/Closed). Hệ quả: nếu cần một hiệu ứng không biểu diễn được bằng stat (ví dụ nổ dây chuyền khi kill) thì phải thêm `StatId` mới + đúng một chỗ đọc nó.

Nơi tiêu thụ từng stat — đây là danh sách đầy đủ, thêm `StatId` mới thì bổ sung vào đây:

| StatId | Đọc ở đâu |
|---|---|
| `WeaponDamage`, `FireRate`, `ReloadSpeed`, `Knockback`, `ProjectilePierce` | `WeaponController.Fire`/`BeginReload` → gói vào struct `ShotStats` cho `ProjectileManager`/`Projectile` |
| `MoveSpeed` | `PlayerMotor.FixedUpdate` |
| `MaxHealth`, `HealPerKill` | `PlayerHealth` (`HandleStatsChanged` cộng luôn máu vào, `Heal`) — heal/kill do `RoguelikeDirector` gọi |
| `BombCharges`, `BombRadius`, `BombDamage` | `BombThrower` (`HandleStatsChanged`, `RequestThrow`, `Explode`) |
| `XpGain` | `RoguelikeDirector.HandleZombieKilled` |

⚠️ **Thứ tự nhân với Meta progression**: hệ số roguelike nhân **lên trên `gun.Stats`** (đã bao gồm cấp nâng cấp mua bằng coin), không nhân lên `definition`. Nhân nhầm chỗ là vô hiệu hoá toàn bộ shop.

**Vòng đời một lần level-up**: `RoguelikeDirector.Update` thấy có nợ level-up (và flow đang `Playing`) → `SkillDraft.Roll` bốc N thẻ **khác nhau**, bỏ skill đã max → `OnChoiceOffered` → `GameplayUiBinder` dựng `SkillCardData` rồi gọi `GameFlowController.PauseForLevelUp()` (state **`LevelUp`**, `timeScale = 0`) và `UIManager.ShowSkillChoicePopup`. Người chơi chạm thẻ → popup `Close()` → `OnClosed` → `RoguelikeDirector.ChooseOffer(index)` cộng stack, rebuild stat sheet. Còn nợ thì mở luôn bộ thẻ kế tiếp; hết nợ mới `OnChoiceClosed` → `ResumeFromLevelUp()`.

- Director **không tự dừng game**: nó chỉ phát event, flow mới đổi state. Nhờ vậy `PlayerMotor`/`WeaponController`/`WaveDirector`/`ZombieManager` tự đứng im vì đều gate sẵn trên `State == Playing`, không phải sửa gì.
- `LevelUp` là state riêng chứ không mượn `Paused`, nếu không `GameplayUiBinder` sẽ mở popup Pause đè lên.
- Popup chọn skill tắt cả `_closeOnBackKey` lẫn `_closeOnBackdropClick` — bắt buộc phải chọn.
- **Layout thẻ**: 3 cột dọc xếp ngang (tên → badge NEW → icon → mô tả → hàng sao), theo mẫu UX user đưa. Hàng sao thay cho chữ "LV 3/5": số sao vàng = cấp **sau khi chọn**, tổng số sao = `MaxStacks`, nên nhìn là biết skill còn sâu bao nhiêu. Prefab author sẵn **5 slot sao** (trần sâu nhất trong pool); skill nông hơn ẩn bớt và `SkillCardView.DrawStars` dịch cả hàng lại cho vẫn cân giữa — thêm skill có `MaxStacks > 5` thì phải thêm slot sao vào mảng `_stars` của cả ba `SkillCardView` trong `SkillChoicePopup.prefab`, view sẽ log error nếu quên. Badge NEW hiện khi `SkillCardData.IsNew` (tức chưa sở hữu stack nào).
- Mô tả skill phải **ngắn, mỗi dòng một hiệu ứng** ("Damage +20%
Knockback +15%") vì cột chỉ rộng 300 px — văn xuôi dài sẽ tràn.
- Mọi skill đã max thì `SkillDraft.Roll` trả 0, director xoá nợ và không hỏi nữa.

Đường cong XP: `base 80 × 1.35^(level-1)`, trần battle level 12. Đo bằng nhịp kill thật của L1 (~280 kill/lượt ≈ 4090 XP) thì một lượt đi hết được **battle level 10, tức 9 lần chọn skill** — khoảng 20 giây một lần. Tổng max stack của 6 skill là 24 > 12 nên không bao giờ full được, lựa chọn luôn có sức nặng.

Wiring roguelike trong `GameplayRoot.prefab` (gán tay, không còn tool): `PlayerStatSheet` trên `Player`; `RoguelikeDirector` trên `Systems/Managers` với `_settings` = `Data/Roguelike/RoguelikeSettings.asset`, `_flow`, `_zombies`, `_stats`, `_playerHealth`; `_stats` nối cho `PlayerHealth`/`PlayerMotor`/`WeaponController`/`BombThrower` và `_rogue` cho `GameplayUiBinder`. Thêm skill mới = tạo asset qua `Create ▸ Zombie War ▸ Passive Skill` rồi kéo vào `_skillPool` của `RoguelikeSettings.asset`. Icon skill là `Item_Icon_*` 256 px của Sci-Fi Survival đã copy vào `Art/UI/Sprites/Icons/` — icon thuộc về skin UI, đổi skin thì phải với tới cả `_icon` của các asset skill cũ.

### Meta progression (ngoài spec P0, thêm 2026-09-05 theo yêu cầu user)

Spec docx cố ý không có metagame; user đã quyết định thêm sau khi P0 xong. Toàn bộ state nằm trong **`Core/ProfileService`** (trên `Systems/Managers`), số liệu tĩnh ở `Data/Rules/ProgressionRules.asset` (`ProgressionRulesSO`: XP/kill, bonus thắng, coin theo điểm, đường cong XP lên cấp, coin khởi điểm) và mục `Upgrade` của mỗi `GunDefinitionSO` (max level, % damage/băng đạn/tốc bắn/nạp đạn mỗi cấp, giá gốc và hệ số tăng giá).

- **Lưu**: PlayerPrefs qua `SaveService` (`zw_player_level`, `zw_player_xp`, `zw_coins`, `zw_gun_level_<id>`, `zw_gun_unlocked_<id>`). `ProfileService.EnsureLoaded()` đọc lười nên gọi từ `Awake` của hệ khác vẫn an toàn.
- **Áp vào gameplay**: `Gun.Stats` (`GunStats`: Damage/FireInterval/MagazineSize/ReloadDuration) do `WeaponController.ApplyUpgrades()` gán trong `Awake` và khi `OnRunStarted`; `WeaponController`/`ProjectileManager` đọc `gun.Stats`, không đọc thẳng `definition` cho bốn chỉ số này. Range, spread, pellet, VFX vẫn lấy từ definition.
- **Thưởng**: `GameFlowController.EndLevel` → `ProfileService.GrantRunRewards(kills, total, won)`; `LevelResult`/`ResultData` mang `CoinsEarned`/`XpEarned`, popup kết quả có hai hàng "COINS EARNED" / "XP EARNED".
- **Nâng cấp** chỉ qua `ProfileService.TryUpgradeGun(gun)` (kiểm tra unlock, max level, đủ coin); `OnChanged` báo cho `MenuUiBinder` vẽ lại header + trang vũ khí.

### Game feel (thêm 2026-09-05, ngoài spec P0)

Lớp cảm giác thuần presentation: logic gameplay chỉ phát event, các presenter dưới đây nghe và diễn. Số liệu nằm ở `Data/Rules/FeedbackProfile.asset` (nhóm Player / Hit Stop) hoặc field serialize trên chính component — không có số hình thức nào trong code.

| Thứ | Ở đâu | Việc |
|---|---|---|
| `Core/HitStopController` | `Systems/Managers` | Đóng băng / slow-mo theo **unscaled** time: bom nổ trúng ≥1 zombie (`BombThrower.OnExploded`; 0.07 s × timeScale 0.12), quái nặng chết (`ZombieDefinitionSO._deathHitStopDuration`: Brute 0.04, Giant 0.1, Walker/Runner 0 × 0.1), player chết (slow-mo 1.4 s × 0.25, chạy trong state `Lost` để zombie sau popup còn nhúc nhích chậm). Chỉ nhận yêu cầu ở `Playing`/`Lost`; khi flow đổi state nó buông và **chỉ trả `timeScale` về 1 nếu giá trị hiện tại vẫn là của nó** — Pause/LevelUp ghi 0 luôn thắng, Retry reload scene nên `Awake` của flow reset |
| Post-process | `Volume` global trên `CameraRig`, profile `Art/Rendering/GameplayPostProcess.asset` (Tonemapping Neutral; ColorAdjustments contrast 8 / saturation 12; Bloom threshold 1 / intensity 0.45 / scatter 0.6 / HQ off; Vignette đen 0.2 / smoothness 0.45); `Main Camera` bật `renderPostProcessing` | Bloom là thứ làm muzzle, tracer, nổ bom và viền dissolve (đều HDR additive) toả sáng. Máy yếu thì tắt Bloom trong profile; chỉ Vignette là bắt buộc vì presenter dưới cần nó |
| `Core/HitVignettePresenter` | `CameraRig` | Đọc `Vignette` từ `Volume.profile` (bản copy runtime, không bẩn asset). Spike lên `_playerHitVignetteIntensity` 0.45 khi `PlayerHealth.OnDamaged` rồi tắt dần trong `_playerVignetteDuration` 0.18 s (spec §12.1); dưới `_lowHealthFlashThreshold` thì thở theo sin tới `_lowHealthVignetteIntensity` 0.38 với tần số `_lowHealthPulseFrequency` 1.2 Hz. Chạy unscaled, chỉ ghi khi giá trị đổi |
| `Core/CameraLookAhead` | `CameraRig` | Vcam follow child `Player/CameraTarget`; script đặt target = player + offset SmoothDamp theo `PlayerMotor.WorldMoveDirection × NormalizedSpeed` (`_maxOffset` 1 m, `_smoothTime` 0.4). Đặt `_maxOffset = 0` là về camera tâm |
| `Player/PlayerHitReactionPresenter` | `Player` | Khi bị cắn: phát `PlayerDefinitionSO._hitVfx` (`Vfx_PlayerBlood`) tại `AimOrigin` quay theo hướng đẩy, impulse camera riêng (`_hitCameraImpulse`), `DOPunchRotation` `ModelPivot` nghiêng ra xa kẻ cắn (`_joltDegrees` 12°, 0.22 s). Bỏ qua jolt khi đòn đó giết; `PlayerDeathPresenter` `DOKill` pivot trước khi tween ngã nên hai tween không giằng nhau |
| `Weapons/Gun` motion | prefab `Gun_*` | Recoil vừa lùi vừa hất nòng lên (`_recoilKickDegrees` 5°); reload không có clip nên súng chúi xuống + hạ thấp theo một cung sin dài đúng `ReloadDuration` (`_reloadTiltDegrees` 35°, `_reloadDipDistance` 0.05, `WeaponController.BeginReload` gọi `BeginReloadMotion`); đổi súng pop scale 0.7→1 OutBack 0.2 s. Chỉ ghi transform khi có chuyển động (`_pivotDirty`); `SetVisible(false)` reset về pose nghỉ đã cache ở `Awake` |
| `Weapons/Bomb` | prefab `Bomb` | `TrailRenderer` (material `Tracer`, 0.3 s, gradient cam → đỏ sẫm) `Clear()` khi lấy/trả pool để không vệt từ vị trí pool; vòng telegraph thở (`_telegraphPulseAmplitude` 0.08, `_telegraphPulseFrequency` 5 Hz) trong `TelegraphLead` cuối |
| Xác zombie bay | `ZombieController.Die(direction, launchForce)` | Đòn kết liễu có lực ≥ `_physicsKnockbackThreshold` (tức bom; Giant threshold 999 nên không bay) giữ Rigidbody dynamic, **bật lại collider** và chuyển root sang layer **`Corpse`(14)** rồi `AddForce` cùng công thức knockback — xác bay theo sóng nổ, đáp đất, dissolve như thường rồi `RestoreBody()` trả layer + kinematic khi despawn. Corpse va chạm với Default/Obstacle/Ground/Prop, **không** với Player/Enemy/Bomb/Corpse; các mask `_hitMask`/`_enemyMask`/`_blastMask`/`_burnMask` không chứa nó nên đạn, auto-aim, bom sau và lửa nhìn xuyên qua xác. Tên layer khai ở `ZombieManager._corpseLayerName` |
| Flinch theo hướng | `ZombieAnimator` layer Hit Reaction | Tham số int `HitSide` (`Enemies/HitSide`: Front 0 / Left 1 / Right 2, tính bằng dot(`transform.right`, hướng đạn) với ngưỡng 0.5 — đạn bay về phía phải zombie là trúng sườn trái) chọn `Hit` (HitReact_Head, speed 3) / `HitLeft` / `HitRight` (HitReact_LeftArm/RightArm 3 s, speed 4). Transition Empty→state có 2 điều kiện `Hit` + `HitSide Equals`; không tự retrigger khi đang flinch |
| NavMeshAgent | 4 prefab `Zombie_*` | `autoBraking = false`: agent không hãm tốc khi tới gần đích (đích chính là player) nên zombie lao thẳng vào tầm cắn thay vì trôi tới |

Wiring bằng tay nếu phải dựng lại (không có tool): layer `Corpse` ở slot 14 + tắt 4 ô ma trận Corpse×{Player,Enemy,Bomb,Corpse}; `VolumeProfile` với 4 override trên; `CameraRig` gắn `Volume` (global, profile) + `HitVignettePresenter` (`_volume`, `_health`, `_feedback`) + `CameraLookAhead` (`_motor`, `_player`, `_followTarget`); child rỗng `Player/CameraTarget` và trỏ `VCam_Follow.Follow` vào nó; `Player` gắn `PlayerHitReactionPresenter` (`_health`, `_definition`, `_modelPivot`, `_bloodOrigin` = `AimOrigin`, `_vfx`, `_impulseSource` = source Bump 0.15 s riêng); `Systems/Managers` gắn `HitStopController` (`_flow`, `_feedback`, `_bombs`, `_zombies`); `Bomb.prefab` thêm `TrailRenderer` kéo vào `_trail`; `PlayerDefinition._hitVfx` = `Vfx_PlayerBlood`. Muốn bỏ một hiệu ứng thì gỡ component tương ứng, không cần sửa code.

## Kiến trúc UI

Kiến trúc uGUI + TextMeshPro + DOTween, không dùng UI Toolkit / MVVM / DI. Canvas tách theo tần suất rebuild, popup chạy qua stack, và **UI không giữ reference gameplay** — mọi ref bắc cầu nằm ở binder (xem bảng bên dưới).

**Toàn bộ UI nằm trong một prefab: `Assets/_ZombieWar/Prefabs/UI/UIRoot.prefab`.** Nó chứa mọi màn hình (menu, HUD, countdown, popup layer), kèm `EventSystem` và `AudioSource` riêng — nghĩa là **không có reference nào từ trong prefab trỏ ra ngoài**. `GameplayRoot.prefab` đặt một instance tên `UIRoot` bên trong nó; menu và HUD là hai canvas của cùng prefab đó, bật/tắt theo state của `GameFlowController`.

```
UIRoot.prefab               [UIManager]        ← hub duy nhất, không giữ ref gameplay
├── EventSystem             [EventSystem, InputSystemUIInputModule]
├── UiAudio                 [AudioSource]      ← UI tự phát tiếng tap của mình
├── Canvas_HUD      order 0   → TopBar (timer/kills/score/pause + health bar), Joystick (neo giữa đáy (0.5, 0), y = 300 — user đổi 2026-09-05, trước đó ở góc trái), GunButton, BombButton
├── Canvas_Menu     order 5  [MenuScreenView]  → Header [MenuHeaderView] (level + XP bar, coin, gear) + Pages/{Page_Shop, Page_Weapon [WeaponPageView], Page_Battle [BattlePageView], Page_Talent, Page_Locked} + TabBar [MenuTabBarView] 5 tab (tab LOCK khoá)
├── Canvas_Countdown order 10 [CountdownView]  ← không GraphicRaycaster, không chặn input
└── Canvas_Popup    order 20 [PopupManager]    → Backdrop + PausePopup + ResultPopup + SkillChoicePopup + WeaponDetailPopup + SettingsPopup (prefab, inactive)
```

### Binder — lớp trung gian giữ ref gameplay

UI **không** được giữ reference tới hệ thống gameplay. Mọi reference bắc cầu nằm trong binder sống ngoài prefab UI, và **chỉ trỏ vào trong** UI (không bao giờ ngược lại):

| Binder | Ở đâu | Giữ ref | Việc |
|---|---|---|---|
| `Core/GameplayUiBinder` | `GameplayRoot/Systems/Managers` | `UIManager`, `GameFlowController`, `PlayerHealth`, `WeaponController`, `BombThrower`, `CameraShakeController` | subscribe event gameplay → gọi setter của `UIManager`; đưa `LevelResult` thành `ResultData`; trao lệnh (`Pause`, `RequestSwitch`, `RequestThrow`, `Resume`, `Retry`, `GoToMenu`) cho UI qua `BindGameplayCommands` |
| `Core/MenuUiBinder` | `GameplayRoot/Systems/Managers` | `UIManager`, `GameFlowController`, `ProfileService`, `LevelDefinitionSO[]` | khi flow vào state `Menu`: dựng `MenuHeaderData` + `LevelCardData[]` + `WeaponEntryData[]` (chỉ số hiện tại và sau nâng cấp) rồi gọi `ShowMenuScreen`; PLAY → `GameFlowController.StartRun(level)`; UPGRADE → `ProfileService.TryUpgradeGun`; `ProfileService.OnChanged` → `RefreshMenu` |

Bố cục menu bám theo ba ảnh tham chiếu user đưa 2026-09-05 (kiểu tower-defense casual), khoác skin Sci-Fi Survival:
- **Header** (`MenuHeaderView`): vương miện + "LEVEL n" + thanh XP "xp/next" bên trái, chip coin, nút bánh răng bên phải → `SettingsPopupUI` (toggle SCREEN SHAKE, cùng `SwitchToggleView` với popup Pause; `MenuUiBinder` giữ `CameraShakeController` để nối).
- **Battle** (`BattlePageView` + `LevelCardView`): segmented NORMAL/HARD ở trên (HARD khoá, thuần visual, chưa có data), tiêu đề "CHAPTER n", khung artwork 520² (`LevelDefinitionSO._artwork`, rỗng thì giữ placeholder icon castle), hai mũi tên hai bên, tên level + chip best score, nút START to ở đáy, counter "i / n". Mặc định đứng ở chapter xa nhất đã mở.
- **Weapon** (`WeaponPageView` + lưới `WeaponCardView` 3 cột, 6 slot author sẵn trong mảng `WeaponPageView._cards`): banner "UNLOCKED", mỗi thẻ có pill tên, icon trong khung, "LEVEL n", thanh tiến độ "n/max"; súng chưa mở hiện khoá + "COMING SOON". Bấm thẻ → **`WeaponDetailPopupUI`** (prefab `Popups/WeaponDetailPopup`): tên, icon to, level, mô tả (`GunDefinitionSO._description`), lưới 2×3 `WeaponStatCellView` (ATTACK/RATE/MAGAZINE/RELOAD kèm giá trị sau nâng cấp màu xanh, SHOTS = pellet, TYPE = SINGLE/SPREAD), giá coin, nút UPGRADE, dòng hint ("NEEDS n MORE COINS" / "MAX LEVEL REACHED" / "LOCKED"). `UIManager` giữ mảng `WeaponEntryData` đang hiển thị và index popup đang mở để `RefreshMenu` sau nâng cấp vẽ lại cả popup.
- **Shop**/**Talent** placeholder "COMING SOON"; tab thứ 5 là **LOCK** (icon khoá, `MenuTabButtonView._locked`, không có badge riêng vì `_lockBadge` là optional). Tab mặc định là Battle (`MenuScreenView._defaultTab`). Trang không phải Battle được author inactive nên `Awake` của view chạy lần đầu khi mở tab; `Bind` chỉ đụng field serialized nên gọi trước `Awake` vẫn đúng.

- `UIManager` chỉ phơi ra setter (`SetHealth`, `SetScore`, `SetAmmo`…), lệnh mở popup, và `BindGameplayCommands` / `ShowMenuScreen(cards, onSelected)`. Nó không biết `GameState`, không biết `LevelDefinitionSO`, không biết `AudioService`.
- Dữ liệu vào UI là **struct trình bày**: `UI/ResultData`, `UI/LevelCardData`. Popup và thẻ level không bao giờ nhận object gameplay.
- View HUD (`HealthBarView`, `TimerView`, `ScoreView`, `GunHudView`, `BombButtonView`, `CountdownView`) là presenter câm; nút nối runtime bằng `Init(callback)` / `Setup(callbacks)`, không có persistent onClick nào trên prefab.
- Toggle dạng công tắc dùng `UI/SwitchToggleView` bọc `Toggle` uGUI: `Toggle` chỉ fade được một graphic, trong khi switch của Layer Lab là frame + handle mỗi trạng thái, nên view bật/tắt hai nhánh visual `On`/`Off`. Sprite switch lấy từ `Sprite/Component/Toggle/` của pack (đã copy vào `Art/UI/Sprites/Toggles/`); kích thước track/handle chép theo chính prefab của pack (222×83 và 147×91).
- Popup mở/đóng chỉ qua `PopupManager` (stack + draw order + backdrop dùng chung + phím Back qua `Keyboard.escapeKey`). Mọi đường đóng đi qua `PopupBase.OnClosed` nên callback không thể bị bỏ sót. Tween popup dùng `SetUpdate(true)` vì mở khi `timeScale = 0`.
- **Health bar chạy bằng anchor** (`DOAnchorMax`), không `Image.Type = Filled` — sprite pill 9-slice sẽ bị cắt cụt đầu bo nếu dùng Filled, và anchor đọc đúng ngay frame đầu khi `CanvasScaler` chưa kịp size canvas.
- **CanvasScaler match theo WIDTH** (`matchWidthOrHeight = 0`, reference 1080×1920) ở mọi canvas. Popup cao gần bằng design height tự co lại bằng `PopupBase._fitPadding`.
- `AudioService` **không còn** `PlayUi`/`_uiVoice`: tiếng UI thuộc về UI (scene menu không có `AudioService` để mượn).

### Art UI (GUI PRO Kit - Sci-Fi Survival)

**Toàn bộ art UI đến từ đúng một pack: `Assets/ThirdParty/GUI PRO Kit - Sci-Fi Survival/`** (yêu cầu user 2026-09-05 — không lấy sprite/font UI ở bất kỳ pack nào khác). **Chỉ asset thực dùng mới được copy** sang `Assets/_ZombieWar/Art/UI/` (`Fonts/`, `Sprites/{Buttons,Frames,Popups,Sliders,Labels,Icons,Toggles,Backgrounds}` — 83 file) bằng copy trong Editor (`AssetDatabase.CopyAsset` / Ctrl+D trong Project window, không copy bằng file system) để giữ nguyên import settings, đặc biệt là **spriteBorder 9-slice**. Font UI là **Oxanium ExtraBold** (title/số) + **Ubuntu Bold** (body), cả hai là TMP SDF có atlas nằm trong chính file `.asset`.

Ngoại lệ duy nhất trong `Art/UI/`: 2 icon súng ở `Art/Sprites/Icon_*.png` là **AssetPreview render từ chính model súng của game**, không phải art của pack nào.

⚠️ Bẫy của pack, đều đã trả giá:
- **Rất nhiều sprite có border phủ hết kích thước texture** (ví dụ `Popup_Frame01_Navy1` 441×709 border 222/352/219/357). Đó là chủ ý: vùng giữa rộng 0 pixel nên khi kéo giãn nó lấy màu tại đúng cột/hàng biên. Cứ để `Image.Type = Sliced`, `pixelsPerUnitMultiplier = 1`, kích thước tuỳ ý. Khi chọn sprite mới phải **kiểm tra cột được lấy mẫu có đặc không** — với fill của slider thì cột đó phải opaque, không thì thanh máu sẽ trong suốt ở giữa.
- `Frame_BarFrame_Top01_Navy` / `Frame_BarFrame_Top02_*` / `Frame_BarFrame_Bottom01_Navy` có border **dọc bằng nguyên chiều cao** — chúng là dải mảnh vẽ sát mép màn hình, kéo cao ra sẽ hỏng. Panel/bar cao dùng `Frame_TableFrame01_Navy` (top bar, header) và `Frame_BannerFrame04_Navy01` (tab bar, thẻ vũ khí) — hai cái này slice được cả hai chiều.
- `Btn_TextButton_Square01_*` (174×188) **không** slice theo chiều dọc — giữ chiều cao nút quanh 140–190 để bo góc không bị kéo méo.
- Pictogram `Icon_FunctionIcons(x2)` **chỉ giải nén sẵn bản 64 px** (128/256/512 còn nằm trong `.unitypackage`). Chỗ nào vẽ icon lớn hơn ~120 px thì lấy `Icon_ItemIcons(x2)/Shadow_256` thay vì phóng to pictogram: bom trên HUD, artwork level, 6 icon skill roguelike đều đang làm vậy.
- `Icon_Gold` là **vòng tròn rỗng phẳng** — chỉ đọc ra "đồng xu" khi tint màu `Warning`. Tint trắng là ra một chấm trắng vô nghĩa.
- `Label_Tag01_SkyBlue` gần như trắng, nên chữ trên nó phải là `Ink` (dùng cho banner LEVEL UP); các tag còn lại (Green/Red/Blue) dùng chữ `Paper`.
- Nút hành động trong trận (`Play_Joystick_*`) là **ảnh vuông**, khác với sprite tròn có bóng của pack cũ — rect phải vuông, đừng bù offset cho bóng.

**Prefab UI là nguồn sự thật** — các tool `Tools ▸ Zombie War ▸ UI` (import asset, rebuild `UIRoot.prefab`, install vào `GameplayRoot`) đã xoá theo quyết định user 2026-09-05 (xem "Không giữ editor tool sinh object"). Bảng màu và mọi `[SerializeField] private` đều đã gán sẵn trong prefab; không mở public setter chỉ để wire. Quy trình tay khi mở rộng:

- **Thêm sprite/font UI**: copy đúng file cần dùng từ pack sang thư mục loại trong `Art/UI/` bằng Unity (giữ import settings + border 9-slice); font TMP copy xong kiểm tra còn atlas. Không tham chiếu xuyên qua `ThirdParty/`.
- **Thêm level**: `LevelDefinitionSO` mới vào `Data/Levels/` → kéo vào `MenuUiBinder._levels` trong `GameplayRoot.prefab`. Trang Battle là carousel một thẻ nên không cần slot UI.
- **Thêm súng**: `GunDefinitionSO` mới vào `Data/Weapons/` + prefab `Gun` gắn dưới `hand_r/GunSocket` của Player → kéo vào `ProfileService._guns` (giữ thứ tự theo tên) và `WeaponController._guns`. Trang Weapon đã author sẵn 6 `WeaponCardView` trong `WeaponPageView._cards`; quá 6 thì thêm thẻ vào prefab.
- **Wiring `GameplayRoot.prefab` phải giữ**: `GameplayUiBinder` / `MenuUiBinder` trỏ vào `UIManager` của instance `UIRoot`; `LevelMapLoader._virtualCamera`; `ProfileService` (`_rules` = `Data/Rules/ProgressionRules.asset`, `_guns`) nối vào `GameFlowController._profile` / `WeaponController._profile` / `MenuUiBinder._profile`; object `Player` tắt sẵn.

Animator controller (`Animation/Soldier/SoldierAnimator.controller`, `Animation/Zombie/ZombieAnimator.controller`) chỉnh trực tiếp trong cửa sổ Animator (tool build từ recipe đã xoá cùng `AnimatorBuildRecipe.asset`). `SoldierAnimator`: 4 layer — Base Locomotion blend tree 2D, Upper Combat với `UpperBodyMask`, Hit Reaction, Full Body. `ZombieAnimator`: Base blend 1D theo **m/s** Idle/Walk/WalkFast/Run + Attack/Knockback/Death; layer Hit Reaction thân trên ở index 1 (`ZombieAnimationPresenter._hitReactionLayerIndex`). Layer Hit Reaction dùng Override (không Additive) vì clip flinch của pack là full pose. Soldier chưa có clip reload/hit/death trong pack.

Zombie animation lấy từ pack **`Assets/ThirdParty/Zombie_Animations/`** (Humanoid, avatar `T_pose`): `Zombie_Idle_01`, `Zombie_Walk_01/Walk_Fast01/Run_01_Forward_InPlace` (ngưỡng 1.4/2.3/3.6 m/s), `Zombie_Attack01` (state speed 2 vì clip dài 2.33 s), `Zombie_HitReact_Head` cho Hit (mặt trước) và Knockback (speed 3), `Zombie_HitReact_LeftArm/RightArm` cho flinch hai sườn (speed 4), `Zombie_Idle_Death`. Năm clip one-shot đó đã tắt Loop Time trong import settings. Không dùng `Paired_*` (cần victim) và `Crawl_*`. `ZombieController` đẩy tốc độ agent thật (m/s) vào tham số `Speed`; `_attackWindup` 0.45 s cho Walker/Runner/Brute khớp thời điểm vung tay, `FeedbackProfile._deathPoseDuration` 0.6 s để clip ngã đọc được trước khi dissolve.

Layer vật lý đã thêm: `Player`(8) `Enemy`(9) `Obstacle`(10) `Ground`(11) `Prop`(12) `Bomb`(13) `Corpse`(14); ma trận đã tắt va chạm Player–Bomb và Corpse–{Player, Enemy, Bomb, Corpse}. `PlayerSettings.runInBackground = true` để Play mode trong Editor không đứng hình khi cửa sổ mất focus (cần cho test qua MCP).

Còn thiếu / biết trước: nhạc nền (project không có track nào), IK tay trái cầm súng, vỏ đạn văng và decal máu/cháy (spec §12.1, chưa có asset), âm nổ bom đang mượn `AntiMaterialRifle_far_01`, icon súng render từ AssetPreview (`Art/Sprites`). Thư mục `Captures/` (ảnh chụp qua MCP) đã gitignore.

## Build Android (sản phẩm APK)

Player settings đã có: scripting backend **IL2CPP**, kiến trúc **chỉ ARM64**, min SDK **25**, `productName: Zombie War`, `companyName: DucHien`, application id `com.duchien.zombiewar`, orientation **Portrait** khoá cứng. Build scene list: chỉ `Gameplay`.

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
