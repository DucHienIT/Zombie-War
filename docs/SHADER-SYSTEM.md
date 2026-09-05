# Shader & material — hệ thống đổ bóng của game

Tài liệu shader/material của **Zombie War**. Viết ngày 2026-09-05, ngay sau khi gộp toàn bộ bề mặt 3D có ánh sáng về một shader Toony Colors Pro (commit `f5e5c938` trên `dev`; message của commit đó không nhắc đến việc này vì được gom chung với UI reskin và game feel).

> Quyết định của user 2026-09-05: **một shader chung cho mọi bề mặt 3D có ánh sáng = `Toony Colors Pro 2/Hybrid Shader`**. Particle, chữ TMP và UI không gộp được vì pack không có loại shader đó (xem mục 6).

---

## 1. Tóm tắt

Game dùng **6 shader asset** được material tham chiếu, cộng **1 shader ngầm** của uGUI. Số liệu tính bằng bao đóng bắc cầu từ scene `Gameplay.unity`, phủ đủ 44 material dưới `Assets/_ZombieWar/Art/Materials/`; không material nào thừa.

| # | Shader | Nguồn | Material | Dùng cho |
|---|---|---|---|---|
| 1 | `Toony Colors Pro 2/Hybrid Shader` | `Assets/ThirdParty/JMO Assets/Toony Colors Pro/Shaders/Hybrid/` (TCP2 **2.6.4**) | 25 | Soldier (13 material), zombie base `Zombie_Mat`, 2 súng, đất/đá/tường biên, prop môi trường, thân bom, vòng telegraph |
| 2 | `WFX_S Particle Add A8` | WarFX, copy vào `Art/Shaders/` | 10 | Particle cộng sáng: muzzle flash, sparks, glow, explosion flames |
| 3 | `Universal Render Pipeline/Particles/Unlit` | URP 14 | 7 | Tracer đạn, khói, debris, lửa nhỏ, 2 material Epic Toon FX |
| 4 | `WFX_S Particle Multiply A8` | WarFX, copy vào `Art/Shaders/` | 1 | Chấm tối nhân màu khi nổ |
| 5 | `ZombieWar/ZombieDissolve` | First-party HLSL, `Art/Shaders/ZombieDissolve.shader` | 1 | Hit flash + dissolve khi zombie chết (4 prefab Walker/Runner/Brute/Giant) |
| 6 | `TextMeshPro/Distance Field` | `Assets/TextMesh Pro/Shaders/TMP_SDF.shader` | 2, nhúng trong font | Oxanium ExtraBold + Ubuntu Bold |
| 7 | `UI/Default` (ngầm) | Unity | 0 | 290 `CanvasRenderer` của UI, `m_Material` để rỗng |

Trước ngày 2026-09-05 còn hai shader nữa: `URP/Lit` (12 material) và `URP/Autodesk Interactive` (13 material soldier). Cả hai đã gộp thành #1.

Post-process profile `Art/Rendering/GameplayPostProcess.asset` (Bloom, Vignette, Tonemapping, Color Adjustments) chạy bằng shader nội bộ của URP, không có material nào tham chiếu nên không tính vào bảng.

### Cách đếm lại bất cứ lúc nào

Quét chuỗi `guid:` trong mọi asset text dưới `_ZombieWar/` bắt đầu từ `Scenes/Gameplay.unity`, lấy bao đóng bắc cầu (kể cả `.meta` của FBX vì material remap nằm ở đó), lọc ra `.mat` rồi đọc dòng `m_Shader:` của từng file và tra GUID sang `.meta` của shader trong `Assets/` + `Library/PackageCache/`. Font TMP giữ material ngay trong `.asset` nên phải grep `m_Shader:` trong `.asset` nữa.

---

## 2. Toony Colors Pro 2 Hybrid trên URP 14

### Đã kiểm chứng trong editor (Unity 2022.3.62f3, URP 14.0.12)

| Hạng mục | Kết quả |
|---|---|
| Compile D3D11 | 0 lỗi, 0 cảnh báo (`ShaderUtil.GetShaderMessages` sau reimport) |
| SubShader active | Index 0 = nhánh URP, đủ pass `UniversalForward` / `ShadowCaster` / `DepthOnly` / `DepthNormals` / `Meta` |
| SRP Batcher | Tương thích (kiểm bằng `ShaderUtil.GetSRPBatcherCompatibilityCode` qua reflection sau khi render thử) |
| Trong suốt | Có `_RenderingMode` (0 Opaque / 1 Fade / 2 Transparent) + `_SrcBlend` / `_DstBlend` / `_ZWrite` |
| Mobile | Có `_UseMobileMode` (keyword `TCP2_MOBILE`) để cắt feature; hiện **tắt** |

