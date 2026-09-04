# CODE RULES — Unity (dùng chung cho mọi project)

Quy tắc code bắt buộc cho toàn bộ code first-party trong project. Đọc file này trước khi viết hoặc sửa bất kỳ script nào.

File này **generic** — copy nguyên sang project Unity khác là dùng được. Những gì riêng của từng project (design spec, tên domain, danh sách hệ thống gameplay, thư viện có sẵn) khai báo ở `CLAUDE.md`/README của project đó, không sửa vào đây.

## 1. Cấu trúc thư mục

Toàn bộ code first-party nằm trong `Assets/Scripts/`, chia thư mục theo hệ thống. Bộ khung chuẩn (các thư mục gameplay đặt theo hệ thống thực tế của game):

```
Assets/Scripts/
├── Core/            # Entry point (GameManager), state machine trung tâm, vòng đời game/run
├── <Hệ thống>/      # Mỗi hệ thống gameplay một thư mục (ví dụ: Combat/, Enemies/, Progression/, Events/...)
├── UI/              # Toàn bộ uGUI code
├── Data/            # Class định nghĩa ScriptableObject (.cs)
├── Save/            # Save/load
├── Audio/           # Âm thanh
├── Utils/           # Helper dùng chung, object pool
└── Editor/          # Editor tooling (hoặc Editor/ con của thư mục liên quan)
```

- File `.asset` (instance của ScriptableObject — số liệu cân bằng thật) nằm ở `Assets/Data/`, tách khỏi code.
- **Editor script** đặt trong thư mục `Editor/` (assembly editor riêng, không lọt vào build).
- **KHÔNG** đặt code mới vào thư mục third-party (plugin, asset pack mua/tải về).

## 2. Naming convention (C# chuẩn Unity)

- Class / struct / enum / method / property: `PascalCase`.
- Field private: `_camelCase` (prefix `_`). Field được serialize: `[SerializeField] private Type _name;` — không dùng field public chỉ để hiện trong Inspector.
- Local variable / parameter: `camelCase`.
- Constant: `PascalCase` (không `ALL_CAPS`).
- Enum không dùng prefix (viết `EventType.Battle`, không `EventType.EVENT_BATTLE`).
- Một class chính mỗi file, tên file trùng tên class.
- **Tên theo domain của project:** nếu project có design spec, dùng đúng thuật ngữ trong spec cho khái niệm đã có — không tự đặt tên khác. Tên viết kiểu `ALL_CAPS` trong spec (ví dụ `EVENT_WEIGHTS`) khi vào C# đổi sang `PascalCase` (`EventWeights`).

## 3. Kiến trúc

- **Data-driven bằng ScriptableObject:** mọi chỉ số cân bằng (chỉ số base, công thức scaling, tỉ lệ/weight, timing, nội dung item/upgrade, palette...) nằm trong file `.asset` dưới `Assets/Data/` (class định nghĩa ở `Assets/Scripts/Data/`) — KHÔNG hardcode số cân bằng trong logic. Designer phải chỉnh được số mà không sửa code.
- **Tách logic khỏi presentation:** logic gameplay không phụ thuộc vào UI/VFX. UI lắng nghe qua C# `event`/`Action`; logic không gọi thẳng vào UI. Class logic thuần (không kế thừa `MonoBehaviour`, không đụng scene) test được mà không cần chạy game.
- **State machine trung tâm:** trạng thái game (menu/chơi/pause/thua...) quản lý ở một nơi (GameManager hoặc tương đương). Mọi hành vi phụ thuộc trạng thái (nút bấm được không, save ghi lúc nào, panel mở/đóng) hỏi state này — không tự track state riêng lẻ ở từng component.
- **Không Singleton tràn lan:** tối đa một entry point (GameManager). Các hệ thống khác nhận reference qua Inspector hoặc được truyền vào, không `static Instance` khắp nơi.
- **Object pooling bắt buộc** cho mọi object spawn lặp lại trong gameplay (floater, projectile, VFX, quái...) — không `Instantiate`/`Destroy` liên tục trong gameplay.

## 4. Quy tắc Unity cụ thể

### Code phải tường minh — mọi thứ khai báo sẵn trong scene/prefab

- **CẤM bootstrap runtime:** không tạo GameObject hay gắn script lúc runtime mà không tồn tại sẵn trong scene hoặc prefab. Cụ thể:
  - Không `new GameObject(...)` + `AddComponent<T>()` để dựng hệ thống lúc chạy.
  - Không dùng `[RuntimeInitializeOnLoadMethod]` để tự khởi tạo manager/singleton ẩn.
  - Không có pattern "tự tạo Instance nếu chưa có" (`if (Instance == null) new GameObject(...)`).
  - Mọi manager, hệ thống, UI đều phải là object đặt sẵn trong scene hoặc prefab được instantiate tường minh — nhìn Hierarchy/Project là thấy được toàn bộ hệ thống.
