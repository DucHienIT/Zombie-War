# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## BẮT BUỘC: đọc `CODE-RULE.md` trước

Trước khi viết, sửa, hay review bất kỳ script nào trong project, phải đọc toàn bộ **`CODE-RULE.md`** ở root repo. Đó là bộ quy tắc code bắt buộc (cấu trúc thư mục, naming, kiến trúc data-driven, cấm bootstrap runtime, zero-allocation, SOLID, quy trình compile-check). File CLAUDE.md này chỉ chứa những gì riêng của project và không lặp lại các quy tắc đó. Code vi phạm `CODE-RULE.md` coi như chưa xong.

## Quy tắc ngôn ngữ

- **Mọi thứ trong script** viết bằng **tiếng Anh 100%**: tên class/method/field/biến, string log, comment, tên asset/prefab/scene do code tham chiếu. Không để tiếng Việt lọt vào code, kể cả comment.
- Trả lời người dùng bằng tiếng Việt.

## Project này là gì

**Zombie War** — game bắn súng 3D góc nhìn top-down cho mobile, làm để nộp bài test kỹ thuật, dựng trên một template game casual có sẵn. Code first-party mới chỉ có một Editor tool (`Assets/Scripts/Editor/`, assembly `ZombieWar.Editor`); toàn bộ phần còn lại của `Assets/` là third-party. Scene duy nhất trong build list, `Assets/Scenes/SampleScene.unity`, là scene **2D** mẫu (camera orthographic, `Global Light 2D`) — tạo scene 3D mới cho gameplay, đừng chuyển đổi scene này. Sau khi renderer mặc định đổi sang 3D (xem Stack chính), scene 2D này render sai là bình thường.

## Spec (nguồn sự thật cho gameplay)

Đề bài đầy đủ của khách hàng, bảng nghiệm thu, trục chấm điểm và phân rã hệ thống đề xuất nằm ở **`docs/GAME_SPEC.md`**. Theo `CODE-RULE.md` §8, logic gameplay phải bám đúng spec — không sáng tạo thêm cơ chế, mọi số cân bằng là nút vặn trong ScriptableObject. Đọc spec trước khi bắt đầu bất kỳ tính năng gameplay nào.

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
- **Layer Lab GUI Pro-CasualGame** (`Assets/Layer Lab/GUI Pro-CasualGame/Prefabs/`) — bộ prefab UI (button, frame, popup, slider, label) và một script `UIParticleSystem`. Ghép các prefab này cho HUD/menu.
- **Toony Colors Pro 2** (`Assets/JMO Assets/`) — toon shading, có asmdef riêng `ToonyColorsPro.*`; nền tốt cho dissolve zombie và look toon.
- **TextMeshPro**, **Timeline**, **Visual Scripting**, **bộ 2D** đã cài nhưng không phải trọng tâm của game này. TMP **Essential Resources chưa import** — 17 material font của Layer Lab đang là `Hidden/InternalErrorShader`; import qua Window → TextMeshPro trước khi dựng HUD.

## Asset pack — phân vai

Mỗi pack chỉ đóng **một** vai. Đừng lấy model của pack animation hay ngược lại.

### Model

| Thư mục | Vai | Ghi chú |
|---|---|---|
| `Assets/ArtStore3D/Zombie/` | Zombie chính | Rig **Humanoid**, `URP/Lit`. `Anim/` chỉ chứa animation camera/đèn của demo scene, **không** phải animation nhân vật |
| `Assets/Survivalist/` | Soldier người chơi | Rig Humanoid, nhiều skin. Material là `URP/Autodesk Interactive` (pack import từ FBX Autodesk Interactive) — nặng hơn `URP/Lit` nhưng chỉ 1 instance trên màn hình; thư mục `Materials URP` của pack cũng đúng shader đó, không hơn gì `Materials` |
| `Assets/Low Poly Guns/` | Súng | `URP/Lit`. Nguồn cho ≥2 loại súng của spec §5 |
| `Assets/ithappy/Military_Free/` | Môi trường, vật cản | `URP/Lit` sẵn từ pack |
| `Assets/ToonSoldiers_WW2_demo/model/` | (không dùng) | Model đi kèm pack animation, giữ để preview clip |

### Animation

Tất cả rig nhân vật đều **Humanoid**, nên clip retarget chéo giữa các model được.

