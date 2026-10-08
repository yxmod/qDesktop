# qDesktop — 技术栈清单（Tech Stack）

> 文档版本：v1.1
> 最后更新：2026-10-08
> 状态：已确认（主干选型、支持平台、许可证、外观设置、托盘交互与分发策略均经干系人确认）

---

## 1. 选型总则

四条约束决定全部技术选择：

1. **必须能直接操作 Windows 桌面窗口层**（Progman / WorkerW / SysListView32），因此需要成熟的 Win32 互操作能力。
2. **必须支持异形透明、无边框、可穿透的置顶窗口**，用于栅栏渲染与边缘隐藏动画。
3. **必须保持轻量**（空闲内存 ≤ 150 MB），排除高运行时开销的方案。
4. **必须限定在已确认的平台范围内**：Windows 10 1903+ 与 Windows 11，首版仅 x64 架构。

结论：**C# / .NET 8 + WPF** 是唯一同时满足上述四条的成熟组合。WPF 提供硬件加速的分层渲染与完善的 `WS_EX_LAYERED` / `WS_EX_TRANSPARENT` 支持，C# 通过 P/Invoke 可直接调用全部所需 Win32 API，且 .NET 8 的 AOT/自包含发布可将体积与启动开销压到可接受范围。

---

## 2. 技术栈清单

### 2.1 运行时与语言

| 项目 | 选型 | 说明 |
|---|---|---|
| 语言 | C# 12 | 使用 `nullable`、`record`、模式匹配等现代特性 |
| 运行时 | .NET 8（LTS） | 长期支持版本，避免使用非 LTS 预览特性 |
| 目标框架 | `net8.0-windows` | 需 `UseWPF=true`、`AllowUnsafeBlocks=true` |
| 支持系统 | Windows 10 1903+ / Windows 11 | 两代系统桌面层结构不同，适配层须按版本分派（见 2.3） |
| 平台架构 | `win-x64`（首版唯一目标） | 不构建 x86 / arm64；ARM64 视后续需求再评估 |
| 发布方式 | 自包含 + ReadyToRun（默认）；评估 NativeAOT（WPF 不支持，故排除） | 自包含避免用户需预装运行时 |

### 2.2 UI 框架与架构

| 项目 | 选型 | 说明 |
|---|---|---|
| UI 框架 | WPF（.NET 8 内置） | 提供分层窗口、硬件加速渲染、成熟的自定义控件能力 |
| MVVM 框架 | CommunityToolkit.Mvvm 8.x | 源生成器实现 `ObservableProperty` / `RelayCommand`，零反射开销 |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | 与 Host 模型配合，统一管理生命周期 |
| 应用宿主 | Microsoft.Extensions.Hosting | 承载 DI、配置、日志、后台服务（文件监听、图标缓存） |
| 配置管理 | Microsoft.Extensions.Configuration + JSON 提供程序 | 应用级配置；用户布局数据另走独立存储（见 2.6） |
| 主题 / 样式 | 自建 ResourceDictionary 设计令牌（Design Token）系统 | 支持用户配置栅栏背景透明度（0–100%）与背景色、边框色、标题色、文本色；不引入重型 UI 库，保证渲染可控与体积 |
| 颜色编辑控件 | 自建颜色选择器（取色器 + 最近使用色 + 十六进制输入）+ 透明度滑块 | 避免为单个控件引入第三方 UI 库，并保证与主题令牌体系一致 |
| 图表 / 动画 | WPF 内建 `Storyboard` + `DoubleAnimation`；必要时用 `CompositionTarget.Rendering` 手写插值 | 边缘隐藏动画需精确控制时长与帧率 |

### 2.3 Windows 互操作层（本项目技术核心）

| 能力 | 技术手段 | 关键 API |
|---|---|---|
| 桌面窗口层定位 | P/Invoke + `EnumWindows` 枚举 | `FindWindow("Progman")`、`FindWindowEx`、`SendMessageTimeout`（`0x052C` 触发 WorkerW 生成） |
| 桌面图标列表句柄 | 层级遍历定位 | `SHELLDLL_DefView` → `SysListView32`（`LVM_GETITEMCOUNT` 等） |
| 桌面图标读取（不侵入） | 优先走 Shell API 读取桌面文件夹，避免跨进程 `LVM_GETITEM` 的内存注入风险 | `SHGetKnownFolderPath(FOLDERID_Desktop)`、`IShellFolder`、`SHGetFileInfo` |
| 窗口样式 | 分层、穿透、工具窗口、无激活 | `WS_EX_LAYERED`、`WS_EX_TRANSPARENT`、`WS_EX_TOOLWINDOW`、`WS_EX_NOACTIVATE` |
| 窗口定位 / 置顶 | 精确 Z 序与坐标控制 | `SetWindowPos`、`SetWindowLongPtr`、`DwmGetWindowAttribute` |
| 全局鼠标钩子 | 底层鼠标钩子（边缘触发） | `SetWindowsHookEx(WH_MOUSE_LL)` + `GetMessage` 消息泵 |
| 多屏 / DPI | 显示器枚举与感知模式 | `EnumDisplayMonitors`、`GetDpiForMonitor`、`SetProcessDpiAwarenessContext`（Per-Monitor V2） |
| 开机自启 | 用户级注册表项 | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| 全局热键 | 注册系统热键 | `RegisterHotKey` / `UnregisterHotKey` + `WM_HOTKEY` |
| 图标提取与缓存 | Shell 图标接口 + 位图转换 | `SHGetFileInfo`、`IImageList`、`System.Windows.Interop.Imaging` |
| 文件系统监听 | .NET 内建 | `FileSystemWatcher`（文件夹映射栅栏） |
| 单实例控制 | 命名互斥体 + 进程间通知 | `Mutex`（Global 命名）+ 命名管道 |
| 托盘图标 | WPF 原生不支持，使用 WinForms 互操作 | `System.Windows.Forms.NotifyIcon`（`UseWindowsForms=true`）；**左键单击**切换全部栅栏的唤起 / 隐藏（`MouseClick` + `MouseButtons.Left`），右键弹出菜单，二者互不干扰 |

