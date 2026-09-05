# Roguelike choice — hệ thống chọn kỹ năng trong trận

Tài liệu hệ thống roguelike của **Zombie War**. Viết ngày 2026-09-05, sau khi hệ thống đã nối xong và compile sạch.

> Hệ thống này **nằm ngoài spec** `Zombie_War_SPEC_GDD_V1.0.docx` (spec cố ý không có roguelike). User yêu cầu thêm sau khi P0 xong, và tự để phần thiết kế skill cho phía implement quyết định.

---

## 1. Tóm tắt

Giết quái → nhận **XP** → đầy thanh thì lên **battle level** → game dừng lại và hiện popup **chọn 1 trong 3 skill**. Battle level chỉ sống trong đúng một lượt chơi và mất khi hết màn — khác hẳn level tài khoản của hệ meta progression (mua nâng cấp súng bằng coin).

Có hai loại skill, cùng một popup, cùng hàng sao:

- **11 passive** — thuần **stat modifier**, **mỗi skill đúng một hiệu ứng** (user 2026-09-05: thẻ nhiều hiệu ứng khó đọc), tức mỗi `StatId` có đúng một passive; thêm cái thứ 12 chỉ cần thêm một `.asset`.
- **6 active** — **tự kích hoạt**, không có nút bấm: game vốn đã auto-aim + auto-fire, người chơi chỉ di chuyển, nên một nút bấm sẽ đi ngược thiết kế đó. Mỗi active là một hành vi riêng → một subclass `ActiveSkillSO` + một controller author sẵn trên `Player` + một `.asset`. Hai active đầu (AUTO GRENADE, ORBIT BLADES) đi cùng hệ thống; bốn active sau (MOLOTOV, CHAIN LIGHTNING, SHOCKWAVE, SENTRY DRONE) thêm ngày 2026-09-05 theo đề xuất được user duyệt, lấp bốn khoảng trống: kiểm soát khu vực theo thời gian, sát thương lan trong đám đông, phòng thủ phản ứng, đồng đội tự bắn.

Cơ chế **ném bom thủ công đã bỏ hẳn** (quyết định user 2026-09-05): không còn nút bom, charge, cooldown riêng trên HUD. Quả bom vẫn tồn tại — dưới dạng active skill AUTO GRENADE — để giữ yêu cầu spec §6 (sát thương + lực vật lý).

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
├── StatId.cs                     # enum 11 stat mà passive được phép động vào
├── StatModifierKind.cs           # Additive | Multiplicative
├── StatModifier.cs               # struct (StatId, Kind, ValuePerStack)
├── SkillDefinitionSO.cs          # abstract: identity + maxStacks + draftWeight + Apply()
├── PassiveSkillSO.cs             # : SkillDefinitionSO — Apply() đổ StatModifier[] vào stat sheet
├── ActiveSkillSO.cs              # : SkillDefinitionSO, abstract — Apply() đăng ký vào AbilityRunner
├── AutoBombSkillSO.cs            # active: cooldown → BombThrower.ThrowVolley()
├── OrbitBladesSkillSO.cs         # active liên tục: cấu hình OrbitBladesController
├── MolotovSkillSO.cs             # active: cooldown → MolotovThrower.Throw(MolotovBurn)
├── MolotovDefinitionSO.cs        # phần không đổi theo stack của molotov: pool, tầm ném, prefab lửa, clip
├── ChainLightningSkillSO.cs      # active: CanTrigger = có mục tiêu → ChainLightningCaster.Strike()
├── ShockwaveSkillSO.cs           # active: CanTrigger = đủ zombie trong tầm → ShockwaveEmitter.Emit()
├── SentryDroneSkillSO.cs         # active liên tục: cấu hình SentryDroneController với Gun_Drone
└── RoguelikeSettingsSO.cs        # pool + starting ability + first-level pool + đường cong XP

Scripts/Player/
├── PlayerStatSheet.cs            # MonoBehaviour: giá trị stat sống, trên object Player
├── AbilityRunner.cs              # MonoBehaviour: slot + cooldown của active, tick tập trung
├── AbilityContext.cs             # readonly struct: những gì một active được phép chạm
├── SkillApplyContext.cs          # readonly struct: (PlayerStatSheet, AbilityRunner) cho Apply()
├── OrbitBladesController.cs      # MonoBehaviour: pivot + 6 lưỡi author sẵn, quét overlap
├── ChainLightningCaster.cs       # MonoBehaviour: 8 LineRenderer author sẵn, chuỗi nhảy bằng overlap
├── ShockwaveEmitter.cs           # MonoBehaviour: ring author sẵn + blast toả tròn từ chân player
└── SentryDroneController.cs      # MonoBehaviour: bay vòng quanh player trong world space, tự quét/ngắm/bắn qua ProjectileManager