| Nguồn | Clip | Dùng cho |
|---|---|---|
| `Assets/ToonSoldiers_WW2_demo/animation/` | `infantry_combat_idle`, `infantry_combat_run`, `infantry_combat_shoot`, `infantry_guard_idle` | Soldier: `combat_run` cho layer thân dưới, `combat_shoot` cho layer thân trên (spec §3) |
| `Assets/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/` | `Idle`, `Walk_N`, `Run_N`, `Run_S`, `Jump`, `InAir`, 2 clip land | Locomotion + Blend Tree cho soldier. **Giữ thư mục này** dù phần còn lại của StarterAssets bị xoá |

**Zombie chưa có animation nào trong project.** Pack `Zombie 1 Low Poly` chỉ có animation camera/đèn của demo scene; model đã rig Humanoid kèm Avatar nên clip Humanoid từ nguồn ngoài (Mixamo) retarget thẳng được — spec §4 cần chase / hit-reaction / death.

Pack `FREE Shirtless Zombie` (`Assets/NewPunch/`) **đã bị gỡ khỏi project**: đối chiếu tận file `.unitypackage` gốc thì nó chỉ có 2 FBX model, 6 prefab, 3 scene demo, 1 script và material/texture — không có animation nào, trong khi vai trò dự kiến của nó là nguồn animation zombie. Đừng import lại.

`Survivalist/StarterAssets/ThirdPersonController/Character/Animations/` còn kèm `StarterAssetsThirdPerson.controller` — Animator controller có sẵn Blend Tree locomotion, dùng làm điểm khởi đầu cho layer thân dưới của soldier.

### Sound

| Thư mục | Vai |
|---|---|
| `Assets/Tybug Studios/Zombie Voice Pack - Free/` | SFX zombie — 10 wav chia sẵn theo hành vi: Aggressive, Chase, Death, Growl, Grunt, Hiss, Moan |
| `Assets/PostApocalypseGunsDemo/` | SFX súng — 41 wav: AssaultRifles, Pistols, Shotguns, SniperRifles, Miniguns_loop |

### Particle

| Thư mục | Vai |
|---|---|
| `Assets/JMO Assets/WarFX/` | VFX súng đạn thực chiến — muzzle flash, bullet impact theo vật liệu, explosion. **Dùng bộ `_Effects (Mobile)`**, không dùng `_Effects` bản desktop |
| `Assets/Epic Toon FX/` | VFX toon — `Prefabs/Combat`, `Environment`, `Interactive`. Hợp look toon cho nổ bom và dissolve zombie |

Material built-in của pack mới import convert bằng **Tools ▸ Zombie War ▸ Convert Built-in Materials To URP** (`Assets/Scripts/Editor/BuiltInToUrpMaterialConverter.cs`) — nó gom material còn shader built-in rồi gọi converter chính chủ của Unity; skybox và material UI cố ý không đụng tới vì chạy tốt dưới URP. URP **không có** upgrader cho `Mobile/Particles/*` và `Legacy Shaders/Particles/*`, nên các material đó phải gán tay sang `URP/Particles/Unlit` nếu cần soft particle.

## Bố cục code first-party

Theo `CODE-RULE.md` §1: script dưới `Assets/Scripts/<Hệ thống>/` (Core, Player, Enemies, Weapons, Level, UI, Data, Audio, Utils, Editor), **instance** ScriptableObject dưới `Assets/Data/`. Assembly definition riêng cho code first-party để không compile chung `Assembly-CSharp` với các asset pack — hiện đã có `ZombieWar.Editor` (`Assets/Scripts/Editor/`, chỉ platform Editor); tạo asmdef runtime tương ứng khi viết script gameplay đầu tiên. Không bao giờ đặt code vào `Assets/JMO Assets`, `Assets/Layer Lab`, `Assets/Plugins`.

`Assets/_Recovery/0.unity` là file recovery sau crash của Unity, không phải scene thật — không dựng gì trên nó; xoá khi tiện.

## Build Android (sản phẩm APK)

Player settings đã có: scripting backend **IL2CPP**, kiến trúc **chỉ ARM64**, min SDK **25**, orientation Auto-rotate. Vẫn còn giá trị template phải đổi trước khi build cuối: `productName: TemplateCasual`, `companyName: DefaultCompany`, và build scene list (chỉ có `SampleScene`). Khoá orientation theo layout joystick đã thiết kế.

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