- **Không `AddComponent`:** component phải được gắn sẵn trên prefab/scene object. Object động (floater, VFX, quái) là prefab đầy đủ component, lấy ra từ pool.
- **Không `GetComponent`/`GetComponentInChildren`/`TryGetComponent`** để tìm reference lúc runtime — mọi reference khai báo `[SerializeField]` và kéo thả trong Inspector (hoặc gán sẵn trong prefab). Ngoại lệ duy nhất: đọc component từ kết quả physics (collision/trigger/raycast/overlap — không có cách nào khác). Ưu tiên hơn nữa: manager đăng ký sẵn map khi spawn (tra dictionary thay vì `GetComponent` mỗi lần).
- Không dùng `GameObject.Find`, `FindObjectOfType`, tìm theo tag/tên — reference qua Inspector.
- Không dùng chuỗi (`Invoke("MethodName")`, `SendMessage`) — dùng gọi trực tiếp hoặc event.
- `Update` chỉ ở nơi thật sự cần chạy mỗi frame; timer đơn giản (beat, cooldown, animation ngắn) dùng cộng dồn `Time.deltaTime`, không tạo coroutine tràn lan.
- Tween dùng thư viện tween của project (thường là **DOTween**); với DOTween luôn đặt `SetLink(gameObject)` để tween tự hủy theo object.
- Không dùng `Resources.Load` cho asset gameplay — reference qua Inspector/ScriptableObject.
- Text dùng **TextMeshPro**, không dùng `UnityEngine.UI.Text` cũ.
- Input dùng **Input System mới** (asset `.inputactions`), không dùng `Input.GetKey` cũ — trừ khi project đã chọn hệ input khác từ đầu.
- **Không dùng `[Tooltip]`:** tên field phải tự nói nó là gì (mục 2 và mục 6) — tooltip chỉ chép lại tên đó thành một dòng chữ nữa, làm class phình ra và không ai đọc khi đọc code. Ràng buộc không hiển nhiên ("đổi `id` là hỏng draw order", "để 0 là tắt") viết thành comment `//` một dòng ngay trên field theo mục 6; mô tả cả một hệ thống thì viết vào `CLAUDE.md`/doc của project, không nhét vào attribute. `[Header]`, `[Range]`, `[SerializeField]`, `[Space]` vẫn dùng bình thường: chúng đổi hành vi Inspector thật, `[Tooltip]` thì không.

### Code không quyết định hình thức

Màu, kích thước, nhịp animation là việc của prefab/scene, không phải của script. Script chỉ **chọn giữa** các giá trị đã author, không **tạo ra** giá trị.

- **Không hardcode màu trong code runtime:** không `new Color(...)`, không `Color.white`/`Color.red`, không nhân-chia màu để tự làm nhạt/đậm (`c * 0.55f`). Màu là `[SerializeField] Color` kéo trong Inspector, hoặc một entry trong ScriptableObject palette.
- **Field màu không đặt giá trị khởi tạo.** Viết `[SerializeField] private Color _lockedColor;`, không `= new Color(...)`. Giá trị đã serialize trong prefab mới là nguồn sự thật; initializer chỉ tạo ra một màu thứ hai mà designer không nhìn thấy, không sửa được, và nó âm thầm thắng bảng màu chung mỗi lần ai đó add component vào object mới.
- **Cùng luật cho mọi con số hình thức khác:** alpha, offset, scale lúc nhấn, font size, thời lượng và ease của tween, khoảng cách grid. Serialize hết, đặt dưới `[Header]` theo nhóm.
- **Code được phép chọn, không được phép chế:** `fill.color = ready ? readyColor : fillColor` là đúng; `fill.color = new Color(0.45f, 1f, 0.4f)` là sai. Cùng logic đó, `stars[i].color = on ? starOnColor : starOffColor`.
- **Ngoại lệ, chỉ bốn:**
  1. **Cache runtime** đọc ra từ renderer để khôi phục lại sau (`baseBoxColor = boxRenderer.material.color`) — nó chép giá trị author, không phát minh ra giá trị.
  2. **Fallback trung tính khi data nguồn thiếu:** `palette ? palette.UiColor(id) : Color.white`. `Color.white` ở đây nghĩa là "không tint", tức là không quyết định gì cả. Fallback phải trung tính — trả về một màu *cụ thể* (đỏ, vàng) là đang chọn hình thức trong code.
  3. **`Gizmos.color` trong `OnDrawGizmos`** — chỉ vẽ trong editor, không phải hình thức của game.
  4. **Editor tool sinh UI** (scaffolding một lần): đó chính là chỗ author. Nhưng ngay khi prefab tồn tại thì prefab là nguồn sự thật, và tool phải từ chối ghi đè thứ đã có.