Scripts/Weapons/
├── MolotovThrower.cs             # MonoBehaviour trên Player: pool chai + pool FireZone riêng, tick lửa
├── Molotov.cs                    # chai lửa pooled (Rigidbody + trail), vỡ khi hết thời gian bay
├── MolotovBurn.cs                # readonly struct: bán kính / thời lượng / damage tick chốt lúc ném
└── ThrowSolver.cs                # static: điểm rơi + vận tốc ném dùng chung cho BombThrower và MolotovThrower

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
├── Skill_{HighCaliber,RapidFire,PiercingRounds,Adrenaline,BloodPact,Demolitionist}.asset
└── Skill_{AutoBomb,OrbitBlades,Molotov,ChainLightning,Shockwave,SentryDrone}.asset

Data/Weapons/
├── MolotovDefinition.asset       # pool 4 chai / 6 lửa, tầm 6 m, bay 0.6 s, prefab FireZone, clip ném/bén
└── Gun_Drone.asset               # GunDefinitionSO riêng của drone: 10 dmg / 0.35 s, không nằm trong shop

Prefabs/Weapons/Molotov.prefab    # dựng từ nội dung Bomb.prefab: bỏ vòng telegraph, Body capsule, cho lăn
Art/Materials/LightningBolt.mat   # copy Tracer.mat, _BaseColor cyan, cho LineRenderer sét
```

### Trách nhiệm

| Class | Việc | Ghi chú |
|---|---|---|
| `BattleXpTracker` | Cộng XP, trả về số cấp vừa lên, tính `Normalized` cho thanh XP | C# thuần → test được không cần scene |
| `SkillDraft` | Bốc N thẻ khác nhau theo `DraftWeight`, loại skill đã kịch trần | C# thuần, zero-alloc (dùng lại `List<int>` candidate) |
| `PlayerStatSheet` | Gộp mọi stack đang sở hữu thành 12 giá trị stat sống | Multiplicative nghỉ ở 1, additive nghỉ ở 0 |
| `AbilityRunner` | Giữ slot `(ActiveSkillSO, stacks, cooldownRemaining)`, tick cooldown, gọi `Trigger` / `OnEquipped` / `OnUnequipped` | Cooldown **không** nằm trong SO — SO là asset dùng chung |
| `OrbitBladesController` | Xoay pivot, mỗi lưỡi `OverlapSphereNonAlloc` → `TakeDamage` theo tick, dọn map định kỳ | Pool 6 lưỡi author sẵn trong prefab; skill chỉ bật/tắt |
| `MolotovThrower` | Ném chai theo cung (`ThrowSolver`), chai vỡ → lấy `FireZone` từ pool riêng, mỗi tick lửa quét overlap `_burnMask` = Enemy → `TakeDamage(Fire)` | Không đốt player; `MolotovBurn` đi theo từng chai nên lên cấp giữa lúc bay không đổi vùng lửa; tick cả ở `Lost` như bom |
| `ChainLightningCaster` | Từ `PlayerAim.CurrentTarget`, nhảy sang con gần nhất chưa trúng trong `jumpRadius` (không cần LOS), vẽ bolt gấp khúc ngẫu nhiên rồi co bề rộng về 0 trong `_boltDuration` | 8 `LineRenderer` author sẵn = trần `1 + jumps`; log error nếu skill đòi nhiều hơn |
| `ShockwaveEmitter` | `CountZombiesWithin` cho `CanTrigger`; `Emit` overlap quanh chân player, lực đồng nhất ≥ ngưỡng physics nên hất bay mọi con (Giant chỉ ăn damage), ring scale ease-out bằng timer | Dùng chung impulse Bump 0.2 s với bom |
| `SentryDroneController` | `LateUpdate`: bay vòng quanh vị trí player theo đồng hồ riêng (`_orbitRadius` 1.4 m, 60°/s, cao 1.7 m + bob) và `SmoothDamp` 0.2 s bám theo; quét 10 m mỗi 0.2 s (giữ mục tiêu còn hợp lệ, phạt con player đang bắn), xoay `Body` về mục tiêu (không có thì theo hướng bay) rồi `ProjectileManager.Spawn` với `Gun_Drone` | Nằm ở `Abilities/Drone`, **không** phải con của `Player`: chỉ đọc `_anchor.position`, không bao giờ ăn rotation của nhân vật. Damage/nhịp bắn do skill `Configure`; không ăn passive lẫn shop |
| `RoguelikeDirector` | Nghe kill, cộng XP, quản nợ level-up, bốc thẻ, cộng stack, rebuild **cả** stat sheet lẫn ability | Không đụng UI, không đụng timeScale |
| `GameplayUiBinder` | Đổi `SkillDefinitionSO` → `SkillCardData`, ra lệnh pause/resume | Lớp duy nhất giữ ref cả hai phía |

### API công khai

```csharp
// RoguelikeDirector
event Action<float, int>                    OnXpChanged;      // normalized, battle level
event Action<SkillDefinitionSO[], int, int> OnChoiceOffered;  // offers, count, battle level
event Action                                OnChoiceClosed;
void ChooseOffer(int index);
int  StacksOf(SkillDefinitionSO skill);

