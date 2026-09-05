# Skill Tree — cây kỹ năng vĩnh viễn trong main menu

Tài liệu hệ thống skill tree của **Zombie War**. Viết ngày 2026-09-05, khi tab TALENT (placeholder "COMING SOON") được thay bằng trang SKILLS.

> Hệ thống này **nằm ngoài spec** `Zombie_War_SPEC_GDD_V1.0.docx` (spec không có metagame). User yêu cầu thêm sau khi P0 + meta progression + roguelike đã xong: *"1 cây kỹ năng giúp nhân vật nâng các chỉ số cơ bản, hình ảnh và animation phải đẹp và hợp lý"*.

---

## 1. Tóm tắt

Người chơi tiêu **coin** (đồng tiền meta đã có, cùng ví với shop súng) để mua **rank** cho các node của một cây cố định 13 node. Mỗi rank cộng thêm một `StatModifier` vĩnh viễn vào `PlayerStatSheet` — đúng struct mà passive skill roguelike đã dùng — nên mọi hệ thống gameplay (súng, máu, di chuyển, bom, XP) nhận bonus **mà không sửa một dòng nào**.

Ba điểm khác với roguelike trong trận:

| | Roguelike (trong trận) | Skill tree (menu) |
|---|---|---|
| Sống bao lâu | Một lượt chơi | Vĩnh viễn (PlayerPrefs) |
| Trả bằng gì | Battle level (XP giết quái) | Coin |
| Ai giữ state | `RoguelikeDirector._stacks` | `ProfileService.Skills` (`SkillTreeProgress`) |

---

## 2. Cây và số cân bằng

Data ở `Assets/_ZombieWar/Data/SkillTree/` — `SkillTree.asset` (`SkillTreeSO`, thứ tự node = thứ tự slot trên trang) và 13 `Node_*.asset` (`SkillTreeNodeSO`). Mọi số dưới đây là field trên asset, không có trong code.

Gốc ở **đáy** cây, ba nhánh mọc lên; node chỉ mở khi **mọi** node trong `_prerequisites` đã có ít nhất 1 rank.

| # | Node | Nhánh | Stat mỗi rank | Trần | Giá gốc × hệ số | Yêu cầu |
|---|---|---|---|---|---|---|
| 0 | COMBAT INSTINCT | gốc (`#3FC8FF`) | `XpGain` ×1.06 | 5 | 100 × 1.4 | — |
| 1 | MARKSMAN | Offense (`#FF9B3D`) | `WeaponDamage` ×1.06 | 5 | 120 × 1.45 | 0 |
| 2 | TOUGHNESS | Survival (`#3DE0A0`) | `MaxHealth` +10 | 5 | 120 × 1.45 | 0 |
| 3 | BLAST RADIUS | Demolition (`#B47CFF`) | `BombRadius` ×1.08 | 4 | 120 × 1.45 | 0 |
| 4 | TRIGGER FINGER | Offense | `FireRate` ×1.05 | 5 | 200 × 1.45 | 1 |
| 5 | FIELD MEDIC | Survival | `HealPerKill` +0.5 | 4 | 200 × 1.45 | 2 |
| 6 | HIGH EXPLOSIVE | Demolition | `BombDamage` ×1.10 | 5 | 200 × 1.45 | 3 |
| 7 | HEAVY IMPACT | Offense | `Knockback` ×1.12 | 4 | 320 × 1.45 | 4 |
| 8 | SPRINTER | Survival | `MoveSpeed` ×1.05 | 4 | 320 × 1.45 | 5 |
| 9 | EXTRA BOMB | Demolition | `ExtraBombsPerVolley` +1 | 2 | 320 × 1.6 | 6 |
| 10 | ARMOR PIERCING | Offense | `ProjectilePierce` +1 | 1 | 800 | 7 |
| 11 | SECOND HEART | Survival | `MaxHealth` +40 | 1 | 800 | 8 |
| 12 | MEGATON | Demolition | `BombDamage` ×1.30 | 1 | 800 | 9 |

Giá rank kế tiếp = `_baseCost × _costGrowth^rankHiệnTại` (`SkillTreeNodeSO.GetCost`). Một lượt L1 thắng trả khoảng 1500–2000 coin (điểm × 0.5 + 100), nên mua trọn một nhánh (~5 lượt) là chủ ý; tổng 46 rank không thể full sớm.

