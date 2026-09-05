# Roguelike choice — hệ thống chọn kỹ năng trong trận

Tài liệu hệ thống roguelike của **Zombie War**. Viết ngày 2026-09-05, sau khi hệ thống đã nối xong và compile sạch.

> Hệ thống này **nằm ngoài spec** `Zombie_War_SPEC_GDD_V1.0.docx` (spec cố ý không có roguelike). User yêu cầu thêm sau khi P0 xong, và tự để phần thiết kế skill cho phía implement quyết định.

---

## 1. Tóm tắt

Giết quái → nhận **XP** → đầy thanh thì lên **battle level** → game dừng lại và hiện popup **chọn 1 trong 3 passive skill**. Battle level chỉ sống trong đúng một lượt chơi và mất khi hết màn — khác hẳn level tài khoản của hệ meta progression (mua nâng cấp súng bằng coin).

Hiện có **6 passive skill**. Tất cả đều là **stat modifier thuần**, nên thêm skill thứ 7 chỉ cần thêm một file `.asset`.

---

## 2. Vòng đời một lần level-up

```
ZombieManager.OnZombieKilled
        │
        ▼
RoguelikeDirector.HandleZombieKilled
   • hồi máu nếu có HealPerKill
   • BattleXpTracker.AddXp(xpReward × XpGain)   → trả về số cấp vừa lên
   • OnXpChanged  ─────────────────────────────► GameplayUiBinder → UIManager.SetXp → XpBarView
        │
        ▼ (còn nợ level-up, và flow đang Playing)
RoguelikeDirector.Update → TryOpenChoice
   • SkillDraft.Roll → N thẻ KHÁC NHAU, bỏ skill đã kịch trần
   • OnChoiceOffered(offers, count, battleLevel)
        │
        ▼
GameplayUiBinder.HandleChoiceOffered
   • dựng SkillCardData[] (UI không bao giờ thấy PassiveSkillSO)
   • GameFlowController.PauseForLevelUp()   → state LevelUp, timeScale = 0
   • UIManager.ShowSkillChoicePopup(...)
        │
        ▼ người chơi chạm thẻ
SkillChoicePopupUI.Close() → (0.18 s unscaled) → OnClosed → RoguelikeDirector.ChooseOffer(index)
   • cộng stack, PlayerStatSheet.BeginRebuild/Apply/EndRebuild
   • còn nợ  → mở luôn bộ thẻ kế tiếp (không thả timeScale ra giữa chừng)
   • hết nợ  → OnChoiceClosed → GameFlowController.ResumeFromLevelUp()
```

### Vì sao director không tự dừng game

`RoguelikeDirector` **chỉ phát event**, không đụng `Time.timeScale` và không gọi thẳng UI. Việc đổi state là của `GameFlowController` — đúng luật "state machine trung tâm" của `CODE-RULE.md` §3.

Lợi ích cụ thể: `PlayerMotor`, `WeaponController`, `WaveDirector`, `ZombieManager`, `BombThrower` **đều đã gate sẵn trên `State == Playing`** từ trước, nên khi state nhảy sang `LevelUp` chúng tự đứng im — không phải sửa một dòng nào trong số đó.

### Vì sao `LevelUp` là state riêng, không mượn `Paused`

`GameplayUiBinder.HandleStateChanged` mở popup Pause khi thấy state `Paused`. Nếu mượn state đó thì popup Pause sẽ bung ra đè lên popup chọn skill.

---

## 3. Kiến trúc

### File

```
Scripts/Data/
├── StatId.cs                     # enum 12 stat mà passive được phép động vào
├── StatModifierKind.cs           # Additive | Multiplicative
├── StatModifier.cs               # struct (StatId, Kind, ValuePerStack)
├── PassiveSkillSO.cs             # 1 skill = identity + maxStacks + StatModifier[]
└── RoguelikeSettingsSO.cs        # pool + số thẻ mỗi lần + đường cong XP

Scripts/Player/
└── PlayerStatSheet.cs            # MonoBehaviour: giá trị stat sống, trên object Player

Scripts/Roguelike/
├── BattleXpTracker.cs            # C# thuần: XP → battle level
├── SkillDraft.cs                 # C# thuần: bốc N thẻ khác nhau, có trọng số
└── RoguelikeDirector.cs          # MonoBehaviour: điều phối, trên Systems/Managers

Scripts/UI/
├── SkillCardData.cs              # struct trình bày (UI không thấy SO)
├── Hud/XpBarView.cs
└── Popup/{SkillChoicePopupUI,SkillCardView}.cs

Data/Roguelike/
├── RoguelikeSettings.asset
└── Skill_{HighCaliber,RapidFire,PiercingRounds,Adrenaline,BloodPact,Demolitionist}.asset
```