⚠️ TCP2 2.6.4 ra tháng 1/2021, nhắm URP 7–10. Nó tự nhúng bản sao code lighting của URP trong `TCP2 Hybrid URP Support.cginc` nên không phụ thuộc include của URP 14 và vẫn compile. Chỉ mới kiểm trên D3D11; **GLES3 / Vulkan của Android chỉ kiểm được khi build APK** — build lần đầu sau khi đổi shader phải nhìn log shader compile.

### Bộ ramp dùng chung cho cả 25 material

| Property | Giá trị | Ghi chú |
|---|---|---|
| `_RampType` | 0 (Default) | |
| `_RampThreshold` | 0.5 | Mặc định pack là 0.75 |
| `_RampSmoothing` | 0.2 | Mặc định pack là 0.1 |
| `_HColor` | trắng | Highlight |
| `_SColor` | (0.36, 0.34, 0.44) | Shadow tint xám tím nhạt; mặc định pack là xám 0.2 |
| `_ShadowColorLightAtten` | 1 | Main light ảnh hưởng màu bóng |
| `_ReceiveShadowsOff` | 1 | Tên ngược: **1 = có nhận bóng** (drawer `ToggleOff`) |
| `_UseSpecular` / `_UseRim` / `_UseMatCap` / `_UseReflections` / `_UseOcclusion` / `_UseOutline` / `_UseMobileMode` | 0 | Tất cả feature phụ tắt |
| `_EmissionColor` | đen khi không dùng emission | Mặc định pack là **vàng** (1,1,0) — quên set là material phát sáng vàng ngay khi bật emission |

Muốn đổi look cho toàn game thì đổi cùng lúc trên cả 25 material (chọn hết trong Project window rồi sửa một lần trong Inspector, TCP2 hỗ trợ multi-edit), hoặc chạy lại script ở mục 3 với số mới.

### Chế độ render — chép đúng cách inspector TCP2 làm

| Rendering mode | `_RenderingMode` | `_SrcBlend` / `_DstBlend` | `_ZWrite` | `renderQueue` | Tag `RenderType` | Keyword |
|---|---|---|---|---|---|---|
| Opaque | 0 | 1 / 0 (One / Zero) | 1 | -1 (mặc định 2000) hoặc 2450 nếu alpha clip | `""` hoặc `TransparentCutout` | `_ALPHATEST_ON` nếu clip |
| Fade | 1 | 5 / 10 (SrcAlpha / OneMinusSrcAlpha) | 0 | 3000 | `Transparent` | |
| Transparent | 2 | 1 / 10 (One / OneMinusSrcAlpha) | 0 | 3000 | `Transparent` | `_ALPHAPREMULTIPLY_ON` |

Hiện chỉ `BlastTelegraph.mat` là Fade (vòng cam alpha 0.35). Set bằng code thì phải set **cả** float, keyword, tag và queue — drawer của TCP2 chỉ chạy khi bấm trong Inspector.

---

## 3. Convert material sang TCP2 Hybrid

