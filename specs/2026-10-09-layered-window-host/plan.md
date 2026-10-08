# 阶段 1 · 无边框透明窗口骨架 — 开发计划（Plan）

> 所属分支：`feature/phase-1-layered-window-host`
> 关联需求：`./requirements.md`　关联验证：`./validation.md`
> 文档版本：v1.0 ｜ 创建日期：2026-10-09

---

## 0. 计划说明

### 0.1 分组原则

任务按**可独立验证的交付单元**分组，每个任务组（Task Group, TG）满足：

1. 组内任务可连续执行，不依赖后续组的产出；
2. 组结束时存在**可观测的产物**（文件、可运行窗口、可复现的窗口行为），而非「代码写完了」；
3. 组的完成判据可在 `./validation.md` 中找到对应的验证条目。

### 0.2 执行顺序与依赖

```
TG1 ──> TG2 ──> TG3 ──> TG5 ──> TG6
          └────> TG4 ──┘
```

| 任务组 | 名称 | 前置依赖 | 预估 |
|---|---|---|---|
| TG1 | Interop 窗口样式基础设施 | — | 0.25 天 |
| TG2 | `LayeredWindowHost` 控件（无边框 / 透明 / 置顶 / 任务栏隐藏 / 防激活） | TG1 | 0.5 天 |
| TG3 | 点击穿透开关 | TG2 | 0.25 天 |
| TG4 | MVVM 视图模型与 DI 装配 | TG1 | 0.25 天 |
| TG5 | 独立临时验证 Demo 窗口 | TG2、TG3、TG4 | 0.4 天 |
| TG6 | 双版本验证、文档与提交 | TG5 | 0.35 天 |

**总预估**：约 2 天（对齐 `roadmap.md` 阶段 1 的 2 天预估）。

> 说明：TG4 仅依赖 TG1，故可在 TG2 / TG3 推进期间并行编写。

### 0.3 任务状态标记

沿用 `roadmap.md` 0.2 节：⬜ 未开始 ｜ 🟨 进行中 ｜ ✅ 已完成 ｜ ⛔ 被阻塞

---

## 任务组 1 · Interop 窗口样式基础设施 ⬜

**目标**：把本阶段所需的全部 Win32 调用与常量收敛到 `qDesktop.Interop`，为后续阶段（2 / 9 / 14）沉淀可复用的扩展样式读写范式。

### 任务

1. **1.1** 新增 `NativeMethods`（`internal static partial class`），声明本阶段所需的 P/Invoke：
   - `GetWindowLongPtr` / `SetWindowLongPtr`（按 `IntPtr.Size` 分派 32 / 64 位入口）；
   - `SetWindowPos`。
2. **1.2** 新增 `WindowExtendedStyle`（`[Flags]` 枚举），覆盖本阶段所需的扩展样式位：
   - `WS_EX_LAYERED = 0x00080000`、`WS_EX_TRANSPARENT = 0x00000020`、
     `WS_EX_TOOLWINDOW = 0x00000080`、`WS_EX_NOACTIVATE = 0x08000000`。
3. **1.3** 新增常量：`GWL_EXSTYLE = -20`、`WM_MOUSEACTIVATE = 0x0021`、`MA_NOACTIVATE = 3`、
   `SWP_*` 组合（`SWP_NOMOVE`、`SWP_NOSIZE`、`SWP_NOZORDER`、`SWP_NOACTIVATE`、`SWP_FRAMECHANGED`）。
4. **1.4** 新增扩展样式辅助（纯逻辑，无副作用），供上层复用：
   - `WithFlag(WindowExtendedStyle current, WindowExtendedStyle flag)`；
   - `WithoutFlag(WindowExtendedStyle current, WindowExtendedStyle flag)`；
   - `HasFlag(WindowExtendedStyle current, WindowExtendedStyle flag)`。
5. **1.5** 在 `App` 层可消费的位置暴露**托管封装**（如 `WindowStyleService`）：提供「读取扩展样式 / 按需置位 / 按需清除 / 提交生效」四个动作，内部完成 `SetWindowPos(SWP_FRAMECHANGED …)` 的补发。**`App` 不得直接 `DllImport`**（依据 `requirements.md` 5.1 第 2 条）。

### 交付物

- `qDesktop.Interop` 内的 P/Invoke 声明、常量与样式辅助
- 面向 `App` 的托管封装（不暴露裸指针与句柄算术）

### 完成判据

- `qDesktop.Interop` 可独立编译，零警告零错误。
- `qDesktop.Interop` 不引用 WPF 程序集。
- 全仓库搜索 `DllImport` 仅命中 `qDesktop.Interop`。

**验证对照**：`validation.md` V1.3、V1.4

---

## 任务组 2 · `LayeredWindowHost` 控件 ⬜

**目标**：实现一个开箱即用的无边框分层窗口基类，默认满足「无边框、半透明、置顶、无任务栏按钮、不抢焦点」。

### 任务