**Mỗi node đúng một stat** (entry đầu của `_modifiersPerRank` là thứ panel chi tiết trích dẫn `hiện tại → sau khi mua`). Giá trị trích dẫn tính ở `SkillTreeNodeSO.EffectValueAt(rank)`: additive = `value × rank`, multiplicative = `(value^rank − 1) × 100` (%) — hai rank ×1.06 hiện đúng +12.4% chứ không phải +12%. Định dạng số nằm trong `_valueFormat` của node (`+{0:0}%`, `+{0:1}`…) vì chỉ data mới biết stat đó là phần trăm hay số lẻ.

⚠️ `StatId` được serialize theo **index** trong asset — đổi thứ tự enum thì phải đánh số lại `_stat` ở cả `Data/Roguelike/Skill_*.asset` lẫn `Data/SkillTree/Node_*.asset`.

---

## 3. Kiến trúc

```
Scripts/Data/
├── SkillTreeNodeSO.cs      # identity + StatModifier[] mỗi rank + giá + prerequisites
└── SkillTreeSO.cs          # danh sách node theo thứ tự slot

Scripts/Core/
├── SkillTreeProgress.cs    # C# thuần: rank đang có, IsUnlocked, CostAt, Advance (ghi PlayerPrefs)
├── ProfileService.cs       # + _skillTree, Skills, CanUpgradeSkill, TryUpgradeSkill (trừ coin, OnChanged)
└── SaveService.cs          # + zw_skill_rank_<id>

Scripts/Player/
├── PlayerStatSheet.cs      # + lớp "permanent": ClearPermanent/AddPermanent, BeginRebuild áp lại
└── SkillTreeStatApplier.cs # MonoBehaviour trên Systems/Managers: Profile → PlayerStatSheet

Scripts/UI/
├── SkillNodeData.cs                     # struct trình bày (UI không thấy SO)
└── MainMenu/{SkillTreePageView, SkillTreeNodeView, SkillDetailPanelView}.cs
```

### Đường đi của một lần mua

```
SkillDetailPanelView (nút UPGRADE)
  → SkillTreePageView._onUpgrade(index)           # index = slot đang chọn
  → UIManager.Wrap → MenuUiBinder.HandleSkillUpgradeRequested
  → ProfileService.TryUpgradeSkill(index)         # kiểm tra mở khoá / trần / đủ coin, trừ coin, Advance, OnChanged
  → MenuUiBinder.HandleProfileChanged → RefreshSkills → UIManager.RefreshMenu(header, weapons, skills)
  → SkillTreePageView.Refresh(nodes)              # diff rank cũ/mới → PlayUpgrade / PlayUnlocked / Bind
  → SkillTreeStatApplier.Push (cũng nghe OnChanged) # nạp lại lớp permanent vào PlayerStatSheet
```

### Vì sao bonus vĩnh viễn nằm *trong* `PlayerStatSheet`

`RoguelikeDirector.RebuildLoadout` gọi `BeginRebuild()` (xoá sạch) rồi áp lại các stack roguelike **mỗi lần chọn thẻ**. Nếu applier chỉ `Apply` một lần thì thẻ roguelike đầu tiên sẽ xoá mất skill tree. Vì thế `PlayerStatSheet` giữ danh sách `_permanent` và `BeginRebuild()` áp lại nó ngay sau khi reset — director không cần biết skill tree tồn tại, không phải sửa `RoguelikeDirector`.

`SkillTreeStatApplier` nằm trên `Systems/Managers` (luôn active) chứ không trên `Player` (bị tắt tới khi vào trận): nó `Push()` ở `OnEnable` và mỗi `ProfileService.OnChanged`, tức lớp permanent luôn đúng **trước** khi `OnRunStarted` kích hoạt rebuild của director. Không có rebuild nào chạy trong menu (Player đang tắt) nên mua skill trong menu không đụng gì tới trận.

Thứ tự nhân vẫn như roguelike: hệ số đi lên trên `gun.Stats` (đã gồm nâng cấp mua bằng coin), xem `docs/ROGUELIKE-SYSTEM.md` §5.

---

## 4. UI — `Page_SkillTree` trong `UIRoot.prefab`

