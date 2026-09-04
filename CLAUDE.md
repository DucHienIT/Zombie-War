# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A **starter template** for casual mobile games in Unity, not a game yet. All C# under `Assets/` is third-party asset packs (Toony Colors Pro 2, DOTween/DOTweenPro, Layer Lab GUI Pro). There is **no first-party game code, no game asmdef, and no gameplay scene** beyond `Assets/Scenes/SampleScene.unity` (the sole scene in the build list). New game code starts from a blank slate — expect to create the scripts, scenes, and ScriptableObject configs yourself.

## Unity version (important)

- Editor version is **`2022.3.62f3`** (`ProjectSettings/ProjectVersion.txt`, branch `unity_2022`). The project was intentionally downgraded from Unity 6 — see commit `1f3bbff "down version"`. Open with this exact version to avoid a forced upgrade.
- If `ProjectVersion.txt` and `README.md` ever disagree on the version, `ProjectVersion.txt` is authoritative.

## Key stack

- **URP 14.0** — supports **both 2D and 3D**. The active pipeline `Assets/Settings/UniversalRP.asset` (guid `681886c5...`, referenced by every quality tier) holds two renderers:
  - index **0 = `Renderer2D.asset`** — the default; 2D lit/sprite scenes render with this out of the box.
  - index **1 = `UniversalRenderer.asset`** — the standard 3D forward renderer, added for 3D scenes.
  - Pick per scene/camera: leave a camera on the default for 2D, or set its **Camera → Rendering → Renderer** to `UniversalRenderer (1)` for 3D. `m_DefaultRendererIndex` stays 0, so nothing changes unless a camera opts in. Add renderers by appending to `m_RendererDataList` in `UniversalRP.asset`.
  - Global settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`.
- **New Input System 1.18.0** — actions asset wired via `ProjectSettings/EditorBuildSettings.asset` (`com.unity.input.settings.actions`). Do not use the legacy `Input.*` API.
- **2D tooling**: Animation, Aseprite, PSD Importer, Sprite Shape, Tilemap (+ Extras).
- **DOTween / DOTweenPro** (`Assets/Plugins/Demigiant/`) — tweening. Settings: `Assets/Resources/DOTweenSettings.asset`.
- **Layer Lab GUI Pro-CasualGame** (`Assets/Layer Lab/`) — prefab-based casual UI kit (buttons, popups, frames, sliders). Prefer composing these prefabs for UI.
- **Toony Colors Pro 2** (`Assets/JMO Assets/`) — stylized shading; ships its own asmdefs (`ToonyColorsPro.*`).

## Unity MCP

`com.coplaydev.unity-mcp` (MCP for Unity) is a package dependency, so the editor can be driven programmatically over MCP. If `mcp__UnityMCP__*` tools are missing or on the wrong port, use the `unity-mcp-connect` skill — each open editor/project binds its own port. Note: `.mcp.json` is not committed.

## Working in this repo

- **No build/lint/test CLI is set up.** Building and playmode happen inside the Unity Editor. The Test Framework (`com.unity.test-framework`) is installed but there are no test assemblies yet.
- To run tests headlessly once test asmdefs exist:
  ```bash
  Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml
  ```
  (swap `EditMode`/`PlayMode`; use the 2022.3.62f3 editor binary).
- The `Assembly-CSharp*.csproj` and `.sln` at the repo root are Unity-generated and gitignored — never hand-edit them; they regenerate on import.
- When adding first-party code, create a dedicated assembly definition (e.g. `Assets/_Game/`) rather than dropping scripts into the third-party folders.

## Conventions for new game code

The available skills encode the intended patterns for this template — follow them when the task matches:
- **`unity-game-clone`** — scene-authored + prefab architecture, tunables/colors in ScriptableObject configs (never hardcoded), procedural sprites/audio, solver-generated levels. Use when building a game from a spec.
- **`unity-ui-refactor`** — code-first uGUI (UIFactory/SpriteFactory), procedural "candy" UI, no image assets.
- **`texture-override`** — pick compression format by target device (TV/desktop → DXT, mobile/TikTok → ASTC) when building WebGL.
- **`tiktok-minigame-sdk`** / **`tv-input-kit`** — TikTok Mini Game (WebGL) and TV-remote input integration.

## Git

- Default branch is `main`; active work is on `unity_2022`.
- Auto-generated folders (`Library/`, `Temp/`, `Logs/`, `obj/`, IDE/`.sln`/`.csproj` files) are gitignored.
