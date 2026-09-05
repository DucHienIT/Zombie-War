# UI System — Zombie War

Ghi lại kiến trúc UI, bảng màu và các quy tắc thị giác của project. Trạng thái mô tả ở đây là commit `f5e5c938` (đợt re-skin sang GUI PRO Kit - Sci-Fi Survival + dọn bớt khung).

CLAUDE.md chỉ tóm tắt; file này là bản đầy đủ. Khi hai file lệch nhau, sửa cả hai.

## 1. Nguyên tắc bất di bất dịch

Bốn luật dưới đây là lý do hệ UI trông như hiện tại. Phá luật nào thì phải sửa lại cả nhánh tương ứng.

1. **UI không giữ reference gameplay.** `UIManager` không biết `GameState`, không biết `LevelDefinitionSO`, không biết `AudioService`. Mọi reference bắc cầu nằm ở **binder** sống ngoài prefab UI, và **chỉ trỏ vào trong** UI, không bao giờ ngược lại.
2. **Toàn bộ UI nằm trong một prefab duy nhất** — `Assets/_ZombieWar/Prefabs/UI/UIRoot.prefab`. Không reference nào từ trong prefab trỏ ra ngoài, kể cả `EventSystem` và `AudioSource` cũng nằm bên trong. Nhờ vậy thả instance vào scene nào cũng chạy.
3. **Prefab là nguồn sự thật.** Bộ editor tool sinh UI đã bị xoá (quyết định user 2026-09-05). Không có menu "rebuild" nào nữa; sửa trực tiếp trong Editor hoặc qua Unity MCP.
4. **Art UI chỉ đến từ đúng một pack**: `Assets/ThirdParty/GUI PRO Kit - Sci-Fi Survival/`. Asset dùng thật phải copy vào `Assets/_ZombieWar/Art/UI/`, không tham chiếu xuyên qua `ThirdParty/`.

Stack: **uGUI + TextMeshPro + DOTween**. Không UI Toolkit, không MVVM framework, không DI container, không singleton — composition root nối bằng Inspector.

## 2. Cây prefab

```
UIRoot.prefab               [UIManager]        ← hub duy nhất
├── EventSystem             [EventSystem, InputSystemUIInputModule]
├── UiAudio                 [AudioSource]      ← UI tự phát tiếng tap của mình
├── Canvas_HUD      order 0   → SafeArea/{TopBar, Joystick, GunButton}
├── Canvas_Menu     order 5  [MenuScreenView]  → Background, Vignette,
│                                                SafeArea/{Header, Pages, TabBar}
├── Canvas_Countdown order 10 [CountdownView]  ← KHÔNG có GraphicRaycaster
└── Canvas_Popup    order 20 [PopupManager]    → Backdrop + 5 popup (inactive)
```

Canvas tách theo **tần suất rebuild**: HUD đổi mỗi frame, menu gần như tĩnh, countdown chỉ sống vài giây, popup mở theo sự kiện. Gộp chung thì mỗi lần thanh máu đổi sẽ rebuild lại cả menu.

`Canvas_Countdown` cố ý không có `GraphicRaycaster` — nó phủ toàn màn hình, có raycaster là nuốt mất input joystick.

**Popup là prefab riêng** trong `Prefabs/UI/Popups/`, được đặt sẵn thành instance inactive trong `Canvas_Popup`, **không bao giờ `Instantiate` lúc runtime**:

| Prefab | Class |
|---|---|
| `PausePopup` | `PausePopupUI` |
| `ResultPopup` | `ResultPopupUI` |
| `SkillChoicePopup` | `SkillChoicePopupUI` (+ `SkillCardView`) |
| `WeaponDetailPopup` | `WeaponDetailPopupUI` (+ `WeaponStatCellView`) |
| `SettingsPopup` | `SettingsPopupUI` |

## 3. Lớp binder — ranh giới giữa gameplay và UI

Binder sống ở `GameplayRoot/Systems/Managers`, ngoài prefab UI.

| Binder | Giữ ref | Việc |
|---|---|---|
| `Core/GameplayUiBinder` | `UIManager`, `GameFlowController`, `PlayerHealth`, `WeaponController`, `BombThrower`, `CameraShakeController`, `RoguelikeDirector` | subscribe event gameplay → gọi setter của `UIManager`; đổi `LevelResult` thành `ResultData`; trao lệnh cho UI qua `BindGameplayCommands` |
| `Core/MenuUiBinder` | `UIManager`, `GameFlowController`, `ProfileService`, `CameraShakeController`, `LevelDefinitionSO[]` | khi flow vào state `Menu`: dựng `MenuHeaderData` + `LevelCardData[]` + `WeaponEntryData[]` rồi `ShowMenuScreen`; PLAY → `StartRun`; UPGRADE → `TryUpgradeGun` |