- **Cách soát:** `grep -rn "new Color(\|Color\.white" Assets/<runtime scripts>`. Mỗi kết quả hoặc rơi vào ba ngoại lệ trên, hoặc là một quyết định hình thức đang nằm sai chỗ.

Lý do không chỉ là gu code. Một màu nằm trong script thì designer không sửa được, code review không thấy nó lệch bảng màu, và nó không xuất hiện khi rà prefab — nên nó là kiểu lệch style sống lâu nhất.

## 5. Performance — ưu tiên hàng đầu

Mặc định game chạy mobile/WebGL; mọi code trong gameplay loop phải viết theo tư duy "frame budget".

### 5.1. Zero allocation trong hot path

Hot path = mọi code chạy mỗi frame hoặc nhiều lần mỗi giây trong gameplay (`Update` của manager, tick logic, animation, floater/projectile). Trong hot path:

- **Không tạo garbage:** không `new` class/array/List, không LINQ (`Where`, `Select`, `OrderBy`, `Any`...), không `foreach` trên interface (`IEnumerable`), không string concat/`$"..."`, không boxing (đưa struct/enum vào `object`).
- Không lambda/closure bắt biến ngoài trong hot path (mỗi lần tạo là một allocation) — cache delegate một lần ở `Awake` nếu cần.
- Collection dùng lại: khai báo `List`/mảng làm field, `Clear()` rồi dùng lại thay vì tạo mới. Khởi tạo với capacity dự kiến (`new List<Floater>(32)`).
- Physics query dùng bản không cấp phát: overload nhận buffer/`List` cấp sẵn (Unity 6 khuyến nghị overload nhận `ContactFilter2D` + `List<T>`; các API `*NonAlloc` cũ một phần đã obsolete).
- So khoảng cách dùng `sqrMagnitude` (so với bình phương range), không `Vector3.Distance`/`magnitude` (tránh sqrt).
- Coroutine: cache `WaitForSeconds` thành field nếu lặp lại; ưu tiên timer cộng `Time.deltaTime` cho nhịp/cooldown (đã quy định ở mục 4).
- Text cập nhật thường xuyên (HP, điểm, damage): chỉ set khi giá trị đổi, không set mỗi frame; số format dùng cache chuỗi cho giá trị nhỏ lặp lại nếu profiler chỉ ra vấn đề.

### 5.2. Cấu trúc update hiệu quả

- **Update manager thay vì trăm `Update()`:** floater, hiệu ứng, timer... không tự có `Update()` riêng. Mỗi hệ thống có một manager giữ danh sách active object và tick chúng trong một `Update()` duy nhất (ví dụ `FloaterManager.Update()` lặp qua list floater). Nhiều `MonoBehaviour.Update()` riêng lẻ có overhead interop đáng kể.
- Cache mọi thứ truy cập lặp lại: `Transform` (field, không gọi `.transform` lặp trong loop), `Time.deltaTime` đọc một lần mỗi frame nếu dùng nhiều lần. Camera reference qua `[SerializeField]` trong Inspector — không dùng `Camera.main` (bản chất là tìm theo tag, vi phạm mục 4).
- `transform.position`/`rotation`: đọc một lần vào biến local, tính toán, ghi lại một lần — không đọc/ghi property nhiều lần trong một phép tính.
- Object active track bằng danh sách trong manager — **không** `FindObjectsOfType` hay scan scene; manager là source of truth (spawn thì add vào list, xong thì remove).

### 5.3. Object pooling (chi tiết hóa mục 3)

- Pool cho: floater (damage/heal/điểm), projectile, VFX, audio one-shot, và mọi object spawn lặp lại trong gameplay.
- **Prewarm** pool trong loading/trước gameplay — không để `Instantiate` xảy ra giữa trận gây spike.
- Trả về pool bằng `SetActive(false)` + reset state trong method `OnDespawn` tường minh; không dựa vào `OnDisable` để chứa logic quan trọng.
- Pool hết thì mở rộng có kiểm soát (log warning để tune size), không âm thầm `Instantiate` từng cái.

### 5.4. uGUI performance

