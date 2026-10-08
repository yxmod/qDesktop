# 阶段 1 · 无边框透明窗口骨架 — 需求文档（Requirements）

> 所属分支：`feature/phase-1-layered-window-host`
> 对应路线图阶段：`specs/roadmap.md` 阶段 1
> 关联文档：`./plan.md`（开发计划）、`./validation.md`（验证标准）
> 编写依据：`specs/mission.md`、`specs/tech-stack.md`
> 文档版本：v1.0 ｜ 创建日期：2026-10-09 ｜ 状态：待确认

---

## 1. 业务背景

### 1.1 项目定位

qDesktop 为 Windows 用户提供**轻量、开源、可自由组织**的桌面图标管理工具，把混乱的系统桌面转化为可分层、可收纳、可自动隐藏的个人工作空间（见 `mission.md` 第 1 节）。其核心差异化能力为**自由层级**与**边缘隐藏**。

### 1.2 本阶段为何存在

阶段 1 是全部 19 个阶段中**第一个触碰真实窗口行为**的阶段，也是路线图 0.1 节「技术前提前置」原则的第一个落点。栅栏渲染（阶段 4）与边缘隐藏（阶段 10–11）在物理上都依赖同一个前提：

> 一个**无边框、半透明、始终置顶、可动态穿透**的窗口，能否在 Windows 上稳定共存且不干扰其他程序的焦点。

这四项属性各自成熟，但**两两组合存在已知冲突**（`tech-stack.md` 第 4 节将其列为「先验证、后投入」的技术前提之一）。若不在早期用最小窗口验证，风险将延后到阶段 4/11 才暴露，届时返工成本被放大到整个渲染层。

| 若缺失本阶段 | 后果 |
|---|---|
| 未验证分层 + 穿透共存 | 阶段 4 栅栏可能遮挡桌面图标点击，或穿透后无法再交互 |
| 未验证防激活 | 栅栏每次刷新可能抢占前台焦点，导致用户正在输入的其他窗口失焦 |
| 未验证无任务栏按钮 | 每个栅栏都会在任务栏留下条目，与「轻量」定位冲突 |
| 未沉淀窗口样式基础设施 | 阶段 2 桌面层嵌入需重复编写 P/Invoke 与样式管理代码 |

**结论**：阶段 1 不产出面向最终用户的功能，其价值在于**出清窗口层技术风险**，并沉淀 `qDesktop.Interop` 的第一批可复用基础设施。

### 1.3 业务价值

1. 把「分层 + 无边框 + 置顶 + 穿透」从假设变为**已验证的事实**，为阶段 4 的渲染层提供可信基线。
2. 建立 `WS_EX_*` 扩展样式的读写与生命周期管理范式，阶段 2（桌面层嵌入）、阶段 9（隐藏不占位）、阶段 14（多屏）均将复用。
3. 通过独立 Demo 窗口把验证过程固化为可复现的**人工验收路径**，避免「只在开发者本机可复现」的隐性知识。

---

## 2. 功能范围

### 2.1 范围内（In Scope）

| 编号 | 范围项 | 说明 |
|---|---|---|
| S1 | `LayeredWindowHost` 控件 | 继承 `Window`，默认 `WindowStyle=None`、`AllowsTransparency=true`、`Topmost=true`、`ShowInTaskbar=false`、`ResizeMode=NoResize` |
| S2 | 扩展样式基础设施 | `qDesktop.Interop` 内集中声明 `WS_EX_*` 常量、`GWL_EXSTYLE`、`SetWindowPos` 等 P/Invoke，并提供扩展样式的**幂等**组合 / 剥离辅助 |
| S3 | 工具窗口与无激活样式 | 在 `SourceInitialized` 时追加 `WS_EX_TOOLWINDOW \| WS_EX_NOACTIVATE`，消除任务栏条目与激活行为 |
| S4 | 点击穿透开关 | `IsClickThrough` 属性，动态增删 `WS_EX_TRANSPARENT`，并调用 `SetWindowPos(SWP_FRAMECHANGED …)` 使样式即时生效 |
| S5 | 窗口防激活 | 拦截 `WM_MOUSEACTIVATE` 返回 `MA_NOACTIVATE`，保证点击不抢夺前台焦点 |
| S6 | MVVM 视图模型 | `LayeredWindowHostViewModel`：`IsClickThrough`、`IsTopmost`、`IsVisible`、`StatusText` 及对应命令，使用 `CommunityToolkit.Mvvm` 源生成器 |
| S7 | DI 装配 | 在阶段 0 的宿主容器中注册视图模型与 Demo 窗口 |
| S8 | 独立临时验证 Demo 窗口 | `LayeredWindowHostDemoWindow`：三个按钮分别切换**穿透**、**置顶**、**可见性**；应用启动路径改为显示该窗口 |
| S9 | 窗口层验证文档 | 本目录三份文档（`plan.md` / `requirements.md` / `validation.md`） |