### Trách nhiệm

| Class | Việc | Ghi chú |
|---|---|---|
| `BattleXpTracker` | Cộng XP, trả về số cấp vừa lên, tính `Normalized` cho thanh XP | C# thuần → test được không cần scene |
| `SkillDraft` | Bốc N thẻ khác nhau theo `DraftWeight`, loại skill đã kịch trần | C# thuần, zero-alloc (dùng lại `List<int>` candidate) |
| `PlayerStatSheet` | Gộp mọi stack đang sở hữu thành 12 giá trị stat sống | Multiplicative nghỉ ở 1, additive nghỉ ở 0 |
| `RoguelikeDirector` | Nghe kill, cộng XP, quản nợ level-up, bốc thẻ, cộng stack, rebuild stat | Không đụng UI, không đụng timeScale |
| `GameplayUiBinder` | Đổi `PassiveSkillSO` → `SkillCardData`, ra lệnh pause/resume | Lớp duy nhất giữ ref cả hai phía |

### API công khai

```csharp
// RoguelikeDirector
event Action<float, int>                    OnXpChanged;      // normalized, battle level
event Action<PassiveSkillSO[], int, int>    OnChoiceOffered;  // offers, count, battle level
event Action                                OnChoiceClosed;
void ChooseOffer(int index);
int  StacksOf(PassiveSkillSO skill);

// PlayerStatSheet
event Action OnChanged;                       // bắn MỘT lần sau khi cả sheet đã settle
float Multiplier(StatId stat);                // nghỉ ở 1
float Additive(StatId stat);                  // nghỉ ở 0
void BeginRebuild(); void Apply(StatModifier[] modifiers, int stacks); void EndRebuild();
```

`OnChanged` cố ý chỉ bắn ở `EndRebuild` — nếu bắn sau mỗi `Apply` thì `PlayerHealth` sẽ đọc phải một build dở dang và cộng nhầm máu.

---

## 4. Thiết kế 6 passive skill

Tất cả đều là stat modifier. Cột "Mỗi cấp" là giá trị cộng/nhân cho **một** stack; nhiều stack thì additive cộng dồn, multiplicative **nhân dồn** (2 cấp `1.2` → `1.44`, không phải `1.4`).

| Skill | Mỗi cấp | Trần | Icon | Màu |
|---|---|---|---|---|
| **HIGH CALIBER** | `WeaponDamage ×1.20`, `Knockback ×1.15` | 5 | `Pictoicon_Attack` | cam `#FF9B3D` |
| **RAPID FIRE** | `FireRate ×1.15`, `ReloadSpeed ×1.135` | 5 | `Pictoicon_Thunder` | vàng `#FFD54A` |
| **PIERCING ROUNDS** | `ProjectilePierce +1` | 3 | `Pictoicon_Missile` | lam `#4ECDE0` |
| **ADRENALINE** | `MoveSpeed ×1.10`, `XpGain ×1.08` | 4 | `Pictoicon_Boot_Fly` | lục `#5CE08B` |
| **BLOOD PACT** | `MaxHealth +15`, `HealPerKill +2` | 4 | `Pictoicon_Life_Add` | đỏ `#FF5B4A` |
| **DEMOLITIONIST** | `BombCharges +1`, `BombRadius ×1.12`, `BombDamage ×1.20` | 3 | `Pictoicon_Boom` | tím `#B47CFF` |

**Ý đồ thiết kế**: mỗi skill chạm một hệ thống khác nhau để mỗi lần chọn là một quyết định về lối chơi, không phải chọn "cái nào to số hơn".