> **架构约束**：所有 P/Invoke 声明集中在 `qDesktop.Interop` 程序集内，禁止在业务层直接调用。桌面层适配必须通过 `IDesktopHostAdapter` 接口隔离，以支持 Windows 版本变化时的替换与降级。

### 2.4 数据与持久化

| 项目 | 选型 | 说明 |
|---|---|---|
| 布局数据格式 | JSON（`System.Text.Json`） | 明文可读、可手工修复、便于开源用户排查 |
| 存储位置 | `%APPDATA%\qDesktop\` | 布局文件、日志、缓存分离存放 |
| 布局文件 | `layout.json`（栅栏树 + 归属关系） | 采用带 `schemaVersion` 的版本化结构，预留迁移能力 |
| 便签内容 | 独立 `.md` / `.txt` 文件，布局仅存路径引用 | 保证用户文本可直接用外部编辑器打开 |
| 图标缓存 | 磁盘缓存（PNG）+ 内存 LRU | 避免每次启动重新提取全部图标 |
| 数据库 | **不引入**（首版不使用 SQLite） | 数据规模小（数百条记录），JSON 足够；降低依赖与体积 |
| 备份策略 | 写入前保留 `layout.json.bak` 上一版本 | 防止崩溃导致布局全损 |

### 2.5 质量保障

| 项目 | 选型 | 说明 |
|---|---|---|
| 单元测试 | xUnit + FluentAssertions | 覆盖数据模型、层级运算、布局序列化、坐标换算 |
| Mock 框架 | NSubstitute | 用于隔离 Win32 适配层 |
| UI 自动化测试 | 暂不引入（人工验收清单替代） | UI 自动化对分层窗口支持差，投入产出比低 |
| 静态分析 | .NET 内置 Analyzer + `TreatWarningsAsErrors` | 保持零警告 |
| 代码风格 | `.editorconfig` + `dotnet format` | 统一格式，CI 中校验 |
| 内存 / 泄漏检查 | dotnet-counters + 手动长时间运行测试 | 重点排查钩子与图标句柄泄漏 |

### 2.6 工程与交付

| 项目 | 选型 | 说明 |
|---|---|---|
| 版本控制 | Git | 仓库根目录含 `specs/`、`src/`、`tests/`、`docs/` |
| 分支模型 | `main` 保护 + 特性分支 | 阶段交付以 PR 合入 |
| CI | GitHub Actions（`windows-latest`） | 构建 + 测试 + 格式校验 |
| 安装包 | Inno Setup 6 | 生成 `win-x64` 安装程序；首版**不做代码签名**，理由见 5.1 |
| 免安装版 | 自包含 zip（`win-x64`） | 绿色版，解压即用 |
| 分发渠道 | GitHub Release + winget / Scoop 清单 | 经包管理器安装可显著降低 SmartScreen 告警对用户的影响 |
| 代码签名 | 首版**不启用**，CI 中预留可开启的签名步骤 | 待用户量或赞助到位后再采购证书，详见 5.1 |
| 自动更新 | 首版**不引入**，Release 页面手动下载 | 避免引入联网依赖，与「本地优先」定位一致 |
| 崩溃收集 | 本地日志（Serilog）+ 用户手动导出 | 不做自动上报，符合无遥测承诺 |
| 许可证 | **MIT** | 已确认；仓库根目录放置 `LICENSE` 文件 |

### 2.7 日志与诊断

| 项目 | 选型 | 说明 |
|---|---|---|
| 日志框架 | Serilog + 文件 Sink | 滚动文件，默认保留 7 天 |
| 日志级别 | Release 默认 `Information`，可配置切换 `Debug` | 桌面层适配失败等关键路径必须留痕 |
| 诊断入口 | 托盘菜单「导出诊断日志」 | 便于开源用户提交 issue |

---

## 3. 明确排除的技术（及理由）

| 排除项 | 理由 |
|---|---|
| Electron / Tauri | 无法可靠嵌入桌面图标层，异形透明窗口与全局钩子支持不足或成本过高 |
| WinUI 3 | 桌面层嵌入与无边框透明窗口限制较多，边缘隐藏实现成本显著高于 WPF |
| UWP | 沙箱模型禁止所需的 Win32 全局钩子与桌面窗口操作 |
| SQLite / LiteDB | 数据量小，JSON 已足够，引入数据库只增加体积与迁移负担 |
| 第三方 UI 组件库（MahApps / HandyControl 等） | 主题与渲染需完全可控，避免为少数控件引入大体积依赖 |
| 跨平台抽象层（Avalonia / MAUI） | 本项目强绑定 Windows 桌面窗口模型，抽象层只增加复杂度 |

---

## 4. 关键技术风险与验证顺序

| 风险 | 验证阶段 | 验证方式 |
|---|---|---|
| 能否稳定嵌入/贴合桌面图标层 | 阶段 2 | 编写最小 Demo，在 Win10 / Win11 上分别验证 Progman / WorkerW 定位 |
| 分层窗口 + 穿透 + 置顶能否共存 | 阶段 1、阶段 4 | 最小窗口 Demo，验证点击穿透与 Z 序稳定 |
| 全局鼠标钩子能否满足 ≤150 ms 触发延迟 | 阶段 10 | 钩子 Demo 计时测量，确认无超时摘除 |
| 图标提取的性能与内存占用 | 阶段 6、阶段 16 | 200 图标场景下的启动耗时与内存基线测量 |
| 多屏 / 高 DPI 下坐标正确性 | 阶段 14 | 100% / 150% / 200% 缩放 + 双屏组合矩阵测试 |
| Win10 1903+ 与 Win11 桌面层结构差异 | 阶段 2 | 在两个系统版本上分别执行探测，验证适配层按版本分派（见 2.3） |
| 托盘图标单击切换的状态一致性 | 阶段 9 | 连续单击 20 次并校验最终可见性状态与单击次数奇偶一致 |

> 上述七项属于「先验证、后投入」的技术前提。任一验证失败须回到本文件更新选型，而非在后续阶段硬扛。

---

## 5. 已确认的关键决策

| 决策项 | 结论 | 影响 |
|---|---|---|
| 开源许可证 | **MIT** | 最宽松许可，允许自由使用、修改、再分发；仓库根目录放置 `LICENSE`，版权人与年份须完整 |
| 支持的系统 | **Windows 10 1903+ / Windows 11** | 桌面层适配须覆盖两个系统版本，适配层按版本分派（见 2.3 与第 4 节） |
| 平台架构 | **首版仅 x64** | 不构建 x86 / arm64；ARM64 视后续需求再评估 |
| 外观设置 | **支持透明度与颜色配置** | 栅栏背景透明度 0–100% 可调，背景色、边框色、标题色、文本色可自定义（见 2.2） |
| 托盘交互 | **单击托盘图标唤起 / 隐藏全部栅栏** | 左键单击绑定切换、右键绑定菜单（见 2.3） |
| 安装包代码签名 | **首版不购买证书** | 通过分发渠道与文档缓解 SmartScreen 告警，理由见 5.1 |

### 5.1 关于安装包代码签名（推荐方案与理由）

**推荐：首版不购买代码签名证书，改用「分发渠道 + 文档说明」缓解 SmartScreen 告警。**

理由如下：

1. **成本与收益不匹配**：OV 代码签名证书年费约 ¥1,000–3,000，EV 证书更高。本项目为轻量开源项目，无营收来源；且签名并非一劳永逸——新证书需累积下载信誉后才逐步减少告警。
2. **存在成本更低的等效缓解手段**：将安装包提交至 **winget** 与 **Scoop** 包管理器后，用户经包管理器安装不触发 SmartScreen；同时在安装文档中明确给出「更多信息」→「仍要运行」的操作路径。
3. **签名可随时追加**：签名属于发布环节的独立动作，不影响任何代码结构。CI 中预留签名步骤（默认关闭、密钥以 Secrets 占位），后续采购证书后只需填入密钥即可启用。
4. **开源场景下信任来源不同**：用户可直接审计源码并自行构建，代码签名所提供的「发布者身份背书」价值低于闭源软件。

**触发重新评估的条件**（满足任一即应采购证书）：

- 安装包下载量显著上升，SmartScreen 告警成为主要用户反馈问题；
- 项目获得赞助或有商业化意向；
- 需要上架 Microsoft Store（该渠道要求签名）。

### 5.2 遗留待决项

| 待决项 | 说明 | 建议确认时点 |
|---|---|---|
| ARM64 支持时机 | 首版仅 x64，是否在 v1.x 追加 ARM64 构建 | 进入 M4 前 |
| 首个 Release 版本号与日期 | 影响 CHANGELOG 与 Release 说明的编写 | 阶段 17 结束前 |
| 贡献者许可协议（CLA） | MIT 项目通常不强制 CLA，需确认是否要求 | 阶段 18 前 |

---

*本文件为技术选型的唯一权威来源。任何新增依赖须在此登记，并说明其是否满足第 1 节的三条约束。*