### 2.2 范围外（Out of Scope）

以下内容**明确不在阶段 1 内**：

- **不做** 桌面层定位与嵌入（`Progman` / `WorkerW`、`IDesktopHostAdapter`）——属阶段 2。
- **不做** 栅栏的渲染、标题栏、圆角、阴影与主题令牌系统——属阶段 4。
- **不做** 栅栏的创建、移动、缩放、吸附与 Z 序持久化——属阶段 5。
- **不做** 图标读取、图标缓存与网格布局——属阶段 6。
- **不做** 边缘隐藏的触发检测、动画与状态机——属阶段 10、阶段 11。
- **不做** 托盘图标、全局热键与「隐藏不占位」语义——属阶段 9。
- **不做** 多显示器枚举与 DPI 感知模式设置——属阶段 14。
- **不引入** UI 自动化测试（`tech-stack.md` 2.5 节：以人工验收清单替代）。
- **不新增** 测试项目或 `qDesktop.Core` 抽象（见 3.2、3.7）。
- **不修改** `qDesktop.Core` 的任何代码——本阶段 Core 无窗口概念。

---

## 3. 技术决策

### 3.1 Demo 形态：独立临时窗口，阶段 4 整体删除

**决策**：新增独立窗口 `LayeredWindowHostDemoWindow` 承载三个验证按钮，与阶段 0 的 `MainWindow` **完全解耦**；应用启动路径改为显示 Demo 窗口。该窗口标记为**临时产物**，在阶段 4 栅栏渲染落地时**整体删除**（含其视图模型与 DI 注册）。

**理由**：

1. 阶段 4 会把 `MainWindow` 演进为承载栅栏的桌面宿主窗口（`roadmap.md` 阶段 4 任务 4：「从 `layout.json` 加载栅栏并渲染到桌面宿主窗口上」）。若本阶段把验证逻辑塞进 `MainWindow`，阶段 4 必须先剥离再重建，属无谓返工。
2. 验证 Demo 的职责是「证明窗口属性成立」，其生命周期天然短于产品代码；显式标记为临时产物可避免它被误当作正式界面维护。
3. 独立窗口使「窗口属性是否生效」的观察对象唯一——Demo 窗口即被测窗口，无需在混合界面中分辨哪一层产生了行为。

**已识别的代价与缓解**：

| 代价 | 缓解措施 |
|---|---|
| 阶段 1 的启动展示不再是阶段 0 的 `MainWindow`，阶段 0 的「启动显示空窗口」验收在当前分支上不再成立 | 在 `validation.md` 第 6.2 节登记为**有意的行为变更**，并说明阶段 4 起由栅栏宿主窗口接管启动展示 |
| 阶段 4 需要一次删除动作，存在「忘记删除」的风险 | 在代码注释与 `validation.md` 第 4 节合并清单中显式登记「Demo 属临时产物」标记，供阶段 4 评审时核对 |

### 3.2 分层归属：`Interop` 声明 + `App` 控件，不新增 `Core` 抽象

**决策**：全部 Win32 常量、P/Invoke 与扩展样式算术集中在 `qDesktop.Interop`；`LayeredWindowHost` 作为 WPF `Window` 子类置于 `qDesktop.App`；**不在 `qDesktop.Core` 新增任何窗口宿主抽象接口**。

**理由**：

1. `tech-stack.md` 2.3 节的架构约束要求「所有 P/Invoke 声明集中在 `qDesktop.Interop` 程序集内，禁止在业务层直接调用」——本决策是该约束的直接执行。
2. `LayeredWindowHost` 依赖 `Window`、`HwndSource`、`AllowsTransparency` 等 WPF 类型，而 `Core` 被硬性禁止引用 WPF（阶段 0 `validation.md` V1.6）。因此控件只能落在 `App`。
3. 阶段 2 的 `IDesktopHostAdapter` 抽象应以**阶段 2 的真实需求**（多适配器分派）为输入设计，而非在阶段 1 凭猜测预置。过早抽象会产生与阶段 2 实际形态不匹配的接口，反而增加改造量。