- Sát thương trực tiếp (HIGH CALIBER) vs nhịp bắn (RAPID FIRE) vs xử lý đám đông (PIERCING ROUNDS)
- Cơ động + cuộn tuyết XP (ADRENALINE)
- Sinh tồn (BLOOD PACT) — nguồn hồi máu **duy nhất** trong trận, nên nó là lựa chọn "cứu mạng" thật sự
- Bùng nổ theo nhịp (DEMOLITIONIST)

**Tổng trần = 5+5+3+4+4+3 = 24 stack**, trong khi cả lượt chỉ đi được tối đa 11 lần chọn (battle level 12). Người chơi **không bao giờ full được** — đó là lý do lựa chọn có sức nặng.

### Số cân bằng

`Data/Roguelike/RoguelikeSettings.asset`:

| Nút vặn | Giá trị | Ý nghĩa |
|---|---|---|
| `_offersPerLevelUp` | 3 | Số thẻ mỗi lần chọn (≤ số slot thẻ author trong popup) |
| `_baseXpToLevel` | 80 | XP để đi từ battle level 1 lên 2 |
| `_xpGrowthPerLevel` | 1.35 | Mỗi cấp đắt hơn cấp trước 35% |
| `_maxBattleLevel` | 12 | Trần |

`ZombieDefinitionSO._xpReward`: Walker **10**, Runner **14**, Brute **30**, Giant **90**.

**Nhịp đã đo** (chạy `BattleXpTracker` với nhịp kill thật của Level 1, ~280 kill/lượt ≈ 4090 XP):

```
280 kills, 4092 xp -> battle level 10 (9 lần chọn skill, trần 12)
```

Tức khoảng **20 giây một lần chọn** trong màn 3 phút.

---

## 5. Stat nào được đọc ở đâu

Đây là danh sách **đầy đủ**. Thêm `StatId` mới thì bắt buộc bổ sung vào bảng này, nếu không sẽ có stat được cộng nhưng không ai đọc.

| StatId | Kiểu | Nơi tiêu thụ |
|---|---|---|
| `WeaponDamage` | nhân | `WeaponController.Fire` → `ShotStats.Damage` |
| `Knockback` | nhân | `WeaponController.Fire` → `ShotStats.Knockback` |
| `ProjectilePierce` | cộng | `WeaponController.Fire` → `ShotStats.Pierce` → `Projectile.TryPierce` |
| `FireRate` | nhân | `WeaponController.Fire` (chia `FireInterval`) |
| `ReloadSpeed` | nhân | `WeaponController.BeginReload` (chia `ReloadDuration`) |
| `MoveSpeed` | nhân | `PlayerMotor.FixedUpdate` |
| `MaxHealth` | cộng | `PlayerHealth.Max` + `HandleStatsChanged` |
| `HealPerKill` | cộng | `RoguelikeDirector.HandleZombieKilled` → `PlayerHealth.Heal` |
| `BombCharges` | cộng | `BombThrower.HandleStatsChanged` |
| `BombRadius` | nhân | `BombThrower.RequestThrow` → `Bomb.BlastRadius` |
| `BombDamage` | nhân | `BombThrower.Explode` |
| `XpGain` | nhân | `RoguelikeDirector.HandleZombieKilled` |

### ⚠️ Thứ tự nhân với meta progression

Hệ số roguelike nhân **lên trên `gun.Stats`** (đã bao gồm cấp nâng cấp mua bằng coin), **không** nhân lên `gun.Definition`:

```csharp
gun.Stats.Damage * _stats.Multiplier(StatId.WeaponDamage)   // đúng
gun.Definition.Damage * _stats.Multiplier(...)              // SAI: vô hiệu hoá toàn bộ shop
```

Ba chỉ số `Damage` / `FireInterval` / `ReloadDuration` đều đi qua `gun.Stats`. Riêng `Knockback`, `Range`, `SpreadAngle`, `PelletCount`, VFX vẫn lấy thẳng từ `definition` vì shop không đụng tới chúng.

### Hai stat áp dụng theo delta