// SkillDefinitionSO (base của cả hai loại)
abstract void Apply(in SkillApplyContext context, int stacks);   // idempotent, gọi mỗi rebuild

// ActiveSkillSO
abstract float CooldownFor(int stacks);            // <= 0 nghĩa là liên tục, runner không tick
virtual  bool  CanTrigger(in AbilityContext ctx, int stacks);   // hỏi mỗi frame khi cooldown đã về 0; false = giữ ở 0, không đốt lượt
virtual  void  OnEquipped(in AbilityContext ctx, int stacks);   // mỗi rebuild — phải idempotent
virtual  void  OnUnequipped(in AbilityContext ctx);             // khi lượt mới bỏ nó
virtual  void  Trigger(in AbilityContext ctx, int stacks);      // mỗi lần hết cooldown và CanTrigger đồng ý
protected static float ScaledCooldown(base, perStack, min, stacks); // base × perStack^(stacks-1), sàn min — dùng chung cho mọi active có cooldown

// AbilityRunner (cùng nhịp với PlayerStatSheet)
void BeginRebuild(); void Equip(ActiveSkillSO skill, int stacks); void EndRebuild();

// PlayerStatSheet
event Action OnChanged;                       // bắn MỘT lần sau khi cả sheet đã settle
float Multiplier(StatId stat);                // nghỉ ở 1
float Additive(StatId stat);                  // nghỉ ở 0
void BeginRebuild(); void Apply(StatModifier[] modifiers, int stacks); void EndRebuild();
```

`OnChanged` cố ý chỉ bắn ở `EndRebuild` — nếu bắn sau mỗi `Apply` thì `PlayerHealth` sẽ đọc phải một build dở dang và cộng nhầm máu.

**Rebuild là một vòng lặp đa hình**, không có `if (là passive)`:

```csharp
_stats.BeginRebuild(); _abilities.BeginRebuild();
foreach (owned in _stacks) owned.Key.Apply(context, owned.Value);   // passive → sheet, active → runner
_stats.EndRebuild();   _abilities.EndRebuild();
```

`AbilityRunner.Equip` giữ nguyên cooldown đang chạy của skill đã có — nếu không, bốc **bất kỳ** thẻ nào cũng tặng một lần kích hoạt miễn phí.

---

## 4. Thiết kế 11 passive skill

Tất cả đều là stat modifier và **mỗi skill đúng một hiệu ứng** (user 2026-09-05: thẻ gộp 2–3 hiệu ứng làm người chơi khó đọc — bản đầu có 6 passive đa hiệu ứng, đã tách ra cùng ngày). Hệ quả: 11 `StatId` ↔ 11 passive, mỗi stat có đúng một thẻ nâng nó, nên đọc tên thẻ là biết nó vặn nút nào. Cột "Mỗi cấp" là giá trị cộng/nhân cho **một** stack; nhiều stack thì additive cộng dồn, multiplicative **nhân dồn** (2 cấp `1.2` → `1.44`, không phải `1.4`).

| Skill | Mỗi cấp | Trần | Icon (`Item_Icon_*` 256) | Màu |
|---|---|---|---|---|
| **HIGH CALIBER** | `WeaponDamage ×1.20` | 5 | `Bullet_Gold` | cam `#FF9B3D` |
| **RAPID FIRE** | `FireRate ×1.15` | 5 | `Magazine` | vàng `#FFD54A` |
| **PIERCING ROUNDS** | `ProjectilePierce +1` | 3 | `Bullet_Silver` | lam `#4ECDE0` |
| **SLUG ROUNDS** | `Knockback ×1.15` | 4 | `Bullet_Shotgun` | cam đất `#F28C59` |
| **ADRENALINE** | `MoveSpeed ×1.10` | 4 | `Boots` | lục `#5CE08B` |
| **VETERAN** | `XpGain ×1.08` | 4 | `Exp` | xanh `#73A6FF` |
| **VITALITY** | `MaxHealth +15` | 4 | `Heart` | hồng đỏ `#FF808C` |
| **BLOOD PACT** | `HealPerKill +2` | 4 | `First-Aid` | đỏ `#FF5B4A` |
| **EXTRA PAYLOAD** | `ExtraBombsPerVolley +1` | 3 | `Bullet_Pack` | tím hồng `#D98CF2` |
| **BIG BLAST** | `BombRadius ×1.12` | 3 | `Gasoline` | tím lam `#8C99FF` |
| **DEMOLITIONIST** | `BombDamage ×1.20` | 3 | `Hammer` | tím `#B47CFF` |