**边界划分**：

| 程序集 | 本阶段新增内容 | 允许依赖 |
|---|---|---|
| `src/qDesktop.Interop` | `NativeMethods`（P/Invoke）、`WindowExtendedStyle`（`[Flags]` 枚举）、`WindowMessages` 常量、扩展样式组合辅助 | 仅 BCL |
| `src/qDesktop.App` | `LayeredWindowHost`、`LayeredWindowHostViewModel`、`LayeredWindowHostDemoWindow`、DI 注册 | Core、Interop |
| `src/qDesktop.Core` | **无改动** | 仅 BCL |

> 与阶段 0 一致，`Core → Interop` 与 `Core → WPF` 的引用禁令继续由项目引用在编译期强制（阶段 0 `validation.md` V1.6）。

### 3.3 MVVM 与 DI：引入 `CommunityToolkit.Mvvm` 并在宿主容器注册

**决策**：本阶段即引入 `CommunityToolkit.Mvvm` 8.x，Demo 窗口采用 `ViewModel` + 数据绑定实现；视图模型与窗口注册到阶段 0 已建立的宿主 DI 容器。

**理由**：

1. `tech-stack.md` 2.2 节已将 `CommunityToolkit.Mvvm` 定为既定选型，其源生成器（`ObservableProperty` / `RelayCommand`）零反射开销，符合「轻量」定位。
2. 阶段 0 已搭好 `Microsoft.Extensions.Hosting` 宿主与 DI 容器（`App.xaml.cs` 中 `builder.Services.AddSingleton<MainWindow>()`）。本阶段只需按同一模式追加注册，边际成本极低。
3. 阶段 4 的 `FenceViewModel` 将直接沿用本阶段确立的绑定与命令范式；若阶段 1 先用 code-behind，阶段 4 需重写一次 Demo 与绑定基础设施。

**注册约定**：

| 类型 | 生命周期 | 理由 |
|---|---|---|
| `LayeredWindowHostViewModel` | `Transient` | 每次打开 Demo 窗口获得独立状态，避免多次打开时状态串扰 |
| `LayeredWindowHostDemoWindow` | `Transient` | 与视图模型生命周期对齐 |

> 阶段 0 的 `MainWindow` 保持 `Singleton` 注册不变（本阶段不显示它，注册不调整）。

### 3.4 扩展样式的应用时机与幂等性

**决策**：扩展样式在 `SourceInitialized` 事件中**一次性**应用；`ShowInTaskbar` 一旦设定，**禁止在运行时切换**；对 `WS_EX_LAYERED` 采用**幂等**处理（已置位则不重复操作）；每次样式变更后调用 `SetWindowPos(… SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE)` 使样式即时生效。

**理由**：

1. **时机**：`SourceInitialized` 是 WPF 中 HWND 已创建、且尚未首次绘制的窗口期，此时调用 `SetWindowLongPtr` 不会与 WPF 的窗口初始化竞争。
2. **`ShowInTaskbar` 不可运行时切换**：WPF 的 `ShowInTaskbar` 在窗口已显示后变更会**销毁并重建 HWND**，已应用的扩展样式随旧句柄一并丢失，表现为「切换后任务栏按钮回来了、样式失效」。将该项约束为初始化期设定，可彻底规避该陷阱。
3. **`WS_EX_LAYERED` 的幂等**：WPF 在 `AllowsTransparency=true` 时**已自行设置** `WS_EX_LAYERED`（其透明渲染基于分层窗口）。若 Interop 层盲目再次置位虽通常无害，但会掩盖「样式由谁负责」的归属；采用「读—判断—按需写」可保证不覆盖 WPF 的内部状态，也使后续排查有明确依据。
4. **`SWP_FRAMECHANGED`**：仅修改 `GWL_EXSTYLE` 后，部分样式变更不会立即被系统应用；补发一次 `SetWindowPos` 并带 `SWP_FRAMECHANGED` 是业界通行做法，同时通过 `SWP_NOACTIVATE` 保证该调用本身不激活窗口。