```
Page_SkillTree [SkillTreePageView]           (index 3 của MenuScreenView._pages = MenuTab.SkillTree)
├── Title "SKILL TREE" · Subtitle
└── Content (1080×1430, co lại bằng FitToPage khi màn ngắn)
    ├── Tree (1040 cao)
    │   ├── Branch_Offense / Branch_Survival / Branch_Demolition   (nhãn màu nhánh)
    │   ├── Links/Link_*    (12 Image không sprite, xoay theo góc cha→con, pivot ở đầu cha)
    │   └── Node_* ×13 [SkillTreeNodeView + Button + UiButtonFx]
    │       ├── Glow (Image_Glow_Circle01_White, thở khi mua được)
    │       ├── Burst (cùng sprite, loé khi mua)
    │       └── Body (scale khi chọn / punch / reveal)
    │           ├── Press (UiButtonFx) → Ring (Btn_MenuButton_Hexagon01_s_White2) · Hex (Btn_IconButton_Hexagon02_White1) · Icon · Lock · MaxTag
    │           └── Pips → Pip1..Pip5 (chấm rank, DrawPips căn giữa theo MaxRank)
    └── Detail [SkillDetailPanelView + CanvasGroup]   (khung Frame_TableFrame01_Navy — tầng khung duy nhất của trang)
        └── Content → IconPlate/Icon · Name · Rank · Description · EffectLabel · CurrentValue · Next(Arrow, Value) · UpgradeButton(Label, Cost) · Hint
```

Toạ độ cây (Tree top-centre, y âm đi xuống): gốc `(0,−930)`; T1 `(±280,−730)`, T2 `(±350,−530)`, T3 `(±385,−330)`, T4 `(±385,−130)`; cột giữa x = 0. Node 150 px, hàng cách 200 px, Detail ở y = −1060 cao 370. Panel đủ chỗ cho 9:16 không notch; có notch thì `FitToPage` co cả `Content` (không có ScrollRect — cố ý, để cây luôn nhìn thấy trọn).

### Trạng thái một node

| Trạng thái | Hex | Icon | Link vào | Khác |
|---|---|---|---|---|
| Khoá (cha chưa có rank) | `_lockedHexColor` navy tối | `IconMuted` | `_lockedLinkColor` | badge khoá góc dưới phải |
| Mở, mua được ngay | màu nhánh (`SkillNodeData.Accent`) | Paper | accent | **Glow thở** (alpha 0.18↔0.55, 0.9 s) — "chạm vào đây" |
| Mở, thiếu coin | accent | Paper | accent | không glow |
| Max | accent | Paper | accent | tag MAX (Label_Tag01_Orange), 5 pip sáng |
| Đang chọn | + Ring màu accent, Body scale 1.1 | | | panel chi tiết hiện node này |

Màu duy nhất code "chọn" là accent lấy từ asset; mọi màu khác là field serialize trên view (`CODE-RULE.md` §4).

### Animation (DOTween, tất cả `SetUpdate(true)` + `SetLink`)

| Lúc nào | Gì | Ở đâu |
|---|---|---|
| Mở tab | Cây **mọc từ gốc**: link kéo dài (scaleX 0→1) rồi hex bung (scale 0→1 OutBack), lệch 0.06 s mỗi node theo thứ tự slot; panel chi tiết trượt lên + fade sau 0.3 s | `SkillTreePageView.PlayReveal` / `SkillTreeNodeView.PlayReveal` |
| Chạm node | Ring fade-in + Body scale lên 1.1 (OutBack), node cũ trả về; panel fade-out 0.16 s → đổi nội dung → trượt lên từ −22 px | `SetSelected`, `SkillDetailPanelView.Show(animate)` |
| Chạm node khoá | Body lắc ngang (DOShakeAnchorPos 0.3 s), panel vẫn mở và ghi "REQUIRES <cha>" màu Warning | `PlayDenied` |
| Mua rank | Body punch scale 0.25; Burst loé (alpha 0.9→0, scale 1→1.8); pip mới nảy từ 1.8→1; số `hiện tại → sau` punch | `PlayUpgrade`, `SkillDetailPanelView.Refresh` |
| Rank đầu tiên mở node con | Hex/icon/link của con tween từ màu khoá sang accent 0.35 s + punch nhẹ | `PlayUnlocked` |
| Nút bấm | UiButtonFx press-scale trên `Press` (tách khỏi `Body` để không đè lên scale chọn) | prefab |

`SkillTreePageView.Refresh` **diff** mảng `SkillNodeData` cũ/mới để biết node nào tăng rank, node nào vừa mở khoá — vì thế `MenuUiBinder.RefreshSkills` phải luôn cấp **mảng mới** (không ghi đè mảng đang được trang giữ).

### Ranh giới

Giữ nguyên luật của `docs/UI-SYSTEM.md`: UI chỉ nhận `SkillNodeData`, không biết `SkillTreeNodeSO`; mọi ref gameplay ở `MenuUiBinder`; trang được author inactive nên `Bind` chạy trước `Awake` (chỉ đụng field serialize).