Lịch sử: RAPID FIRE từng có thêm `ReloadSpeed`; băng đạn/nạp đạn đã bỏ khỏi game (2026-09-05) nên stat đó bị xoá và enum đánh số lại. Cùng ngày, HIGH CALIBER (kèm knockback), ADRENALINE (kèm XP), BLOOD PACT (kèm máu tối đa) và DEMOLITIONIST (gộp cả ba stat bom) được tách thành SLUG ROUNDS / VETERAN / VITALITY / EXTRA PAYLOAD / BIG BLAST; bốn thẻ cũ giữ tên và giữ đúng một hiệu ứng, số liệu mỗi cấp không đổi.

**Ý đồ thiết kế**: mỗi thẻ là một nút vặn đơn, nên ba thẻ trong một lần bốc so sánh được ngay với nhau, và mỗi nhóm dưới đây trả lời một câu hỏi khác nhau về lối chơi.

- Nhóm súng: sát thương (HIGH CALIBER), nhịp bắn (RAPID FIRE), xuyên đám đông (PIERCING ROUNDS), đẩy lùi (SLUG ROUNDS)
- Nhóm cơ động / cuộn tuyết: tốc chạy (ADRENALINE), XP (VETERAN)
- Nhóm sinh tồn: máu tối đa (VITALITY), hồi máu theo kill (BLOOD PACT) — nguồn hồi máu **duy nhất** trong trận, nên nó là lựa chọn "cứu mạng" thật sự
- Nhóm bom: số quả mỗi volley (EXTRA PAYLOAD), bán kính (BIG BLAST), sát thương nổ (DEMOLITIONIST) — cả ba chỉ có tác dụng khi đã bốc AUTO GRENADE. `SkillDraft` **chưa** lọc điều kiện này nên chúng vẫn có thể được chào khi chưa có lựu đạn (biết trước, chưa xử lý; nếu làm thì thêm một danh sách "cần sở hữu" vào `SkillDefinitionSO` và lọc trong `SkillDraft.Roll`).

**Tổng trần passive = 42 stack, cộng 6 active × 5 = 72**, trong khi cả lượt chỉ đi được tối đa 11 lần chọn (battle level 12). Người chơi **không bao giờ full được** — đó là lý do lựa chọn có sức nặng.

### 6 active skill (tự kích hoạt)