### 3.5 防激活策略：`WS_EX_NOACTIVATE` + 拦截 `WM_MOUSEACTIVATE`

**决策**：双重保障——静态样式位 `WS_EX_NOACTIVATE` 与消息级拦截 `WM_MOUSEACTIVATE`（返回 `MA_NOACTIVATE`）同时启用。

**理由**：

1. `WS_EX_NOACTIVATE` 阻止窗口在点击时成为前台窗口，但**不阻止** WPF 内部对该消息的处理路径；显式拦截 `WM_MOUSEACTIVATE` 可保证返回值确定，避免不同 Windows 版本或 WPF 版本下行为漂移（`mission.md` 第 7 节「Win10 与 Win11 桌面层结构差异」的同类风险）。
2. 拦截通过 `HwndSource.AddHook` 实现，钩子在 `SourceInitialized` 时挂载、在窗口 `Closed` 时移除，避免句柄泄漏（对齐 `tech-stack.md` 2.5 节的泄漏排查要求）。

**已识别的代价**：

| 代价 | 说明与缓解 |
|---|---|
| 窗口无法通过点击获得键盘焦点 | 阶段 1 无文本输入需求；阶段 4 的标题栏编辑（`roadmap.md` 阶段 4 任务 5）需要焦点，届时须评估「编辑态临时撤销 `WS_EX_NOACTIVATE`」的策略，登记为遗留项（见 5.3） |

### 3.6 可见性切换的守护策略

**决策**：Demo 的第三个按钮切换 `Visibility`（`Visible ↔ Hidden`）；隐藏后启动一个 **3 秒守护定时器自动恢复为 `Visible`**，并在界面状态文本中说明该行为。

**理由**：

1. `roadmap.md` 阶段 1 任务 5 明确要求验证「可见性」切换，故须真实操作 `Visibility` 而非用透明度近似。
2. 阶段 1 尚无托盘图标与全局热键（属阶段 9）。若 Demo 窗口隐藏后无任何恢复入口，验证者会被锁在「窗口不可见且无法唤回」的状态，只能结束进程——这会使该验证项**不可复现**，违反 `validation.md` 的「可复现」原则。
3. 守护定时器是**验证期权宜手段**，在阶段 9 托盘 / 热键到位后应移除；已在 5.3 节登记。

### 3.7 不引入自动化行为测试

**决策**：本阶段**不新增测试项目**，不编写针对窗口行为的自动化测试。自动化验证仅覆盖构建、架构引用与静态分析；窗口行为通过 `validation.md` 第 3 节的人工验收清单验证。

**理由**：

1. `roadmap.md` 阶段 1 的四条验收标准全部是**可观测的窗口行为**（无边框、无任务栏按钮、点击穿透、不失焦），其判定依赖真实桌面合成与输入路由，单元测试无法覆盖。
2. `tech-stack.md` 2.5 节已明确「UI 自动化测试暂不引入（人工验收清单替代）」，理由是 UI 自动化对分层窗口支持差、投入产出比低。
3. 本阶段唯一的纯逻辑是扩展样式的按位组合 / 剥离，属单行运算；为其单独新建 `tests/qDesktop.Interop.Tests` 项目的结构性成本高于收益。若后续阶段（阶段 2 的适配器分派、阶段 10 的触发带判定）出现成规模的可测逻辑，再一并决定是否建立 Interop 测试项目（登记为遗留项，见 5.3）。

### 3.8 沿用 `tech-stack.md` 的既定选型（不重复论证）

| 项目 | 选型 | 依据 |
|---|---|---|
| 语言 / 运行时 | C# 12 / .NET 8 | `tech-stack.md` 2.1 |
| UI 框架 | WPF | `tech-stack.md` 2.2 |
| MVVM | CommunityToolkit.Mvvm 8.x | `tech-stack.md` 2.2 |
| DI / 宿主 | Microsoft.Extensions.DependencyInjection / Hosting | `tech-stack.md` 2.2 |
| 窗口样式 API | `WS_EX_LAYERED` / `WS_EX_TRANSPARENT` / `WS_EX_TOOLWINDOW` / `WS_EX_NOACTIVATE` | `tech-stack.md` 2.3 |
| 窗口定位 / 置顶 | `SetWindowPos`、`SetWindowLongPtr` | `tech-stack.md` 2.3 |
| 静态分析 | .NET Analyzer + `TreatWarningsAsErrors` | `tech-stack.md` 2.5 |
| 平台架构 | `win-x64`（首版唯一） | `tech-stack.md` 2.1 |