**Dữ liệu vào UI là struct trình bày**, không phải object gameplay: `ResultData`, `LevelCardData`, `MenuHeaderData`, `WeaponEntryData`, `WeaponStatsData`, `SkillCardData`.

`UIManager` chỉ phơi ra setter và lệnh mở popup:

```
BindGameplayCommands(onPause, onSwitchGun, onThrowBomb)
ShowGameplayScreen() / ShowMenuScreen(...) / RefreshMenu(...)
SetHudVisible / SetPauseButtonInteractable
SetHealth / SetRemainingTime / SetScore / SetXp
SetGun
ShowCountdown / SetCountdownSeconds
ShowPausePopup / ShowResultPopup / ShowSkillChoicePopup / ShowSettingsPopup
PlayTap
```

Mọi callback đi ra ngoài đều được bọc qua `Wrap(Action)` để tự phát tiếng tap — UI tự lo âm thanh của nó, không mượn `AudioService`.

## 4. View

View là **presenter câm**: nhận dữ liệu, vẽ, không quyết định gì. Nút nối runtime bằng `Init(callback)` / `Setup(callbacks)`, **không có persistent onClick nào trên prefab**.

| Nhóm | Class |
|---|---|
| HUD | `HealthBarView`, `XpBarView`, `TimerView`, `ScoreView`, `GunHudView` (icon + tên súng, không có số đạn — súng bắn vô hạn từ 2026-09-05), `CountdownView` |
| Menu | `MenuScreenView`, `MenuHeaderView`, `MenuTabBarView`, `MenuTabButtonView`, `BattlePageView`, `LevelCardView`, `WeaponPageView`, `WeaponCardView`, `SkillTreePageView`, `SkillTreeNodeView`, `SkillDetailPanelView` (xem `docs/SKILL-TREE.md`) |
| Popup | `PopupBase`, `PopupManager`, `PopupBackdrop`, + 5 class popup, `SkillCardView`, `WeaponStatCellView` |
| Dùng chung | `SwitchToggleView`, `SafeAreaFitter`, `UiButtonFx`, `TimeTextFormatter` |

`MenuTab` enum quyết định thứ tự: `Shop, Weapon, Battle, SkillTree, Gacha`. Mảng `MenuScreenView._pages` **phải khớp thứ tự enum này**, và `_defaultTab` = `Battle`. Tab `Gacha` (nhãn LOCK) đang khoá bằng `MenuTabButtonView._locked`.

Trang không phải Battle được author **inactive**, nên `Awake` của chúng chạy lần đầu khi mở tab. `Bind` chỉ đụng field serialized nên gọi trước `Awake` vẫn đúng.

## 5. Popup stack

Mở/đóng **chỉ qua `PopupManager`**: stack + draw order + backdrop dùng chung + phím Back (`Keyboard.escapeKey`, Android map sang Escape).

- Mọi đường đóng đi qua `PopupBase.OnClosed` → callback không thể bị bỏ sót.
- `Restack()` đặt sibling order = draw order, backdrop chèn ngay dưới popup trên cùng có `_dimBackground`.
- Tween popup dùng `SetUpdate(true)` vì popup mở khi `timeScale = 0`.
- `PlayShow()` gọi `DOTween.Kill(_canvasGroup, true)` với `complete = true`, để `OnClosed` của lần đóng dở không bị nuốt.
- `PopupBase._fitPadding` co popup cao lại trên màn hình aspect thấp.
- Popup chọn skill tắt cả `_closeOnBackdropClick` lẫn `_closeOnBackKey` — bắt buộc phải chọn. Popup kết quả cũng tắt back key.

## 6. Skin

### Font

| Vai | Asset |
|---|---|
| Title, số | `Oxanium-ExtraBold_Extended ASCII SDF` |
| Body, nhãn | `Ubuntu-Bold_Extended ASCII SDF` |

Cả hai là TMP SDF có atlas nằm trong chính file `.asset`. Copy bằng Editor (`AssetDatabase.CopyAsset` / Ctrl+D), copy bằng file system là mất atlas và mọi chữ render ra trắng trơn — lỗi này **im lặng lúc runtime**.

### Bảng màu