| Skill | Cơ chế | Mỗi cấp | Trần | Icon |
|---|---|---|---|---|
| **AUTO GRENADE** | Cứ hết cooldown là ném một **volley** về phía mục tiêu auto-aim (không có thì theo hướng chạy). Volley = `1 + ExtraBombsPerVolley` quả, quả thêm rơi lệch ngẫu nhiên trong `VolleyScatter` 1.6 m | cooldown 7 s × 0.84ⁿ, sàn 2 s | 5 | `Item_Icon_Grenade` |
| **ORBIT BLADES** | Lưỡi cưa xoay quanh người 240°/s ở bán kính 2.2 m; mỗi lưỡi quét overlap, một zombie ăn tick tối đa 2.5 lần/giây (`_hitInterval` 0.4) | 2 lưỡi + 1/cấp (trần 6 = số lưỡi author trong prefab), damage 14 × 1.2ⁿ | 5 | `Icon_Change` |
| **MOLOTOV** | Ném chai theo cung y hệt grenade (tầm 6 m, bay 0.6 s), vỡ thành vùng lửa bán kính 2.2 m đốt zombie đứng trong đó 8 dmg mỗi 0.4 s (~20 dps, Walker chết sau ~2.4 s). Mỗi tick lửa là một `HitStun` 0.14 s nên lửa còn **làm chậm** kẻ đi qua. Không đốt player | cooldown 9 s × 0.85ⁿ sàn 4 s, cháy 4 s + 1 s/cấp, damage × 1.15ⁿ | 5 | `Item_Icon_Burning_Bottle` |
| **CHAIN LIGHTNING** | Chỉ bắn khi auto-aim có mục tiêu (`CanTrigger`). Sét đánh mục tiêu rồi nhảy sang con gần nhất chưa trúng trong 4 m, mỗi lần nhảy damage × 0.8, không cần đường bắn thẳng nên với được phía sau lưng. 40 dmg đầu, 3 lần nhảy | cooldown 5 s × 0.88ⁿ sàn 2.5 s, +1 lần nhảy/cấp (trần 7 = 8 bolt author sẵn) | 5 | `Item_Icon_Car_Battery` |
| **SHOCKWAVE** | Cooldown hết thì **chờ** tới khi có ≥ 2 zombie trong bán kính (`CanTrigger`) rồi mới nổ: 30 dmg + lực 7 (trên ngưỡng physics 4) hất bay mọi con trong 3.5 m, Giant chỉ ăn damage. Ring toả ra + impulse camera 0.4 + khói `Vfx_ZombieDeathSmoke` | cooldown 8 s × 0.85ⁿ sàn 3.5 s, bán kính +0.3 m/cấp, damage × 1.2ⁿ | 5 | `Item_Icon_Megaphone` |
| **SENTRY DRONE** | Liên tục. Drone **bay vòng quanh player** trong world space (bán kính 1.4 m, 60°/s, cao 1.7 m, bob 0.12 m / 1.2 Hz, SmoothDamp 0.2 s bám theo) nên luôn di chuyển và không xoay theo nhân vật; quét 10 m mỗi 0.2 s, ưu tiên con **khác** mục tiêu của player (phạt 4 m), bắn qua pipeline đạn chung với `Gun_Drone` 8 dmg / 0.35 s (~23 dps, ≈ 40% rifle nền 60 dps sau đợt cân bằng 2026-09-06; 3 viên hạ Walker 24 HP, 2 viên hạ Runner 14 HP) | damage × 1.2ⁿ, nhịp bắn × 0.9ⁿ | 5 | `Item_Icon_Machine` |

Knockback của lưỡi (1.2) và sét (1.0) cố ý dưới `PhysicsKnockbackThreshold` (4) của zombie: chúng **hích**, không hất bay — hất bay liên tục sẽ phá nhịp chase. SHOCKWAVE là active duy nhất ngoài grenade cố ý vượt ngưỡng đó.

**Vì sao có `CanTrigger`**: runner cũ reset cooldown ngay khi về 0 rồi gọi `Trigger`. Với grenade thì ổn (không có mục tiêu vẫn ném về hướng chạy), nhưng sét vào khoảng không hay sóng xung kích khi chẳng có ai đứng cạnh là đốt trọn một cooldown. Giờ runner hỏi `CanTrigger` mỗi frame sau khi cooldown về 0 và **giữ cooldown ở 0** cho tới khi skill đồng ý. Hai skill cũ không sửa gì vì mặc định trả `true`.

**Cách nhận**: `_startingAbility` để trống — người chơi vào trận chỉ có súng. **Lần level-up đầu tiên chỉ bốc từ `_firstLevelPool`** (cả 6 active, offer 3) để lượt nào cũng có ability sớm và lượt nào cũng khác lượt nào; từ lần thứ hai cả hai loại trộn chung pool, active đã sở hữu được offer như bản nâng cấp.

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
| `MoveSpeed` | nhân | `PlayerMotor.FixedUpdate` |
| `MaxHealth` | cộng | `PlayerHealth.Max` + `HandleStatsChanged` |
| `HealPerKill` | cộng | `RoguelikeDirector.HandleZombieKilled` → `PlayerHealth.Heal` |
| `ExtraBombsPerVolley` | cộng | `BombThrower.ThrowVolley` (số quả thêm mỗi volley) |
| `BombRadius` | nhân | `BombThrower.ThrowVolley` → `Bomb.BlastRadius` |
| `BombDamage` | nhân | `BombThrower.Explode` |
| `XpGain` | nhân | `RoguelikeDirector.HandleZombieKilled` |

### ⚠️ Thứ tự nhân với meta progression

Hệ số roguelike nhân **lên trên `gun.Stats`** (đã bao gồm cấp nâng cấp mua bằng coin), **không** nhân lên `gun.Definition`:

```csharp
gun.Stats.Damage * _stats.Multiplier(StatId.WeaponDamage)   // đúng
gun.Definition.Damage * _stats.Multiplier(...)              // SAI: vô hiệu hoá toàn bộ shop
```

Hai chỉ số `Damage` / `FireInterval` đều đi qua `gun.Stats` (băng đạn và nạp đạn đã bỏ hẳn khỏi game ngày 2026-09-05). Riêng `Knockback`, `Range`, `SpreadAngle`, `PelletCount`, VFX vẫn lấy thẳng từ `definition` vì shop không đụng tới chúng.