Theo CLAUDE.md ("Không giữ editor tool sinh object"), việc convert **không** có editor script trong project; nó chạy một lần qua Unity MCP `execute_code` (C# 6 / codedom, không dùng local function hay `$""` phức tạp). Ghi lại ở đây để chạy lại khi có material mới import từ pack.

### Bảng remap property

| Nguồn | `URP/Lit` | `Autodesk Interactive` | Đích TCP2 Hybrid |
|---|---|---|---|
| Albedo | `_BaseMap` | `_MainTex` | `_BaseMap` (+ tiling/offset) |
| Màu | `_BaseColor` | `_Color` | `_BaseColor` |
| Normal | `_BumpMap` + `_BumpScale` | như Lit | `_BumpMap`, `_BumpScale` clamp [-1, 1], `_UseNormalMap = 1`, keyword `_NORMALMAP` |
| Emission | keyword `_EMISSION` + `_EmissionColor` / `_EmissionMap` | như Lit | `_UseEmission = 1`, keyword `_EMISSION`, `_EmissionChannel` = 4 (RGB) nếu có map, 5 nếu chỉ màu |
| Alpha clip | `_AlphaClip` + `_Cutoff` | keyword `_ALPHATEST_ON` | `_UseAlphaTest`, `_Cutoff`, keyword `_ALPHATEST_ON` |
| Trong suốt | `_Surface = 1`, `_Blend` (0 Alpha → Fade, 1 Premultiply → Transparent) | keyword `_SURFACE_TYPE_TRANSPARENT` → Fade | xem bảng chế độ render ở mục 2 |
| Cull | `_Cull` | `_Cull` | `_Cull` |
| Metallic / roughness / occlusion map | có | có | **bỏ** — TCP2 không có slot tương đương, toon không cần |

Thứ tự bắt buộc: **đọc hết giá trị cũ → `mat.shader = hybrid` → tắt mọi keyword còn sót (`foreach (var k in mat.shaderKeywords) mat.DisableKeyword(k)`) → ghi giá trị mới**. Đổi shader trước rồi mới đọc thì `_MainTex`/`_Color` của Autodesk không còn truy cập được qua API.

### Thân script (rút gọn phần lặp)

```csharp
const float RampThreshold = 0.5f;
const float RampSmoothing = 0.2f;
Color highlightColor = Color.white;
Color shadowColor = new Color(0.36f, 0.34f, 0.44f, 1f);
string litName = "Universal Render Pipeline/Lit";
string autodeskName = "Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive";
var hybrid = Shader.Find("Toony Colors Pro 2/Hybrid Shader");
string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/_ZombieWar/Art/Materials" });
foreach (var guid in guids)
{
    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    bool isLit = mat.shader.name == litName; bool isAutodesk = mat.shader.name == autodeskName;
    if (!isLit && !isAutodesk) continue;

    string baseMapProp = isLit ? "_BaseMap" : "_MainTex";
    string baseColorProp = isLit ? "_BaseColor" : "_Color";
    Texture baseMap = mat.GetTexture(baseMapProp);
    Vector2 scale = mat.GetTextureScale(baseMapProp); Vector2 offset = mat.GetTextureOffset(baseMapProp);
    Color baseColor = mat.GetColor(baseColorProp);
    Texture bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
    float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1f;
    bool emission = mat.IsKeywordEnabled("_EMISSION");
    Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
    Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
    float cull = mat.HasProperty("_Cull") ? mat.GetFloat("_Cull") : 2f;
    bool alphaClip = mat.IsKeywordEnabled("_ALPHATEST_ON") || (mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f);
    float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;
    bool transparent = mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT") || (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") > 0.5f);
    bool premultiply = transparent && mat.HasProperty("_Blend") && Mathf.RoundToInt(mat.GetFloat("_Blend")) == 1;

    mat.shader = hybrid;
    foreach (var k in mat.shaderKeywords) mat.DisableKeyword(k);
    mat.SetTexture("_BaseMap", baseMap); mat.SetTextureScale("_BaseMap", scale); mat.SetTextureOffset("_BaseMap", offset);
    mat.SetColor("_BaseColor", baseColor);
    mat.SetFloat("_RampType", 0f); mat.SetFloat("_RampThreshold", RampThreshold); mat.SetFloat("_RampSmoothing", RampSmoothing);
    mat.SetColor("_HColor", highlightColor); mat.SetColor("_SColor", shadowColor);
    mat.SetFloat("_ShadowColorLightAtten", 1f); mat.SetFloat("_UseShadowTexture", 0f);
    bool useNormal = bump != null;
    mat.SetFloat("_UseNormalMap", useNormal ? 1f : 0f);
    if (useNormal) { mat.EnableKeyword("_NORMALMAP"); mat.SetTexture("_BumpMap", bump); mat.SetFloat("_BumpScale", Mathf.Clamp(bumpScale, -1f, 1f)); }
    bool useEmission = emission && (emissionColor.maxColorComponent > 0f || emissionMap != null);
    mat.SetFloat("_UseEmission", useEmission ? 1f : 0f);
    mat.SetColor("_EmissionColor", useEmission ? emissionColor : Color.black);
    if (useEmission) { mat.EnableKeyword("_EMISSION"); mat.SetFloat("_EmissionChannel", emissionMap != null ? 4f : 5f); mat.SetTexture("_EmissionMap", emissionMap); }
    mat.SetFloat("_UseAlphaTest", alphaClip ? 1f : 0f); mat.SetFloat("_Cutoff", cutoff);
    if (alphaClip) mat.EnableKeyword("_ALPHATEST_ON");
    mat.SetFloat("_Cull", cull);
    mat.SetFloat("_ReceiveShadowsOff", 1f);
    mat.SetFloat("_UseSpecular", 0f); mat.SetFloat("_UseRim", 0f); mat.SetFloat("_UseMatCap", 0f); mat.SetFloat("_UseReflections", 0f);
    mat.SetFloat("_UseOcclusion", 0f); mat.SetFloat("_UseOutline", 0f); mat.SetFloat("_UseMobileMode", 0f);
    if (!transparent)
    {
        mat.SetFloat("_RenderingMode", 0f); mat.SetFloat("_SrcBlend", 1f); mat.SetFloat("_DstBlend", 0f); mat.SetFloat("_ZWrite", 1f);
        mat.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "");
        mat.renderQueue = alphaClip ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
    }
    else if (premultiply)
    {
        mat.SetFloat("_RenderingMode", 2f); mat.SetFloat("_SrcBlend", 1f); mat.SetFloat("_DstBlend", 10f); mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON"); mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
    else
    {
        mat.SetFloat("_RenderingMode", 1f); mat.SetFloat("_SrcBlend", 5f); mat.SetFloat("_DstBlend", 10f); mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
    UnityEditor.EditorUtility.SetDirty(mat);
}
UnityEditor.AssetDatabase.SaveAssets();
```

Material giữ nguyên GUID nên prefab/FBX remap không cần wire lại.

### Thêm material mới bằng tay

1. Tạo material, chọn shader `Toony Colors Pro 2/Hybrid Shader`.
2. Copy component material từ một material cùng loại đã có (chuột phải header Inspector → Copy / Paste) để lấy đúng bộ ramp, rồi chỉ đổi `_BaseMap` / `_BaseColor` / normal map.
3. Nếu vật thể trong suốt thì chọn Rendering Mode trong Inspector (drawer tự set blend/queue), đừng gõ tay từng số.

---

## 4. Zombie dissolve — shader first-party

`Art/Shaders/ZombieDissolve.shader` (HLSL thuần URP, 3 pass `ForwardLit` / `ShadowCaster` / `DepthOnly`), material `Art/Materials/ZombieDissolve.mat`, gắn trên 4 prefab zombie. Spec §4 bắt buộc chết bằng dissolve shader nên nó không gộp vào TCP2 Hybrid được.

| Property | Ai ghi | Ý nghĩa |
|---|---|---|
| `_HitAmount` (0–1) | `ZombieMaterialFx` qua `MaterialPropertyBlock` | Lerp albedo + emission sang `_HitColor` khi trúng đạn |
| `_DissolveAmount` (0–1) | `ZombieMaterialFx` qua MPB | Ngưỡng clip theo `_NoiseMap`; viền phát sáng `_EdgeColor` rộng `_EdgeWidth` |
| `_BaseMap` / `_BaseColor` / `_Smoothness` / `_NoiseMap` / `_EdgeColor` / `_HitColor` | author trên material | |

Ba pass đều gọi `ApplyDissolve` nên bóng đổ và depth tan cùng thân.

### Lỗi đã sửa 2026-09-05

Pass `ShadowCaster` include `Shadows.hlsl` mà không include `Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl`; `Shadows.hlsl` gọi `LerpWhiteTo` nằm trong file đó nên pass báo `undeclared identifier 'LerpWhiteTo'` từ commit đầu tiên. Pass `ForwardLit` không lỗi vì `Lighting.hlsl` kéo `BRDF.hlsl` → `CommonMaterial.hlsl`. Đã thêm include; shader hiện 0 lỗi.

### Việc còn lại: đồng bộ look với TCP2

Shader vẫn dùng `UniversalFragmentBlinnPhong` nên zombie đang bóng mềm trong khi soldier/prop đã cel-shade. Hai hướng đã cân nhắc:

1. **Thêm ramp toon vào shader hiện tại** (khuyến nghị): thay Blinn-Phong bằng `smoothstep(threshold - smoothing, threshold + smoothing, NdotL × shadow)` lerp giữa `_SColor` và `_HColor` với cùng bộ số ở mục 2, giữ nguyên property/MPB nên `ZombieMaterialFx` không đổi.
2. **Sinh shader bằng TCP2 Shader Generator 2** (template `Shader Templates 2/SG2_Template_URP.txt` có module Dissolve; các overload URP nó gọi — `GetMainLight(shadowCoord)`, `MixRealtimeAndBakedGI(..., half4)` — vẫn tồn tại trong URP 14). Nhược điểm: thành shader TCP2 thứ hai, property đổi tên (`_DissolveValue`, `_DissolveMap`) và hit flash phải chuyển sang emission nên `ZombieMaterialFx` phải sửa.

Đang chờ user duyệt look của 25 material trước khi làm.

---

## 5. Code phụ thuộc tên property shader

Đây là danh sách đầy đủ; đổi shader mà không rà chỗ này là hỏng hiệu ứng mà không có compile error.

| Script | Property | Ghi chú |
|---|---|---|
| `Player/PlayerHitFlash` | `[SerializeField] _colorPropertyName` trong `GameplayRoot.prefab` = **`_BaseColor`** | Trước là `_Color` (Autodesk). Sai tên thì `Awake` log error và bỏ renderer đó khỏi hiệu ứng, không crash |
| `Enemies/ZombieMaterialFx` | `_HitAmount`, `_DissolveAmount` (hằng trong code) | Chỉ khớp `ZombieDissolve.shader` |

Cả hai đều dùng `MaterialPropertyBlock`, chạy bình thường với TCP2 Hybrid (không instance material, không phá SRP Batcher hơn hiện trạng).

---

## 6. Những gì không gộp vào TCP2 được

| Nhóm | Số material | Lý do |
|---|---|---|
| Particle (`WFX_S Particle Add/Multiply A8`, `URP/Particles/Unlit`) | 18 | TCP2 không có shader particle: không additive theo vertex color, không soft particle, không flipbook |
| Chữ TMP (`TMP_SDF`) | 2 font | Text SDF là shader riêng của TextMeshPro |
| UI (`UI/Default`) | ngầm | Canvas cần masking/stencil của uGUI |

Vì vậy trần thấp nhất của game là **6 shader asset**; "một shader chung" đúng nghĩa là một shader cho mọi bề mặt 3D có ánh sáng, và đã đạt.

---

## 7. Bẫy đã gặp và lưu ý

- **Đổi shader trên material không giữ texture nếu tên property khác.** Lit → Hybrid giữ được (cùng `_BaseMap`/`_BaseColor`/`_BumpMap`); Autodesk → Hybrid mất albedo và màu nếu không remap `_MainTex`/`_Color`.
- **Keyword cũ bám theo material** sau khi đổi shader (`_METALLICGLOSSMAP`, `_SPECGLOSSMAP`, `_SURFACE_TYPE_TRANSPARENT`...). Phải tắt hết trước khi bật keyword mới, không thì `m_ValidKeywords` chứa rác.
- **`_EmissionColor` mặc định của Hybrid là vàng.** Set về đen cho material không phát sáng.
- **`_ReceiveShadowsOff` = 1 nghĩa là có nhận bóng** (drawer `ToggleOff`), đọc ngược với tên.
- **Bloom**: `GameplayPostProcess.asset` có threshold 1 / intensity 0.45 cân theo material Lit cũ. `_HColor` trắng × albedo không vượt 1 nếu cường độ đèn ≤ 1, nhưng phải kiểm lại bằng đúng camera rig trong `GameplayRoot` (post-processing bật) sau khi chốt ramp; chỉnh trong profile, không chỉnh code.
- **Ảnh duyệt look không qua post-process.** Bốn ảnh `Captures/toon_before*.png` / `toon_after*.png` render bằng camera tạm trong edit mode (instantiate `Map_FlatOutpost` + `Player` tách từ `GameplayRoot` + 4 prefab zombie, pose bằng `AnimationMode.SampleAnimationClip`, camera pitch 72° offset (0, 14, −4.5) FOV dọc 66° cho khung 9:16). Cách này không cần Play mode và không làm bẩn scene (object tạm `HideAndDontSave`, huỷ ngay sau khi chụp).
- **Metallic / roughness map của soldier bị bỏ** khi sang toon. Muốn lấy lại độ bóng thì bật `_UseSpecular` kiểu Stylized và nạp `_MetallicGlossMap` cũ vào `_SpecGlossMap` với `_SpecularMapType` = Custom channel — chưa làm vì look toon phẳng đang là mục tiêu.
- **Editor tool convert không được giữ trong project** (CLAUDE.md). Chạy lại thì dán script ở mục 3 vào `execute_code`; đừng tạo lại `Scripts/Editor/` cho việc này.

---

## 8. Việc còn lại

1. User duyệt bộ ramp (mục 2) — có thể đổi threshold/smoothing/shadow tint hoặc bật outline.
2. Đồng bộ zombie: thêm ramp toon vào `ZombieDissolve.shader` (mục 4, hướng 1).
3. Chụp lại bằng camera rig thật với post-processing bật, retune Bloom trong `GameplayPostProcess.asset` nếu highlight gắt.
4. Cập nhật CLAUDE.md: mục "Stack chính" (Toony Colors Pro không còn là "nền tốt" mà là shader chính), bảng model (soldier không còn Autodesk Interactive), dòng `PlayerHitFlash` (property `_BaseColor`), mục "Còn thiếu".
5. Build APK lần đầu sau khi đổi shader: đọc log compile GLES3/Vulkan của `TCP2 Hybrid Shader`.
6. Tuỳ chọn: bật `_UseMobileMode` trên 25 material nếu profiler trên máy thật cho thấy fragment nặng.