- **Chia Canvas theo tần suất thay đổi:** một element thay đổi làm rebuild cả Canvas chứa nó. Tách riêng: Canvas tĩnh (khung, nền), Canvas HUD thay đổi thường xuyên (HP, điểm, tiến độ), Canvas popup/hiệu ứng. Không dồn tất cả vào một Canvas.
- Tắt `Raycast Target` trên mọi Image/Text không cần nhận input (đa số là không cần).
- Không dùng Layout Group (`HorizontalLayoutGroup`...) + `ContentSizeFitter` cho UI thay đổi trong gameplay — chúng rebuild đắt; layout động thì set vị trí bằng code. Layout Group chỉ chấp nhận được cho UI tĩnh dựng một lần (panel chọn thẻ, hàng chip cố định).
- Ẩn UI bằng cách disable `Canvas` component (hoặc `CanvasGroup.alpha = 0` + tắt interactable/raycast), không `SetActive(false)` cả hierarchy lớn nếu sẽ bật lại thường xuyên (tránh chi phí rebuild khi bật).
- Sprite UI đóng gói vào Sprite Atlas để giảm draw call.

### 5.5. Rendering & mobile

- `Application.targetFrameRate = 60` set tường minh ở khởi động (mặc định mobile là 30).
- Nội dung nền/level dựng **một lần khi đổi ngữ cảnh** (vào level, đổi map/theme) rồi giữ nguyên — không sinh lại chi tiết nền mỗi frame.
- Hạn chế overdraw: không xếp chồng nhiều layer sprite/UI trong suốt full-screen; particle giới hạn max particles và không dùng particle trong suốt phủ màn hình.
- Material dùng chung (shared) cho object cùng loại để batch được; **không** `renderer.material` (tạo instance material — phá batching và leak) — đổi màu per-object thì dùng `MaterialPropertyBlock` hoặc màu vertex/sprite color.
- Shadow, post-processing đắt tiền tắt trên mobile trừ khi art direction yêu cầu và đã profile.

### 5.6. Đo trước khi tối ưu thêm

- Các quy tắc trên là mặc định (rẻ để tuân thủ ngay từ đầu). Tối ưu **sâu hơn** (job system, burst, cache-friendly layout...) chỉ làm khi Profiler chỉ ra bottleneck thật — không phức tạp hóa code vì vấn đề chưa đo được.
- Khi nghi ngờ performance: dùng Unity Profiler trên thiết bị/target thật, nhìn GC Alloc và CPU time, sửa đúng chỗ nóng nhất.

## 6. Clean code

- **Mỗi class một trách nhiệm**, đặt tên nói rõ trách nhiệm đó (`EventRoller`, `BattleBeatTicker`, `EnemyStatScaler` — không `GameHelper`, `Utils2`, `ManagerManager`).
- Method ngắn, làm một việc; ưu tiên **guard clause / early return** thay vì if lồng sâu (tối đa ~2 tầng lồng).
- Không viết trước cho tương lai (YAGNI): không interface/abstraction cho thứ mới có một implementation, không tham số "để sau này dùng". Thêm abstraction khi có nhu cầu thứ hai thật sự.
- Không copy-paste logic — lặp đến lần thứ hai thì tách method/class chung.
- State phơi bày tối thiểu: field `private` mặc định, property chỉ có setter khi thật sự cần bên ngoài ghi; dùng `readonly` cho reference gán một lần.
- Điều kiện phức tạp tách thành biến/method có tên (`bool mustForceBattle = ...` hoặc `IsBossRound(round)`) thay vì biểu thức dài trong `if`.
- Event đặt tên theo chuyện đã xảy ra (`OnBattleEnded`, `OnLevelUp`), subscriber tự lo việc của mình — publisher không biết ai nghe.
- **Mặc định là không comment.** Code tự nói nó *làm gì*: đổi tên biến/method cho đúng, tách một method có tên, hoặc rút điều kiện dài thành một `bool` có tên — đó mới là cách giải thích. Không chép lại dòng ngay bên dưới (`// cộng điểm` trên `score++`), không đánh dấu vùng (`// ---- UI ----`), không viết XML doc `///` cho method chỉ dùng nội bộ trong project. Một comment sai thì không compile nên không ai phát hiện: nó tồn tại lâu hơn cả đoạn code nó mô tả, và người đọc sau tin nó thay vì tin code.
- **Chỉ comment "tại sao", và chỉ khi lý do không đọc được từ code:** ràng buộc thứ tự ("`Setup` fence trước vì nó định nghĩa stop line"), một luật trong design spec, một workaround của Unity/thư viện, một con số trông tùy tiện nhưng đến từ chỗ khác. Viết một dòng `//` ngay trên đúng chỗ nó nói về — không phải một khối comment đầu method. Cần mô tả cả một hệ thống thì viết vào `CLAUDE.md`/doc của project, chỗ đó có người đọc và có người cập nhật.
- **Không `[Tooltip]`** — xem mục 4. Cùng một lý do: một dòng chữ chép lại tên field không phải là tài liệu, chỉ là thứ phải sửa thêm một lần nữa khi field đổi nghĩa.
- Xử lý null tường minh: reference Inspector bắt buộc thì validate trong `Awake` (fail sớm, log rõ thiếu gì) thay vì NullReference giữa game.

