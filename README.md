# TemplateMobileGame

A Unity template project for casual mobile games, set up with the Universal Render Pipeline (URP), the new Input System, and both **2D and 3D** rendering support.

## Requirements

- **Unity** `2022.3.62f3` (Unity 2022 LTS) — open with the matching editor version for best results.

## Key Packages

- **Universal RP** (`com.unity.render-pipelines.universal`) — rendering pipeline, configured with **two renderers** so the same project handles both 2D and 3D (see [Rendering: 2D & 3D](#rendering-2d--3d))
- **Input System** (`com.unity.inputsystem`) — input handling
- **2D Tooling** — Animation, Aseprite, PSD Importer, Sprite Shape, Tilemap (+ Extras)
- **Timeline**, **Visual Scripting**, **uGUI**
- **Test Framework** — play/edit mode tests
- **Toony Colors Pro 2** — stylized/toon shading (under `Assets/JMO Assets`)
- **Unity MCP** (`com.coplaydev.unity-mcp`) — Model Context Protocol integration for editor automation

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/DucHienIT/TemplateMobileGame.git
   ```
2. Open the project in **Unity Hub** with editor version `2022.3.62f3`.
3. Let Unity import packages and regenerate the `Library/` folder on first launch.

## Rendering: 2D & 3D

The active pipeline asset `Assets/Settings/UniversalRP.asset` lists two renderers, so you can build 2D and 3D scenes in the same project:

| Index | Renderer | Use for |
|-------|----------|---------|
| `0` (default) | `Renderer2D.asset` | Sprites, 2D lights, tilemaps |
| `1` | `UniversalRenderer.asset` | 3D meshes, lit/shadowed 3D scenes |

The default is 2D, so existing 2D content is unaffected. To render a 3D scene or camera, select the camera and set **Camera → Rendering → Renderer** to `UniversalRenderer (1)`. Need more renderers (e.g. a dedicated UI or post-processing pass)? Add them to the **Renderer List** on `UniversalRP.asset`.

## Project Structure

```
Assets/             # Game assets, scripts, scenes, and third-party packages
Packages/           # Package manifest and lock file
ProjectSettings/    # Unity project configuration
```

> Auto-generated folders (`Library/`, `Temp/`, `obj/`, `Logs/`, IDE/solution files) are excluded via `.gitignore`.
