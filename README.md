# Zombie War

Game bắn súng 3D góc nhìn top-down cho mobile, màn hình dọc, làm trên Unity với Universal Render Pipeline (URP) và Input System mới. **Dev mới bắt đầu tại [Tài liệu hệ thống và hướng dẫn phát triển](docs/DEVELOPER-GUIDE.md)**: kiến trúc, luồng gameplay, save, cách thêm nội dung, debug và kiểm thử. Thiết kế gốc xem tại [GDD](docs/Zombie_War_SPEC_GDD_V1.0.docx); quy tắc code xem tại [CODE-RULE.md](CODE-RULE.md).

## Yêu cầu

- **Unity** `2022.3.62f3` (Unity 2022 LTS) — mở đúng phiên bản editor này.

## Package chính

- **Universal RP** (`com.unity.render-pipelines.universal`) — pipeline render, cấu hình **hai renderer** để cùng project xử lý được 2D và 3D (xem [Render: 2D & 3D](#render-2d--3d))
- **Input System** (`com.unity.inputsystem`) — xử lý input
- **Bộ 2D** — Animation, Aseprite, PSD Importer, Sprite Shape, Tilemap (+ Extras)
- **Timeline**, **Visual Scripting**, **uGUI**
- **Test Framework** — test play/edit mode
- **Toony Colors Pro 2** — toon shading (trong `Assets/JMO Assets`)
- **DOTween / DOTweenPro** — tween (trong `Assets/Plugins/Demigiant`)
- **Layer Lab GUI Pro-CasualGame** — bộ prefab UI casual (trong `Assets/Layer Lab`)
- **Unity MCP** (`com.coplaydev.unity-mcp`) — tích hợp Model Context Protocol để tự động hoá editor

## Bắt đầu

1. Clone repository:
   ```bash
   git clone https://github.com/DucHienIT/Zombie-War.git
   ```
2. Mở project bằng **Unity Hub** với editor `2022.3.62f3`.
3. Để Unity import package và sinh lại thư mục `Library/` ở lần mở đầu.

## Render: 2D & 3D

Pipeline asset đang dùng `Assets/Settings/UniversalRP.asset` có hai renderer, nên có thể dựng scene 2D và 3D trong cùng project:

| Index | Renderer | Dùng cho |
|-------|----------|----------|
| `0` | `Renderer2D.asset` | Sprite, đèn 2D, tilemap |
| `1` (mặc định) | `UniversalRenderer.asset` | Mesh 3D, scene 3D có ánh sáng/bóng |

Mặc định hiện là renderer 3D (index 1); camera để **Renderer: Default** dùng đường render này. Camera 2D có thể chọn renderer index 0. Cần thêm renderer thì thêm vào **Renderer List** trên `UniversalRP.asset`.

## Cấu trúc project

```
Assets/             # Asset game, script, scene và package third-party
Packages/           # Manifest và lock file của package
ProjectSettings/    # Cấu hình project Unity
docs/               # Spec và tài liệu
```

> Các thư mục tự sinh (`Library/`, `Temp/`, `obj/`, `Logs/`, file IDE/solution) đã được loại qua `.gitignore`.
