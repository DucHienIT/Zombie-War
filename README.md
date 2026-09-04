# Zombie War

Game bắn súng 3D góc nhìn top-down cho mobile, làm trên Unity với Universal Render Pipeline (URP), Input System mới, và hỗ trợ render cả **2D lẫn 3D**. Đề bài và tiêu chí nghiệm thu xem tại [docs/GAME_SPEC.md](docs/GAME_SPEC.md). Quy tắc code xem tại [CODE-RULE.md](CODE-RULE.md).

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
| `0` (mặc định) | `Renderer2D.asset` | Sprite, đèn 2D, tilemap |
| `1` | `UniversalRenderer.asset` | Mesh 3D, scene 3D có ánh sáng/bóng |

Mặc định là 2D. Để render scene hoặc camera 3D, chọn camera và đặt **Camera → Rendering → Renderer** thành `UniversalRenderer (1)`. Cần thêm renderer (ví dụ pass UI hay post-processing riêng) thì thêm vào **Renderer List** trên `UniversalRP.asset`.

## Cấu trúc project

```
Assets/             # Asset game, script, scene và package third-party
Packages/           # Manifest và lock file của package
ProjectSettings/    # Cấu hình project Unity
docs/               # Spec và tài liệu
```

> Các thư mục tự sinh (`Library/`, `Temp/`, `obj/`, `Logs/`, file IDE/solution) đã được loại qua `.gitignore`.