### Một stat áp dụng theo delta

`MaxHealth` không thể đọc lười mỗi frame — nó phải **trao ngay máu** lúc người chơi bấm chọn: `PlayerHealth.HandleStatsChanged` cộng đúng phần chênh vào máu hiện tại (nhớ `_maxHpBonus` của lần trước để rebuild nhiều lần vẫn đúng). Không làm vậy thì "+15 máu tối đa" chỉ nới rộng phần rỗng của thanh máu — vô dụng đúng lúc cần nhất.

`BombThrower` không còn state nào để áp theo delta: không charge, không cooldown riêng — nó chỉ còn `ThrowVolley()` và phần nổ; nhịp ném là việc của `AbilityRunner`.

---

## 6. UI

### Thanh XP (HUD)

Nằm trong `TopBar` ngay dưới thanh máu: badge tròn hiện battle level + thanh fill xanh dương. Chạy bằng **anchor** (`DOAnchorMax`) chứ không phải `Image.Type = Filled` — cùng lý do với thanh máu: sprite pill 9-slice bị cắt cụt đầu bo nếu dùng Filled, và anchor đọc đúng ngay frame đầu khi `CanvasScaler` chưa kịp size canvas. Fill được author sẵn ở 0 để không loé một frame đầy thanh lúc vào trận.

### Popup chọn skill

`Prefabs/UI/Popups/SkillChoicePopup.prefab` — **3 cột dọc xếp ngang**, mỗi thẻ: tên → badge NEW → icon → mô tả → hàng sao. Từ 2026-09-06 **mỗi thẻ có frame riêng** (`Frame_BannerFrame04_Navy01`, 300×720, cách nhau 30 px) và popup **không còn khung nền chung**: `Panel` vẫn giữ `Image` (tắt) để layout/fit không đổi, chỉ banner LEVEL UP + chip battle level nổi trên backdrop. Image của thẻ là `targetGraphic` của Button nên frame tối đi khi bấm.