Bộ tool sinh UI đã xoá nên bảng màu giờ **chỉ tồn tại dưới dạng màu đã serialize** rải trên ~189 graphic. Ghi lại ở đây để đổi tông sau này còn biết tìm gì:

| Tên | Hex | Dùng cho |
|---|---|---|
| Paper | `#FFFFFF` | chữ chính, tint sprite giữ nguyên màu |
| Ink | `#07101D` | chữ trên nền sáng (pill, tag sáng) |
| HudLabel | `#8FA6C4` | nhãn phụ, chữ mô tả |
| Danger | `#FF4B5C` | máu thấp, sát thương, nút MAIN MENU |
| Warning | `#FFC44D` | coin, sao, giá tiền, cảnh báo |
| Good | `#3DE0A0` | thắng, chỉ số sau nâng cấp, nút xanh |
| XpAccent | `#3FC8FF` | badge battle level, thanh XP |
| IconOnDark | `#DCEAFF` | icon trên nền tối |
| IconMuted | `#4E5E75` | icon/chữ bị khoá |
| Backdrop | `#030811` @ 80% | nền mờ sau popup |

### Thư mục

`Art/UI/Fonts/` + `Art/UI/Sprites/{Buttons, Frames, Popups, Sliders, Labels, Icons, Toggles, Backgrounds}` — 83 file.

Ngoại lệ duy nhất không thuộc pack: 2 icon súng ở `Art/Sprites/Icon_*.png` là **AssetPreview render từ chính model súng của game**.

## 7. Quy tắc thị giác: một tầng khung

Yêu cầu user 2026-09-05: *"UI đang quá nhiều frame và bg gây đầy mắt"*. Trước đợt sửa, popup skill có 3 tầng hộp có viền lồng nhau, popup kết quả có 8 hộp trong 1 hộp, HUD có 4.

**Luật: mỗi màn chỉ một tầng khung.** Phần tử lồng bên trong khung đó dùng nền phẳng mờ hoặc không nền, và để **khoảng trắng + typography** gánh việc phân nhóm.

| Vai | Xử lý |
|---|---|
| Khung ngoài cùng của màn (panel popup, tab bar, thẻ vũ khí) | giữ sprite khung |
| Điểm nhấn duy nhất của một khối (đế hexagon icon skill, khung icon súng) | giữ |
| Track của thanh (máu, XP, tiến độ) | giữ — đây là khung **chức năng** |
| Chip/row/plate lồng bên trong | bỏ sprite → nền phẳng mờ, hoặc tắt hẳn Image |

Giá trị nền phẳng đang dùng — màu `#8CB8F2`, khác nhau ở alpha:

| Alpha | Dùng cho |
|---|---|
| `0.00` | thẻ chọn skill (cột tự tách bằng khoảng trắng) |
| `0.055` | hàng chỉ số popup kết quả, ô cài đặt, lưới chỉ số súng |
| `0.10` | track segmented NORMAL/HARD |
| `0.13` | hàng TOTAL (nhấn mạnh, không viền) |

Nền menu: ảnh `Background_10` tint `#566B8F`, vignette `Background_ScreenGlow` alpha 0.55, glow sau artwork alpha 0.10.

HUD **không còn tấm nền trên đầu** — chữ và thanh trôi thẳng trên map. Kéo theo hai điều chỉnh bắt buộc:

- Track thanh XP phải **mờ** (`IconMuted` @ 55%). Để nguyên navy đặc thì trên map sáng nó thành một vệt đen giữa màn hình.
- Bỏ khung thì icon và số **rời nhau ra**, vì trước đó cái khung là thứ giữ chúng lại. Phải thu hẹp bề rộng chip để cặp icon–số dính lại (`TimerChip` 252, `KillsChip` 145, `ScoreChip` 200, `CoinChip` 150, `BestScore` 170).

⚠️ Alpha **không** ảnh hưởng raycast. Nền nút để alpha 0 vẫn bấm được, miễn `raycastTarget = true`. Sau mỗi lần đụng vào nền nút, kiểm lại không có `Button` nào mất `targetGraphic` hoặc mất `raycastTarget`.

## 8. Bẫy đã trả giá

### uGUI