## 7. OOP & SOLID

Áp dụng SOLID theo hướng thực dụng cho Unity — mục tiêu là code dễ mở rộng nội dung game (thêm item, level, event, loại quái mới) mà không sửa code cũ.

### 7.1. Nguyên tắc OOP nền tảng

- **Composition hơn inheritance:** Unity là component-based — ghép hành vi bằng nhiều component/class nhỏ thay vì cây kế thừa sâu. Cây kế thừa tối đa 2 tầng, sâu hơn là dấu hiệu cần tách composition.
- **Encapsulation triệt để:** state là `private`, bên ngoài tương tác qua method/event có tên theo hành vi (`TakeDamage(int)`, không `set Hp`). Không có class nào "thò tay" chỉnh field của class khác.
- **Không God object:** GameManager chỉ điều phối state machine và vòng đời game — không chứa logic gameplay của từng hệ thống. Mỗi hệ thống trong mục 1 tự quản logic của mình.

### 7.2. SOLID áp dụng cụ thể

- **S — Single Responsibility:** một class, một lý do để thay đổi. Ví dụ combat tách thành: logic nhịp/damage, logic buff/trạng thái, và view (animation, floater) — sửa hiệu ứng đánh không đụng vào công thức damage.
- **O — Open/Closed:** thêm nội dung mới bằng cách **thêm** asset/class, không **sửa** code cũ. Cấm switch-case/if-else theo loại nội dung (loại event, loại item, loại quái) rải rác trong code — hành vi riêng của từng loại nằm trong class/ScriptableObject của loại đó (polymorphism hoặc data-driven). Thêm nội dung thứ N+1 không được phép sửa file nào ngoài file của chính nó + data.
- **L — Liskov Substitution:** class con phải dùng được ở mọi nơi class cha được dùng. Không override method thành no-op hay throw để "vô hiệu hóa" hành vi thừa kế — nếu phải làm vậy nghĩa là kế thừa sai chỗ (tách hẳn hai nhánh thay vì kế thừa rồi vô hiệu hóa).
- **I — Interface Segregation:** interface nhỏ theo vai trò: `IDamageable`, `ITickable`... — không một interface khổng lồ bắt mọi loại implement cả những gì nó không có. Hệ thống chỉ phụ thuộc đúng interface nó cần (combat chỉ biết `IDamageable`, không cần biết đó là quái thường hay boss).
- **D — Dependency Inversion:** hệ thống cấp cao phụ thuộc abstraction, không phụ thuộc chi tiết. Trong Unity nghĩa là: logic gameplay phát `event`/gọi interface, không reference trực tiếp class UI/VFX cụ thể (đã quy định ở mục 3); wiring cụ thể diễn ra qua Inspector — Inspector chính là composition root của project.

### 7.3. Giới hạn — SOLID không phải mục tiêu tự thân

- Tuân thủ YAGNI ở mục 6: **không** tạo interface/abstraction khi mới có một implementation và chưa có nhu cầu thay thế thật (interface có từ 2 implementer trở lên, hoặc là ranh giới hệ thống như `IDamageable`). SOLID dùng để code dễ thêm nội dung, không phải để tăng số file.

## 8. Chất lượng & quy trình

- Sau mỗi lần thêm/sửa script: compile-check trước khi coi là xong (qua Unity MCP `validate_script` + `read_console` nếu có, hoặc để editor compile và đọc Console) — không được để lại compile error hoặc warning mới.
- Không commit code chết (method/field không dùng), không để `Debug.Log` rác trong code hoàn thiện (log quan trọng thì có prefix hệ thống, ví dụ `[Combat]`, `[Save]`).
- Magic number trong logic phải thành constant có tên hoặc field trong ScriptableObject config.
- Nếu project có design spec: logic gameplay phải tôn trọng tuyệt đối luật và công thức trong spec — code không được "sáng tạo" thêm cơ chế; cân bằng chỉnh theo đúng các nút vặn (tuning lever) spec chỉ định. Spec của project nào nằm ở đâu do `CLAUDE.md`/README của project đó khai báo.