⚠️ Hai bẫy lifecycle đã xử lý, đừng gỡ: (1) `SkillTreeNodeView` chỉ ghi scale sau khi `Awake` đã đọc `_restScale` (cờ `_awake`) — `Bind`/`SetSelected` từ trang inactive chỉ lưu cờ và bật ring; (2) khi bật tab, `OnEnable` của trang chạy **trước** `Awake` của các node con, nên `PlayReveal` được hoãn một tick bằng `DOVirtual.DelayedCall(0)`. Gọi thẳng sẽ tween scale 0→0 và cây biến mất.

---

## 5. Wiring (gán tay, không có tool)

- `GameplayRoot.prefab` → `Systems/Managers`: `ProfileService._skillTree` = `Data/SkillTree/SkillTree.asset`; component `SkillTreeStatApplier` với `_profile` = ProfileService, `_stats` = `PlayerStatSheet` trên `Player`.
- `UIRoot.prefab` → `Canvas_Menu/SafeArea/Pages/Page_SkillTree` ở đúng index 3 của `MenuScreenView._pages` và `MenuScreenView._skillTreePage`; `TabBar/Tab_SKILLS` (nhãn SKILLS).
- Sprite mới copy từ pack vào `Art/UI/Sprites/`: `Buttons/Btn_IconButton_Hexagon02_White1`, `Buttons/Btn_MenuButton_Hexagon01_s_White2`, và 11 pictogram `Icons/Icon_{Exp,Bullet_0,Thunder,Fist,Shooting,Emergency_Box,Life_Add,Boom,Bomb,Grenade_1,Tnt}`.

---

## 6. Thêm / sửa node

- **Đổi số** (giá, trần, giá trị mỗi rank, mô tả, màu): sửa `Node_*.asset`, không đụng code.
- **Thêm node** cần thêm cả một slot trên trang: tạo `Node_*.asset` (`Create ▸ Zombie War ▸ Skill Tree Node`), thêm vào `SkillTree._nodes` đúng vị trí, rồi author thêm `Node_*` + `Link_*` trong `Page_SkillTree/Content/Tree` và kéo vào `SkillTreePageView._nodes` cùng index. `SkillTreePageView` log error nếu hai đầu lệch số lượng.
- Node có `MaxRank > 5` cần thêm pip vào `_pips` của node view đó (view log error nếu thiếu).
- Stat mới → thêm `StatId` + đúng một chỗ đọc, và cập nhật bảng §5 của `docs/ROGUELIKE-SYSTEM.md`.

---

## 7. Trường hợp biên

| Tình huống | Xử lý |
|---|---|
| Mua khi menu đang mở popup khác | Không xảy ra: backdrop chặn raycast |
| Coin đổi vì nâng súng | `ProfileService.OnChanged` → `RefreshSkills` → node nào vừa đủ/hết tiền đổi glow ngay |
| Mua rank cuối của node cha rồi node con đã mở từ trước | `Refresh` chỉ animate node có rank tăng hoặc `Unlocked` chuyển false→true; node khác chỉ `Bind` |
| Asset tree rỗng / thiếu ref | `SkillTreeProgress` log error và chạy với 0 node; `ProfileService.Awake` cũng log thiếu ref |
| Prerequisite không nằm trong tree | `FirstMissingPrerequisite` log error và coi node là khoá |
| Reload scene (retry / next) | Rank đọc lại từ PlayerPrefs qua `EnsureLoaded`; applier `Push` ở `OnEnable` |

---

## 8. Đã kiểm chứng những gì

- Compile toàn bộ `Assets/_ZombieWar/Scripts` (126 file) bằng Roslyn của Unity với reference/define lấy từ `ZombieWar.csproj`: 0 lỗi.
- Prefab: 1574 block, không có `fileID` treo; `MenuScreenView._pages[3]` vẫn là GameObject cũ (giữ fileID) nên thứ tự `MenuTab` không đổi.
- Bố cục kiểm bằng ảnh chụp Game view 1080×1920 trong Editor (edit mode, `Canvas_Menu` tạm chuyển `ScreenSpaceCamera`, bind data mẫu qua `execute_code`, rồi `OpenScene` lại không save): `Captures/skilltree_final.png` (thư mục gitignore, chỉ có trên máy đã chụp). Đủ chỗ cho 9:16, không phần tử nào đè nhau, panel chi tiết không chạm pip của gốc.
- **Chưa play test**: animation (reveal, glow thở, punch/burst khi mua, lắc khi khoá) chỉ được review theo code vì chụp ở edit mode không chạy DOTween; nhịp animation chỉnh bằng field `[Header("...")]` trên `SkillTreeNodeView` / `SkillDetailPanelView` / `SkillTreePageView`, không sửa code.
