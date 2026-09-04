# GAME SPEC — Zombie War

Spec chính thức của project. Logic gameplay phải tôn trọng tuyệt đối các yêu cầu ở mục 1 (xem `CODE-RULE.md` §8). Mục 2 trở đi là phân rã kỹ thuật do team tự suy ra từ đề bài — khi mâu thuẫn, mục 1 thắng.

## 1. Đề bài gốc (nguyên văn từ khách hàng)

**Đề bài: Xây dựng Game Zombie War.**

Yêu cầu:

1. **Camera:** người lính chiến đấu với bầy zombie tràn về từ bốn phía, dùng Cinemachine follow nhân vật, góc nhìn từ phía trên xuống.
2. **Control:** dùng virtual joystick điều khiển soldier.
3. **Soldier:** dùng animation layer tách riêng phần chạy và bắn, có hiệu ứng mất máu…
4. **Zombie:** dùng AI tìm soldier, hiệu ứng trúng đạn và biến mất bằng shader…
5. **Gun:** ít nhất 2 loại súng, có button switch, có particle đạn nổ, hiệu ứng súng giật…
6. **Bom:** Có hiệu ứng bom nổ sát thương vật lý đến zombie.
7. **Level:** mỗi level thời gian chơi tầm 3 phút. Level 1 mặt đất bằng phẳng, có vật cản. Level 2 có dốc và zombie khổng lồ. Nhịp điệu game dồn dập tăng cao dần (Level 2 có thể có hoặc không => điểm cộng).
8. Tích hợp âm thanh và particle.

**Yêu cầu sản phẩm:** Thực hiện đầy đủ các yêu cầu.

Bài nộp bao gồm:

- Link Github: Push source code lên github
- File apk: Build apk
- Quay video

LINK ASSET: "Asset for Test" (link đính kèm trong đề, không nằm trong repo).

**Note:**

- Bài test sẽ chiếm tổng 80% kết quả, 20% sẽ dựa theo kết quả phỏng vấn trực tiếp.
- Team đánh giá thông qua 4 phần chính:
  - a. Gameplay
  - b. Physics
  - c. Animation (Blend Trees)
  - d. Shader, Visual, UI. Support multi-resolution
- Có thể sử dụng Asset cho sẵn hoặc sử dụng Asset cá nhân để gắn vào game.
- Con game hoàn thiện theo khả năng nhất có thể (chú ý thêm các yếu tố về visual, độ mượt,…).

**Thời gian:** mục tiêu 3–4 ngày. **Deadline: 7 ngày.**

## 2. Tiêu chí nghiệm thu từng yêu cầu

Checklist dùng để tự kiểm tra trước khi nộp. Mỗi dòng phải nhìn thấy được trong video.

| # | Yêu cầu | Nghiệm thu |
|---|---------|-----------|
| 1 | Camera | Có `CinemachineVirtualCamera` follow soldier, góc nhìn top-down. Zombie spawn từ cả 4 hướng quanh soldier. |
| 2 | Control | Joystick ảo trên màn hình cảm ứng điều khiển di chuyển. Chạy được trên Android thật. |
| 3 | Soldier | Animator có **Blend Tree** cho di chuyển và ít nhất 2 **layer**: thân dưới (idle/run) và thân trên (aim/shoot) dùng Avatar Mask. Trúng đòn có phản hồi hình ảnh (flash/vignette/HP bar) và có HP giảm. |
| 4 | Zombie | Tự tìm đường tới soldier (NavMesh). Trúng đạn có phản hồi (hit react/particle). Chết thì **dissolve bằng shader** rồi trả về pool. |
| 5 | Gun | ≥ 2 loại súng khác nhau rõ rệt về số liệu. Nút switch trên HUD. Muzzle flash + particle khi đạn chạm. Recoil (camera shake/kick model). |
| 6 | Bom | Ném/đặt bom, nổ theo bán kính, gây sát thương **và** lực vật lý (`AddExplosionForce` hoặc tương đương) hất zombie. |
| 7 | Level | Level 1: mặt phẳng + vật cản chặn đường (zombie phải né). Level 2: có dốc + zombie khổng lồ (boss/elite). Mỗi level ~3 phút, mật độ spawn tăng dần theo thời gian. Level 2 là điểm cộng, không bắt buộc. |
| 8 | Audio/Particle | Có SFX bắn, trúng đạn, zombie chết, bom nổ, nhạc nền. Particle đi kèm các sự kiện trên. |
| — | Multi-resolution | HUD dùng Canvas Scaler scale theo màn hình, không vỡ layout ở tỉ lệ 16:9, 18:9, 20:9 và tablet 4:3. |

## 3. Bốn trục chấm điểm và chỗ thể hiện trong game

- **Gameplay** — vòng lặp: di chuyển, bắn, đổi súng, ném bom, sống sót hết thời gian level. Nhịp spawn tăng dần tạo cao trào.
- **Physics** — bom nổ hất zombie bằng lực thật; đạn/vật cản dùng collider; zombie khổng lồ có khối lượng khác. Dốc ở Level 2 thử khả năng đi trên mặt nghiêng.
- **Animation (Blend Trees)** — soldier: blend tree locomotion (idle/walk/run theo tốc độ, có thể 2D theo hướng strafe) + layer bắn tách thân trên. Zombie: blend tree walk/run theo tốc độ agent.
- **Shader, Visual, UI, multi-resolution** — shader dissolve zombie, toon shading (Toony Colors Pro 2 có sẵn), HUD từ Layer Lab GUI Pro, hỗ trợ nhiều tỉ lệ màn hình.

## 4. Phân rã hệ thống (đề xuất, theo `CODE-RULE.md` §1)

```
Assets/Scripts/
├── Core/        # GameManager: state machine (Menu / Playing / Paused / LevelWon / GameOver), level timer
├── Player/      # SoldierController (joystick → movement), SoldierAnimator (layers/blend tree), SoldierHealth
├── Weapons/     # Gun (base data-driven), GunSwitcher, Projectile/hitscan, Bomb, Explosion
├── Enemies/     # ZombieAI (NavMeshAgent), ZombieHealth, ZombieDissolve, ZombieSpawner (4 hướng, wave curve)
├── Level/       # LevelConfig loader, spawn pacing theo thời gian
├── UI/          # HUD (HP, súng đang cầm, nút switch, nút bom, timer), màn hình thắng/thua, chọn level
├── Data/        # ScriptableObject: GunData, ZombieData, LevelData (thời lượng, đường cong spawn), BombData
├── Audio/       # AudioManager pool one-shot, nhạc nền
└── Utils/       # ObjectPool
Assets/Data/     # Các file .asset instance của Data/ ở trên
```

Số liệu cân bằng (damage, tốc độ bắn, HP zombie, bán kính bom, lực nổ, thời lượng level, đường cong spawn) **chỉ** nằm trong `Assets/Data/*.asset`, không hardcode.

## 5. Sản phẩm nộp

1. **GitHub** — repo này, nhánh `main` là bản nộp. Code sạch, không để scene/asset thừa.
2. **APK** — build Android (IL2CPP, ARM64). `*.apk` bị gitignore → đính kèm vào **GitHub Release**.
3. **Video** — quay trên thiết bị thật hoặc emulator, phải thấy đủ 8 yêu cầu ở mục 2 (đặc biệt: đổi súng, bom hất zombie, zombie dissolve, Level 2 nếu có).

## 6. Ngoài phạm vi

Không có trong đề, không làm trừ khi dư thời gian: save/progression giữa các phiên, cửa hàng/nâng cấp, multiplayer, quảng cáo/IAP, nhiều hơn 2 level.