`MaxHealth` và `BombCharges` không thể đọc lười mỗi frame — chúng phải **trao ngay tài nguyên** lúc người chơi bấm chọn:

- `PlayerHealth.HandleStatsChanged`: cộng đúng phần chênh vào máu hiện tại. Không làm vậy thì "+15 máu tối đa" chỉ nới rộng phần rỗng của thanh máu — vô dụng đúng lúc cần nhất.
- `BombThrower.HandleStatsChanged`: cộng đúng phần chênh vào số bom đang cầm, phát `OnChargesChanged` để HUD cập nhật.

Cả hai đều nhớ `_appliedBonus` của lần trước để chênh lệch tính đúng khi rebuild nhiều lần.

---

## 6. UI

### Thanh XP (HUD)

Nằm trong `TopBar` ngay dưới thanh máu: badge tròn hiện battle level + thanh fill xanh dương. Chạy bằng **anchor** (`DOAnchorMax`) chứ không phải `Image.Type = Filled` — cùng lý do với thanh máu: sprite pill 9-slice bị cắt cụt đầu bo nếu dùng Filled, và anchor đọc đúng ngay frame đầu khi `CanvasScaler` chưa kịp size canvas. Fill được author sẵn ở 0 để không loé một frame đầy thanh lúc vào trận.

### Popup chọn skill

`Prefabs/UI/Popups/SkillChoicePopup.prefab` — **3 cột dọc xếp ngang**, mỗi thẻ: tên → badge NEW → icon → mô tả → hàng sao.

- **Hàng sao thay cho chữ "LV 3/5"**: số sao vàng = cấp **sau khi chọn**, tổng số sao = `MaxStacks`. Nhìn phát biết skill còn sâu bao nhiêu, không phải đọc và trừ nhẩm giữa lúc đang bị vây.
- Prefab author sẵn **5 slot sao** trong mảng `SkillCardView._stars` (bằng trần sâu nhất trong pool). Skill nông hơn ẩn bớt sao và `SkillCardView.DrawStars` dịch cả hàng lại cho vẫn cân giữa.
- Badge **NEW** hiện khi `SkillCardData.IsNew` (chưa sở hữu stack nào).
- Icon được **tô màu accent của skill**. Sprite nền đĩa tròn của pack gần như đen nên tint lên nó không ăn thua; phải tô lên chính icon nét trắng.
- Popup tắt cả `_closeOnBackKey` lẫn `_closeOnBackdropClick` — **bắt buộc phải chọn**.

**Mô tả skill phải ngắn, mỗi dòng một hiệu ứng** (`Damage +20%\nKnockback +15%`) vì cột chỉ rộng 300 px. Văn xuôi dài sẽ tràn.

### Ranh giới UI

UI **không giữ reference nào tới gameplay**. `SkillChoicePopupUI` chỉ nhận `SkillCardData` — nó không biết `PassiveSkillSO` là gì, y như `ResultPopupUI` không biết `LevelResult`. Việc quy đổi nằm ở `GameplayUiBinder`. Nhờ vậy toàn bộ UI vẫn nằm gọn trong `UIRoot.prefab` không có ref trỏ ra ngoài.

**Hợp đồng của popup**: `Setup()` chỉ được phép **nhận data**, phần vẽ nằm ở `OnShowing()`. Lý do: popup nằm inactive giữa các lần level-up, `Awake` chỉ chạy khi `PlayShow()` gọi `SetActive(true)` — vẽ trong `Setup()` sẽ đọc phải field mà `Awake` chưa kịp tạo. Đây là lỗi `NullReferenceException` đã gặp thật và đã sửa.

---

## 7. Wiring trong prefab

Hệ này **không có editor tool** (quyết định user 2026-09-05: không giữ script sinh object trong repo — xem `CLAUDE.md`, mục "Không giữ editor tool sinh object"). Prefab và asset là nguồn sự thật; mọi reference dưới đây gán tay trong `GameplayRoot.prefab`:

- `PlayerStatSheet` trên `Player`.
- `RoguelikeDirector` trên `Systems/Managers`: `_settings` = `Data/Roguelike/RoguelikeSettings.asset`, `_flow`, `_zombies`, `_stats`, `_playerHealth`.
- `_stats` cho `PlayerHealth`, `PlayerMotor`, `WeaponController`, `BombThrower`.
- `_rogue` cho `GameplayUiBinder`.
- `RoguelikeSettings._skillPool` liệt kê 6 `Skill_*.asset`; icon là `Item_Icon_*` 256 px của GUI PRO Kit - Sci-Fi Survival đã copy vào `Art/UI/Sprites/Icons/`.

`SkillChoicePopup.prefab` sửa thẳng trong Prefab Mode; `UIRoot.prefab` chứa nó dưới `Canvas_Popup`.

---

## 8. Thêm skill thứ 7

Trường hợp thường gặp — **không phải sửa dòng code nào**:

1. Tạo asset bằng `Create ▸ Zombie War ▸ Passive Skill` trong `Data/Roguelike/` (id, tên, mô tả, icon, màu, trần `MaxStacks`, danh sách `StatModifier`).
2. Kéo asset đó vào mảng `_skillPool` của `RoguelikeSettings.asset`.

**Chỉ khi** hiệu ứng mới không biểu diễn được bằng stat có sẵn (nổ dây chuyền khi kill, hồi sinh, aura, đạn nảy…) thì mới phải:

1. Thêm một entry vào `StatId`
2. Đọc nó ở **đúng một chỗ** trong hệ thống liên quan
3. Bổ sung vào bảng §5 của tài liệu này

Ràng buộc còn lại: skill có `MaxStacks > 5` thì phải thêm slot sao vào mảng `_stars` của cả ba `SkillCardView` trong `SkillChoicePopup.prefab`. `SkillCardView` sẽ log error nếu quên chứ không im lặng hỏng.

---

## 9. Các trường hợp biên đã xử lý

| Tình huống | Xử lý |
|---|---|
| Lên 2 cấp cùng lúc (giết Giant) | Nợ được xếp hàng; chọn xong thẻ đầu là mở luôn bộ thứ hai, không thả `timeScale` ra giữa chừng |
| Mọi skill đã kịch trần | `SkillDraft.Roll` trả 0 → director xoá nợ và không hỏi nữa |
| Chết đúng lúc lên cấp | `HandleZombieKilled` và `Update` đều gate `State == Playing`; `OnLevelEnded` xoá sạch nợ |
| Hết giờ khi popup đang mở | Không xảy ra: `timeScale = 0` nên `LevelTimer` không chạy |
| Bấm nút bom/đổi súng khi popup mở | Backdrop chặn raycast, và `RequestThrow`/`RequestSwitch` vẫn tự gate `State == Playing` |
| Chơi lại / sang màn mới | Scene reload; `OnRunStarted` reset tracker, xoá stack, rebuild stat sheet về mặc định |
| Đạn xuyên gặp lại đúng con vừa xuyên | `Projectile.LastPiercedCollider` bỏ qua collider đó ở frame sau, một viên không tiêu hết pierce vào một con |

---

## 10. Đã kiểm chứng những gì

Chạy thẳng class thuần qua Unity MCP (không cần Play mode):

```
280 kills, 4092 xp   -> battle level 10 (9 lần chọn, trần 12)
400 lượt bốc thẻ     -> 0 thẻ trùng
4/6 skill đã max     -> chỉ offer 2 thẻ còn lại
tất cả đã max        -> 0 thẻ
HighCaliber ×2       -> damage ×1.440   (nhân dồn, không cộng)
Demolitionist ×3     -> bom +3, radius ×1.405
sau BeginRebuild     -> damage ×1, bom +0
```

Layout kiểm bằng ảnh chụp canvas qua `ScreenSpaceCamera` (`Captures/rogue_hud.png`, `Captures/rogue_levelup.png`), bind sẵn 3 trạng thái thẻ khác nhau: skill mới toanh, skill đang 4/5, skill sắp kịch 3/3.

**Chưa play test thật.** Nhịp 20 giây một lần chọn có làm gãy mạch bắn hay không là thứ chỉ chơi mới biết — chỉnh bằng `_baseXpToLevel` / `_xpGrowthPerLevel` trong `RoguelikeSettings.asset`, không đụng code.