- **Hàng sao thay cho chữ "LV 3/5"**: số sao vàng = cấp **sau khi chọn**, tổng số sao = `MaxStacks`. Nhìn phát biết skill còn sâu bao nhiêu, không phải đọc và trừ nhẩm giữa lúc đang bị vây.
- Prefab author sẵn **5 slot sao** trong mảng `SkillCardView._stars` (bằng trần sâu nhất trong pool). Skill nông hơn ẩn bớt sao và `SkillCardView.DrawStars` dịch cả hàng lại cho vẫn cân giữa.
- Badge **NEW** hiện khi `SkillCardData.IsNew` (chưa sở hữu stack nào).
- **Phân biệt passive / active** (user phản hồi 2026-09-05: hai loại nhìn y hệt nhau): mỗi thẻ có **chip loại** dưới tên — `PASSIVE` trên tag `Label_Tag01_SkyBlue` (nhạt, chữ ink) và `ACTIVE` trên `Label_Tag01_Blue` (xanh đậm, chữ trắng) — cộng **đĩa icon tô cyan** cho active, trắng cho passive. Dữ liệu vào view là `SkillCardData.IsActive`, binder lấy từ `SkillDefinitionSO.Kind` (thuộc tính đa hình, không `is`-check). Tag của pack là art có màu sẵn nên view **đổi sprite** theo loại chứ không tint.
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
- `AbilityRunner` trên `Player`: `_flow`, `_health`, `_bombs`, `_blades`; `RoguelikeDirector._abilities` → runner.
- Nhánh `Abilities/OrbitBlades` (local 0; từ 2026-09-06 không còn là con của `Player` vì vòng cưa ăn rotation của nhân vật — user báo sai): `OrbitBladesController` với `_flow`, `_zombies`, `_anchor` = `Player` (chỉ đọc position, controller đặt `transform.position = anchor + up × _height` 1 m mỗi Update), `_enemyMask` = layer Enemy, `_pivot` = con `Pivot`, `_blades` = 6 con `Blade1..6` (Cylinder dẹt 0.6 × 0.04 × 0.6, material `WFXM_M_GlowPalet Add`, không collider — va chạm bằng overlap, author **inactive**; `Awake` của controller cũng gọi `Deactivate()`). Muốn trần ORBIT BLADES cao hơn 5 thì thêm Blade7… vào mảng `_blades`.
- `BombThrower` không còn ref nào từ UI; `BombDefinition` mất `_chargesPerLevel`/`_cooldown`, thêm `_poolSize` (8) và `_volleyScatter` (1.6).
- `MolotovThrower` trên `Player` (cạnh `BombThrower`): `_definition` = `Data/Weapons/MolotovDefinition.asset`, `_prefab` = `Prefabs/Weapons/Molotov.prefab` (layer Bomb, Rigidbody không khoá xoay để chai lăn, trail copy từ Bomb), `_bottlePoolParent` = `Pools/MolotovPool`, `_firePoolParent` = `Pools/PlayerFirePool` (pool `FireZone` riêng, tách khỏi `FireZonePool` của hazard), `_throwOrigin`, `_aim`, `_motor`, `_health`, `_flow`, `_zombies`, `_audio`, `_burnMask` = Enemy.
- Node **`GameplayRoot/Abilities`** (ngang hàng `Player`, active ngay từ menu; các controller trong đó tự gate bằng cờ active/`_litBolts` nên `Update` ở menu rẻ). Sinh ra vì user yêu cầu 2026-09-05: sét và drone **không được phụ thuộc rotation của player** — là con của `Player` thì mọi lần nhân vật quay theo mục tiêu là chúng bị quăng theo. Thứ phải xoay cùng nhân vật (lưỡi cưa, ring sóng xung kích) vẫn ở dưới `Player`.
- Nhánh `Abilities/Lightning` (local 0): `ChainLightningCaster` với `_aim`, `_zombies`, `_origin` = `Player/AimOrigin` (nằm trên trục nhân vật nên rotation không đổi vị trí), `_enemyMask` = Enemy, `_vfx`, `_audio`, `_bolts` = 8 con `Bolt1..8` (LineRenderer material `LightningBolt.mat`, width 0.08, `useWorldSpace` = true, màu cyan author trên renderer, inactive), `_hitVfx` = `Vfx_ImpactFlesh`, `_zapClip` = `JackHammer_3p_01`.
- Nhánh `Player/Shockwave` (local 0): `ShockwaveEmitter` với `_zombies`, `_vfx`, `_audio`, `_impulseSource` = `CinemachineImpulseSource` Bump 0.2 s (dùng chung với bom), `_enemyMask`, `_ring` = con `Ring` (Cylinder 1 × 0.02 × 1 ở y 0.05, material `WFXM_M_GlowCircle Add`, không collider, inactive), `_burstVfx` = `Vfx_ZombieDeathSmoke`, `_clip` = `AssaultCanon_1p_tail`, `_cameraImpulse` 0.4.
- Nhánh `Abilities/Drone` (local 0): `SentryDroneController` với `_flow`, `_health`, `_playerAim`, `_zombies`, `_projectiles`, `_vfx`, `_audio`, `_enemyMask` = Enemy, `_obstacleMask` = Obstacle, `_anchor` = `Player` (chỉ đọc position), `_body` = con `Body` (local 0, inactive; controller đặt `Body.position` trong world mỗi `LateUpdate`; chứa `Hull` Sphere 0.28 + `Glow` Cylinder 0.5 × 0.03 + `Barrel` Cylinder xoay 90° — đều primitive không collider, material `BombBody` / `WFXM_M_GlowPalet Add`), `_muzzle` = `Body/Muzzle` (0, −0.02, 0.33). Nhóm `Flight`: `_orbitRadius` 1.4, `_orbitDegreesPerSecond` 60, `_hoverHeight` 1.7, `_hoverAmplitude` 0.12, `_hoverFrequency` 1.2, `_followSmoothTime` 0.2, `_turnSpeedDegrees` 540, `_headingSpeedThreshold` 0.2. Có model drone thật thì thay ba primitive dưới `Body`, giữ `Muzzle`.
- `AbilityRunner` thêm `_molotovs`, `_lightning`, `_shockwave`, `_drone`; `Awake` fail sớm nếu thiếu bất kỳ cái nào.
- `RoguelikeSettings._skillPool` liệt kê 12 `Skill_*.asset`, `_firstLevelPool` liệt kê 6 active; icon là `Item_Icon_*` 256 px của GUI PRO Kit - Sci-Fi Survival đã copy vào `Art/UI/Sprites/Icons/` (`Burning_Bottle`, `Car_Battery`, `Megaphone`, `Machine` thêm 2026-09-05).

`SkillChoicePopup.prefab` sửa thẳng trong Prefab Mode; `UIRoot.prefab` chứa nó dưới `Canvas_Popup`.

---

## 8. Thêm skill mới

Trường hợp thường gặp — **không phải sửa dòng code nào**:

1. Tạo asset bằng `Create ▸ Zombie War ▸ Passive Skill` trong `Data/Roguelike/` (id, tên, mô tả, icon, màu, trần `MaxStacks`, **đúng một** `StatModifier` — quy ước mỗi passive một hiệu ứng, mô tả một dòng kiểu "Damage +20%").
2. Kéo asset đó vào mảng `_skillPool` của `RoguelikeSettings.asset`.