1. **2.1** 新增 `LayeredWindowHost : Window`，在默认样式中设定：
   - `WindowStyle = None`、`AllowsTransparency = true`、`ResizeMode = NoResize`；
   - `Topmost = true`、`ShowInTaskbar = false`；
   - `Background = Transparent`（半透明效果由内容层控制，阶段 1 先用低不透明度背景验证）。
2. **2.2** 在 `SourceInitialized` 中获取 `HwndSource`，一次性应用扩展样式：
   - 追加 `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`；
   - 对 `WS_EX_LAYERED` 执行**幂等**处理：读取当前样式，已置位则不重复写（依据 `requirements.md` 3.4）。
3. **2.3** 在 `SourceInitialized` 中通过 `HwndSource.AddHook` 挂载窗口过程钩子，拦截 `WM_MOUSEACTIVATE` 并返回 `MA_NOACTIVATE`。
4. **2.4** 在窗口 `Closed` 时移除钩子并释放 `HwndSource` 引用，避免句柄泄漏。
5. **2.5** 约束 `ShowInTaskbar` 为初始化期设定：在窗口显示后尝试运行时切换时，记录一条警告日志并忽略该变更（依据 `requirements.md` 3.4）。

### 交付物

- `LayeredWindowHost` 控件
- 可被 Demo 窗口直接继承的窗口基类

### 完成判据

- Demo 窗口呈现为**无边框**、**无任务栏按钮**、**始终置顶**。
- 点击 Demo 窗口时，当前前台窗口**不失焦**（任务栏其他窗口标题栏无闪烁）。
- 通过 `Spy++` 或等效工具可观察到窗口含 `WS_EX_TOOLWINDOW` 与 `WS_EX_NOACTIVATE`。

**验证对照**：`validation.md` V2.1–V2.5、V4.1–V4.3

---

## 任务组 3 · 点击穿透开关 ⬜

**目标**：让窗口可在「可交互」与「鼠标穿透」两种状态间即时切换。

### 任务

1. **3.1** 在 `LayeredWindowHost` 上新增 `IsClickThrough` 属性（`bool`），默认 `false`。
2. **3.2** 实现属性变更逻辑：置位时追加 `WS_EX_TRANSPARENT`，清除时剥离；两种情形均补发 `SetWindowPos(… SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE)`。
3. **3.3** 处理边界：在 `SourceInitialized` **之前**设置 `IsClickThrough` 时，仅记录期望状态，待句柄创建后统一应用，避免空句柄操作。
4. **3.4** 暴露只读状态 `IsClickThroughApplied`（当前扩展样式的实际值），用于验证「属性值」与「实际样式」是否一致。

### 交付物

- `IsClickThrough` 开关及其样式切换链路

### 完成判据

- 开启穿透后，在窗口区域点击，事件落到**下层窗口**（如记事本），本窗口不接收点击。
- 关闭穿透后，点击本窗口可被本窗口接收。
- 连续切换 20 次后，`IsClickThrough` 与实际扩展样式位始终一致。

**验证对照**：`validation.md` V3.1–V3.4

---

## 任务组 4 · MVVM 视图模型与 DI 装配 ⬜

**目标**：以 `CommunityToolkit.Mvvm` 实现 Demo 的状态与命令，并注册进阶段 0 的宿主容器。

### 任务

1. **4.1** 在 `qDesktop.App` 引入 `CommunityToolkit.Mvvm` 8.x。
2. **4.2** 新增 `LayeredWindowHostViewModel`：
   - 可观察属性：`IsClickThrough`、`IsTopmost`、`IsVisible`、`StatusText`；
   - 命令：`ToggleClickThroughCommand`、`ToggleTopmostCommand`、`ToggleVisibilityCommand`（`[RelayCommand]` 源生成）；
   - `IsVisible` 的置位触发 3 秒守护定时器自动恢复为 `true`（依据 `requirements.md` 3.6），并在 `StatusText` 中说明。
3. **4.3** 视图模型不直接引用 `Window`；窗口可见性、置顶、穿透的实际应用由**视图**在绑定回调中完成，保持视图模型可脱离 UI 独立构造。
4. **4.4** 在 `App.BuildHost` 中追加注册（依据 `requirements.md` 3.3）：
   - `AddTransient<LayeredWindowHostViewModel>()`；
   - `AddTransient<LayeredWindowHostDemoWindow>()`。
5. **4.5** 定时器须可取消：窗口关闭时停止定时器，避免已释放视图模型被回调。

### 交付物

- `LayeredWindowHostViewModel`
- 宿主容器的两条 `Transient` 注册

### 完成判据

- 应用启动时容器可解析 `LayeredWindowHostDemoWindow`，不抛异常。
- 视图模型不引用任何 WPF `Window` / `Control` 类型。
- 隐藏后 3 秒窗口自动恢复为可见。

**验证对照**：`validation.md` V5.1–V5.3

---

## 任务组 5 · 独立临时验证 Demo 窗口 ⬜

**目标**：把三项窗口属性固化为可复现的人工验收界面。

### 任务