- **Thanh máu chạy bằng anchor** (`DOAnchorMax`), **không** dùng `Image.Type = Filled`. Sprite pill 9-slice bị Filled cắt cụt đầu bo; và anchor đọc đúng ngay frame đầu khi `CanvasScaler` chưa kịp size canvas.
- **`CanvasScaler` match theo WIDTH** (`matchWidthOrHeight = 0`, reference 1080×1920) ở mọi canvas. Màn cao hơn chỉ được thêm khoảng trống, UI không đổi kích thước vật lý.
- Tween mở popup phải `SetUpdate(true)`.
- `Toggle` của uGUI chỉ fade được **một** graphic, trong khi switch của pack là frame + handle cho mỗi trạng thái → `SwitchToggleView` bật/tắt hai nhánh visual `On`/`Off`.

### Sprite của pack Sci-Fi Survival

- **Rất nhiều sprite có border phủ hết kích thước texture** (ví dụ `Popup_Frame01_Navy1` 441×709, border 222/352/219/357). Đó là chủ ý: vùng giữa rộng 0 pixel nên khi kéo giãn nó lấy màu tại đúng cột/hàng biên. Cứ để `Image.Type = Sliced`, `pixelsPerUnitMultiplier = 1`. Khi chọn sprite mới phải **kiểm tra cột được lấy mẫu có đặc không** — với fill của slider, cột đó phải opaque, không thì thanh sẽ trong suốt ở giữa.
- `Frame_BarFrame_Top01_Navy` / `Top02_*` / `Bottom01_Navy` có border **dọc bằng nguyên chiều cao** — chúng là dải mảnh vẽ sát mép màn hình, kéo cao ra là hỏng. Panel/bar cao dùng `Frame_TableFrame01_Navy` hoặc `Frame_BannerFrame04_Navy01` (slice được cả hai chiều).
- `Btn_TextButton_Square01_*` (174×188) **không** slice theo chiều dọc — giữ chiều cao nút quanh 140–190.
- Pictogram `Icon_FunctionIcons(x2)` **chỉ giải nén sẵn bản 64 px** (128/256/512 còn nằm trong `.unitypackage`). Chỗ nào vẽ icon lớn hơn ~120 px thì lấy `Icon_ItemIcons(x2)/Shadow_256`.
- `Icon_Gold` là **vòng tròn rỗng phẳng** — chỉ đọc ra "đồng xu" khi tint `Warning`. Tint trắng ra một chấm trắng vô nghĩa.
- `Label_Tag01_SkyBlue` gần như trắng → chữ trên nó phải là `Ink`. Các tag Green/Red/Blue dùng chữ `Paper`.
- Nút hành động trong trận (`Play_Joystick_*`) là **ảnh vuông**, rect phải vuông, đừng bù offset cho bóng.

## 9. Quy trình sửa và mở rộng

Không còn tool sinh UI, nên tất cả làm tay:

- **Thêm sprite/font UI**: copy đúng file cần dùng từ pack sang thư mục loại trong `Art/UI/` **bằng Unity** (giữ import settings + border 9-slice).
- **Thêm level**: `LevelDefinitionSO` mới vào `Data/Levels/` → kéo vào `MenuUiBinder._levels`. Trang Battle là carousel một thẻ nên không cần thêm slot UI.
- **Thêm súng**: `GunDefinitionSO` mới vào `Data/Weapons/` + prefab `Gun` dưới `hand_r/GunSocket` → kéo vào `ProfileService._guns` (giữ thứ tự theo tên) và `WeaponController._guns`. Trang Weapon đã author sẵn 6 `WeaponCardView`.
- **Thêm skill roguelike**: chỉ thêm `PassiveSkillSO` asset, không sửa code — trừ khi cần một hiệu ứng không biểu diễn được bằng `StatModifier`.
- **Wiring trong `GameplayRoot.prefab` phải giữ**: `GameplayUiBinder` / `MenuUiBinder` trỏ vào `UIManager` của instance `UIRoot`; `LevelMapLoader._virtualCamera`; `ProfileService` nối vào `GameFlowController._profile` / `WeaponController._profile` / `MenuUiBinder._profile`; object `Player` tắt sẵn.

Prefab save là **ghi đè cả file**, nên khi nhiều người cùng làm phải hỏi trước khi sửa `UIRoot.prefab`.

## 10. Cố ý không có

- Không nhạc nền (không pack nào có track).
- Không vignette khi trúng đòn.
- Chữ HUD **không có viền/đổ bóng** — trên nền đất sáng độ tương phản sẽ giảm. Nếu cần khắc phục mà không dựng lại khung: thả một dải gradient tối mờ dần ở mép trên.
- Tab Shop hiện là placeholder "COMING SOON"; tab thứ 5 khoá cứng. Tab SKILLS (trước là TALENT) là cây kỹ năng vĩnh viễn — `docs/SKILL-TREE.md`.