**Chỉ khi** hiệu ứng mới không biểu diễn được bằng stat có sẵn (nổ dây chuyền khi kill, hồi sinh, aura, đạn nảy…) thì mới phải:

1. Thêm một entry vào `StatId`
2. Đọc nó ở **đúng một chỗ** trong hệ thống liên quan
3. Bổ sung vào bảng §5 của tài liệu này

Ràng buộc còn lại: skill có `MaxStacks > 5` thì phải thêm slot sao vào mảng `_stars` của cả ba `SkillCardView` trong `SkillChoicePopup.prefab`. `SkillCardView` sẽ log error nếu quên chứ không im lặng hỏng.

### Thêm active mới

Active thứ 7 tốn đúng năm bước, và không bước nào đụng vào director, draft hay UI:

1. **Controller** MonoBehaviour trong `Scripts/Player/` (hoặc `Scripts/Weapons/` nếu nó là vũ khí): giữ toàn bộ state + ref scene, phơi ra vài method (`Configure`/`Deactivate` cho loại liên tục, một method hành động cho loại cooldown). Mọi mesh/renderer/pool nó cần đều author sẵn trong prefab, inactive — không tạo object lúc chạy. Quyết định sớm nó nằm ở đâu: dưới `Player` nếu hiệu ứng phải xoay cùng nhân vật, dưới `Abilities/` (đọc vị trí player qua một ref `Transform`) nếu không được ăn rotation của player.
2. **SO subclass** `ActiveSkillSO` trong `Scripts/Data/`: chỉ chứa số theo stack và gọi vào controller qua `AbilityContext`. Có cooldown thì dùng `ScaledCooldown`; có điều kiện kích hoạt thì override `CanTrigger`.
3. Thêm một field vào `AbilityContext` + một `[SerializeField]` vào `AbilityRunner` (đây là chỗ duy nhất "phình").
4. Author nhánh dưới `Player` hoặc `Abilities` trong `GameplayRoot.prefab` và nối ref (qua Editor hoặc MCP, không sửa YAML tay).
5. Tạo asset qua `Create ▸ Zombie War ▸ Active Skill ▸ …`, kéo vào `_skillPool` và `_firstLevelPool`.

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

Active skill (AUTO GRENADE, ORBIT BLADES) mới chỉ có **compile xanh** (dotnet build toàn bộ assembly với reference/define của Unity) và kiểm cấu trúc YAML của prefab; chưa có ảnh chụp lẫn play test — session MCP rớt đúng lúc nối. Việc đầu tiên khi có editor: import `GameplayRoot.prefab`, xem `Player/OrbitBlades` có đủ 6 lưỡi và `AbilityRunner._blades` có trỏ đúng không.

Bốn active mới (MOLOTOV, CHAIN LIGHTNING, SHOCKWAVE, SENTRY DRONE, 2026-09-05): compile xanh qua Roslyn csc với reference của `ZombieWar.csproj`, Unity import xong không error/warning, và wiring đã dump lại từ `GameplayRoot.prefab` sau khi save (mọi `[SerializeField]` của 4 controller + `AbilityRunner` đều non-null, 8 bolt, ring/body inactive, mask Enemy = 512 / Obstacle = 1024). **Chưa play test** — cần xem thật: chai molotov có rơi đúng chỗ vòng lửa không, bolt sét có đọc được ở tốc độ 0.18 s không, ring shockwave có đủ to để hiểu tại sao đám đông bay không, và drone có bắn trúng thân zombie khi treo ở độ cao 1.7 m không (đạn bay theo hướng 3D tới `AimPoint`, `_hitMask` của `ProjectileManager` không có Ground nên viên trượt sẽ tự hết tầm sau 12 m). Sau lần play test đầu của user (2026-09-05) drone đã được chuyển ra `Abilities/` và đổi sang bay vòng world-space vì bản đầu neo local vào `Player` nên quay theo nhân vật và đứng chết một chỗ.

Layout kiểm bằng ảnh chụp canvas qua `ScreenSpaceCamera` (`Captures/rogue_hud.png`, `Captures/rogue_levelup.png`), bind sẵn 3 trạng thái thẻ khác nhau: skill mới toanh, skill đang 4/5, skill sắp kịch 3/3.

**Chưa play test thật.** Nhịp 20 giây một lần chọn có làm gãy mạch bắn hay không là thứ chỉ chơi mới biết — chỉnh bằng `_baseXpToLevel` / `_xpGrowthPerLevel` trong `RoguelikeSettings.asset`, không đụng code.