1. **5.1** 新增 `LayeredWindowHostDemoWindow : LayeredWindowHost`，含三个按钮与一块状态文本：
   - 按钮一：切换**点击穿透**；
   - 按钮二：切换**置顶**；
   - 按钮三：切换**可见性**（隐藏后 3 秒自动恢复）。
2. **5.2** 采用数据绑定连接视图模型：按钮绑定命令，状态文本绑定 `StatusText`，窗口自身属性绑定 `IsClickThrough` / `IsTopmost` / `IsVisible`。
3. **5.3** 窗口内添加一段可见的说明文字，注明「本窗口为阶段 1 临时验证产物，阶段 4 删除」。
4. **5.4** 调整启动路径：`App.OnStartup` 改为从容器解析并显示 `LayeredWindowHostDemoWindow`；`MainWindow` 保留注册但不在启动路径显示（依据 `requirements.md` 3.1）。
5. **5.5** 窗口尺寸与位置固定为便于验证的默认值（如 420 × 220，屏幕居中）。

### 交付物

- `LayeredWindowHostDemoWindow`
- `BoolToHiddenVisibilityConverter`（`bool` → `Visibility`，`false` 映射为 `Hidden`；注册在 `App.xaml` 的 `Application.Resources`）
- 切换后的启动路径

### 完成判据

- 应用启动后显示 Demo 窗口，窗口无边框、无任务栏按钮、置顶。
- 三个按钮分别产生预期行为，状态文本与实际状态一致。
- 关闭窗口后进程正常退出，无残留进程。

**验证对照**：`validation.md` V2.1、V3.1、V5.4、V6.1、M1

---

## 任务组 6 · 双版本验证、文档与提交 ⬜

**目标**：完成双版本覆盖策略下的验证记录，并按约定提交形成可评审的 PR。

### 任务

1. **6.1** 在当前系统上逐项执行 `./validation.md` 第 3 节手工验证，记录结果与证据。
2. **6.2** 按 `requirements.md` 5.3 与 `validation.md` V6 的策略处理另一 Windows 版本：标注为「待补」，并在 PR 描述中说明补验时点（建议随阶段 2 双系统探测一并完成）。
3. **6.3** 更新根 `README.md`：补充阶段 1 的运行说明（启动即显示验证窗口、三个按钮的含义）。
4. **6.4** 确认 `specs/` 下三份文档与实现一致；若实现中产生偏差，回改文档而非留待后续。
5. **6.5** 按 Conventional Commits 拆分提交，建议序列：
   - `feat: 新增窗口扩展样式互操作基础设施`
   - `feat: 实现无边框分层窗口基类 LayeredWindowHost`
   - `feat: 支持点击穿透开关与防激活`
   - `feat: 引入 MVVM 视图模型与宿主 DI 注册`
   - `feat: 添加窗口属性验证 Demo 窗口`
   - `docs: 补充阶段 1 规格文档与运行说明`
6. **6.6** 推送分支 `feature/phase-1-layered-window-host` 并创建面向 `main` 的 PR。
7. **6.7** 在 PR 描述中引用本阶段三份文档路径，并附 `validation.md` 的验收结论。

### 交付物

- 验证记录（`validation.md` 附录 A）
- 更新的 `README.md`
- 待评审的 PR

### 完成判据

- 提交信息符合 Conventional Commits。
- PR 描述包含三份文档链接与验证结论。
- 按 `validation.md` 第 4 节清单逐项勾选完毕。

**验证对照**：`validation.md` 第 4 节、附录 A

---

## 附：任务组与路线图关键任务对照

| 路线图阶段 1 关键任务 | 对应任务组 |
|---|---|
| 1. 实现 `LayeredWindowHost`（`WindowStyle=None` / `AllowsTransparency` / `Topmost` / `ShowInTaskbar=false`） | TG2 |
| 2. 通过 `SetWindowLongPtr` 追加 `WS_EX_TOOLWINDOW \| WS_EX_NOACTIVATE \| WS_EX_LAYERED` | TG1、TG2 |
| 3. 实现点击穿透开关（动态增删 `WS_EX_TRANSPARENT`）与 `IsClickThrough` 属性 | TG1、TG3 |
| 4. 实现窗口防激活（拦截 `WM_MOUSEACTIVATE` 返回 `MA_NOACTIVATE`） | TG1、TG2 |
| 5. 编写手动验证 Demo 页（三个按钮切换穿透、置顶、可见性） | TG4、TG5 |

| 路线图阶段 1 验收标准 | 对应验证项 |
|---|---|
| 窗口显示为无边框、无任务栏按钮、始终置顶 | V2.1–V2.4 |
| 开启穿透后鼠标点击落到下层窗口；关闭后正常点击本窗口 | V3.1–V3.3 |
| 显示窗口时任务栏中其他窗口不失焦、不闪烁 | V4.1–V4.3 |
| 在 Win10 与 Win11 上行为一致 | V6.1–V6.2 |

---

*本文档为阶段 1 开发计划的权威来源。任务增删须同步更新 0.2 节的依赖表与 `./validation.md` 的验证条目。*