---

## 4. 架构约束（编译期强制）

阶段 1 **不新增程序集**，引用方向与阶段 0 保持一致：

```
qDesktop.App ──> qDesktop.Core
qDesktop.App ──> qDesktop.Interop
qDesktop.Core ──✗ qDesktop.Interop
qDesktop.Core ──✗ 任何 WPF 程序集
tests/qDesktop.Core.Tests ──> qDesktop.Core
```

| 项目 | 本阶段职责变化 |
|---|---|
| `src/qDesktop.App` | 新增 `LayeredWindowHost`、视图模型与 Demo 窗口；启动路径改为显示 Demo 窗口；DI 追加两个 `Transient` 注册 |
| `src/qDesktop.Interop` | 新增窗口样式 P/Invoke、常量与扩展样式辅助 |
| `src/qDesktop.Core` | **无改动** |
| `tests/qDesktop.Core.Tests` | **无改动** |

> 关键纪律：**任何 `DllImport` 不得出现在 `qDesktop.App` 内**。`App` 只调用 `Interop` 暴露的托管封装，不直接 `DllImport`。

---

## 5. 约束与假设

### 5.1 硬约束

1. `dotnet build` 必须零警告零错误（`TreatWarningsAsErrors=true`）。
2. 所有 P/Invoke 声明必须位于 `qDesktop.Interop`；`qDesktop.App` 内不得出现 `DllImport`。
3. `qDesktop.Core` 不得被本阶段修改，且不得引用 WPF 或 Interop。
4. 窗口须在 Windows 10 1903+ 与 Windows 11 上表现一致（本阶段验收方式见 3.1 与 `validation.md` V6）。
5. `ShowInTaskbar` 不得在窗口显示后于运行时切换（依据 3.4）。
6. 应用退出时不得残留进程，且 `HwndSource` 钩子须被移除。

### 5.2 环境假设

| 假设 | 当前实测值 | 影响 |
|---|---|---|
| 开发机为 Windows 且已安装可构建 `net8.0-windows` 的 SDK | ✅ win32 / .NET SDK 10.0.401（沿用阶段 0） | 可本地执行窗口验证 |
| 开发机仅安装一个 Windows 版本 | ⚠️ 双版本覆盖受限 | V6 采用「本机实测 + 另一版本待补」 |
| 桌面合成（DWM）处于启用状态 | ✅ Windows 10/11 默认启用 | 分层窗口与透明渲染可用 |

### 5.3 开放问题（待后续阶段解决）

| 问题 | 建议解决时点 |
|---|---|
| 双版本（Win10 / Win11）中未覆盖版本的实测补验 | 阶段 2 桌面层探测时一并完成（阶段 2 本就要求在双系统分别探测） |
| 标题栏编辑态如何临时获得键盘焦点（与 3.5 的 `WS_EX_NOACTIVATE` 冲突） | 阶段 4（首次出现文本输入） |
| 可见性守护定时器的移除 | 阶段 9（托盘图标与全局热键到位后） |
| Demo 窗口及其视图模型的删除 | 阶段 4 |
| 是否为 `qDesktop.Interop` 建立独立测试项目 | 阶段 2 或阶段 10（出现成规模可测逻辑时） |

---

## 6. 交付物清单

| 编号 | 交付物 | 形式 |
|---|---|---|
| D1 | `qDesktop.Interop` 窗口样式基础设施（P/Invoke + 常量 + 辅助） | 源码 |
| D2 | `LayeredWindowHost` 控件 | 源码 |
| D3 | `LayeredWindowHostViewModel`（MVVM） | 源码 |
| D4 | `LayeredWindowHostDemoWindow`（三按钮验证窗口） | 源码 |
| D5 | 宿主 DI 注册更新与启动路径切换 | 源码 |
| D6 | 窗口行为验证记录（见 `validation.md` 附录 A） | 验证记录 |
| D7 | 待评审的 PR（含三份文档链接与验收结论） | 交付流程 |

---

*本文档为阶段 1 需求的权威来源。范围或决策变更须先修改本文档，再同步 `./plan.md` 与 `./validation.md`。*
