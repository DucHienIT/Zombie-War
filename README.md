# Zombie War

Game bắn súng 3D góc nhìn top-down cho mobile, màn hình dọc 9:16, làm trên Unity với Universal Render Pipeline (URP) và Input System mới.

> Nhánh `build` là bản rút gọn để đóng gói: chỉ giữ asset và package mà game thật sự dùng. Không có kho asset pack dự phòng (`Assets/ThirdParty`), tài liệu thiết kế hay hướng dẫn phát triển — những thứ đó nằm trên nhánh `dev`.

## Yêu cầu

- **Unity** `2022.3.62f3` (Unity 2022 LTS) — mở đúng phiên bản editor này.
- **Android Build Support** (kèm SDK/NDK/OpenJDK) trong Unity Hub để build APK.

## Package chính

- **Universal RP** (`com.unity.render-pipelines.universal`) — pipeline render, asset `Assets/Settings/UniversalRP.asset`, renderer mặc định là `UniversalRenderer.asset` (3D forward)
- **Input System** (`com.unity.inputsystem`) — joystick ảo đi qua on-screen device, action asset `Assets/InputSystem_Actions.inputactions`
- **Cinemachine** — camera top-down follow soldier
- **AI Navigation** — `NavMeshSurface` bake theo map prefab
- **TextMeshPro**, **uGUI** — UI
- **DOTween / DOTweenPro** — tween (trong `Assets/Plugins/Demigiant`)
- **Toony Colors Pro 2 Hybrid** — shader toon, chỉ giữ file shader tại `Assets/_ZombieWar/Art/Shaders/`

## Bắt đầu

1. Clone repository và checkout nhánh `build`:
   ```bash
   git clone -b build https://github.com/DucHienIT/Zombie-War.git
   ```
2. Mở project bằng **Unity Hub** với editor `2022.3.62f3`.
3. Để Unity import package và sinh lại thư mục `Library/` ở lần mở đầu.

## Build APK

Build bằng menu, không mở File → Build Settings:

| Menu | Ra file |
|---|---|
| `Tools ▸ Zombie War ▸ Build ▸ Android APK (Release)` | `Builds/Android/Release/ZombieWar-<version>-vc<code>.apk` |
| `Tools ▸ Zombie War ▸ Build ▸ Android APK (Development)` | `Builds/Android/Development/…-dev.apk` (kèm profiler) |

Tool (`Assets/_ZombieWar/Scripts/Editor/AndroidBuilder.cs`) tự áp toàn bộ player settings ảnh hưởng tới bản build — IL2CPP, ARM64, min SDK 25, portrait, ASTC, APK thay vì AAB — nên không cần tick tay thứ gì. Sửa tay trong Inspector sẽ bị lần build sau ghi đè; muốn đổi thật thì sửa hằng số trong script.

Texture và audio có luật import riêng cho Android, ép tự động lúc import (`TextureImportRules` / `AudioImportRules`): texture nén ASTC với maxSize theo loại nội dung, audio mono Vorbis. Trước khi build release nên chạy `Tools ▸ Zombie War ▸ Assets ▸ Reapply Texture Import Rules (Android)` một lần.

Mặc định APK ký bằng debug keystore của Unity — cài chạy được trên máy thật nhưng Play Store từ chối. Ký thật thì đặt 4 biến môi trường `ZW_ANDROID_KEYSTORE`, `ZW_ANDROID_KEYSTORE_PASS`, `ZW_ANDROID_KEYALIAS`, `ZW_ANDROID_KEYALIAS_PASS` trước khi mở Unity.

Build headless:

```bash
Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod ZombieWar.EditorTools.AndroidBuilder.BuildReleaseFromCommandLine -logFile build.log
```

## Cấu trúc project

```
Assets/_ZombieWar/   # Toàn bộ asset first-party: Scripts, Data, Prefabs, Scenes, Models, Animation, Audio, Art
Assets/Plugins/      # DOTween
Assets/Settings/     # URP asset và renderer
Assets/TextMesh Pro/ # TMP Essential Resources
Packages/            # Manifest và lock file của package
ProjectSettings/     # Cấu hình project Unity
```

Game có đúng hai scene: `Assets/_ZombieWar/Scenes/Loading.unity` (build index 0) và `Gameplay.unity`. Map của từng level là prefab `Assets/_ZombieWar/Prefabs/Environment/Map_*.prefab`, được nạp tại chỗ khi bắt đầu một lượt chơi.

> Các thư mục tự sinh (`Library/`, `Temp/`, `obj/`, `Logs/`, `Builds/`, file IDE/solution) đã được loại qua `.gitignore`.
