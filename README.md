# qDesktop

Windows 桌面图标管理工具：以**栅栏（收纳盒）**组织桌面，支持**便签**、**文件夹映射**、**自由层级**、**隐藏/显示**，以及 QQ 式的**边缘隐藏**（鼠标划过即显）。

> 当前进度：**阶段 1（无边框透明窗口骨架）** 代码实现完成，自动化与静态验证通过；手工验收（窗口外观、穿透、防激活）待评审者在真实桌面会话执行。阶段划分见 [`specs/roadmap.md`](specs/roadmap.md)。

---

## 环境要求

| 项 | 要求 |
|---|---|
| 操作系统 | Windows 10 1903+ / Windows 11（首版仅 x64） |
| .NET SDK | 任意可构建 `net8.0-windows` 的版本（本机验证：10.0.401） |
| 目标框架 | `net8.0` / `net8.0-windows`（.NET 8 LTS） |

仓库**不含 `global.json`**，不锁定 SDK 版本：本地用已安装 SDK 构建，CI 通过 `setup-dotnet` 固定 `8.0.x` 以保证流水线可复现。决策依据见 [`specs/2026-10-08-project-scaffold/requirements.md`](specs/2026-10-08-project-scaffold/requirements.md) 3.1。

---

## 目录结构

```
qDesktop.sln                 解决方案
Directory.Build.props        全仓库统一构建配置（语言版本、警告即错误、元数据）
.editorconfig                代码风格与命名规则
.gitattributes               换行符策略（统一 LF）
src/
  qDesktop.App/              WPF 应用宿主：组合根、DI、日志、视图
  qDesktop.Core/             领域模型与业务逻辑（无 UI 依赖）
  qDesktop.Interop/          全部 Win32 P/Invoke 声明的唯一落点
tests/
  qDesktop.Core.Tests/       Core 单元测试
specs/                        项目章程、路线图、技术选型、各阶段规格
```

### 架构约束（编译期强制）

```
qDesktop.App        ──> qDesktop.Core
qDesktop.App        ──> qDesktop.Interop
qDesktop.Core       ──✗ qDesktop.Interop
qDesktop.Core       ──✗ 任何 WPF 程序集
qDesktop.Core.Tests ──> qDesktop.Core
```

`qDesktop.Core` 不得引用 `Interop` 或 WPF；该约束由项目引用关系保证，并有单元测试持续断言。

---

## 构建与运行

```bash
# 还原
dotnet restore qDesktop.sln

# 构建（Release；警告即错误，零警告方可通过）
dotnet build qDesktop.sln -c Release

# 运行（显示阶段 1 的窗口属性验证窗口）
dotnet run --project src/qDesktop.App
```

> 平台：首版仅 x64。构建时若指定 `-p:Platform=x86` 或 `-p:Platform=arm64` 将**直接失败**（平台白名单见 `Directory.Build.props`）。

### 阶段 1 · 窗口属性验证窗口

启动后显示一个 **440 × 240** 的半透明无边框窗口（`LayeredWindowHostDemoWindow`），
用于人工验证「无边框 + 半透明 + 始终置顶 + 无任务栏按钮 + 不抢焦点」五项属性能否共存。
窗口内含三个按钮与一行状态文本：

| 按钮 | 行为 |
|---|---|
| **切换穿透** | 开/关 `WS_EX_TRANSPARENT`：开启后鼠标点击直接落到下层窗口（如记事本），本窗口不再接收点击。**10 秒后自动关闭** |
| **切换置顶** | 开/关 `Topmost`：关闭后窗口会**主动沉到当前前台窗口之下**，可被其他窗口正常遮挡与覆盖，其他窗口也能被点击提升到最前；重新开启即恢复始终置顶。**10 秒后自动恢复置顶** |
| **切换可见性** | 隐藏/显示窗口。**3 秒后自动恢复显示** |

> **防锁死守护**：阶段 1 尚无托盘图标与全局热键，三个切换项各自都可能把验证者锁在无法操作窗口的状态（穿透后不可点击；隐藏后不可见；取消置顶后可能被完全遮挡）。因此每项在进入风险状态时都会启动倒计时守护，到期自动恢复，剩余秒数实时显示在状态文本中。该守护属验证期权宜手段，阶段 9 移除。
>
> 取消置顶后，点击本窗口**不会**把它带到最前（窗口不参与激活，若提到前台窗口之上会让前台窗口再也无法被点击提升）。要让它回到最前，请重新打开置顶，或等待守护自动恢复。

验证要点：窗口无标题栏与系统边框、任务栏无对应条目、点击本窗口时其他窗口不失焦。
窗口左上角的说明文字已标注「本窗口为阶段 1 临时验证产物，阶段 4 删除」。
验收清单见 [`specs/2026-10-09-layered-window-host/validation.md`](specs/2026-10-09-layered-window-host/validation.md)。

> 阶段 0 的空白窗口 `MainWindow` 仍保留注册，但已不在启动路径显示。

---

## 测试与覆盖率

```bash
# 运行测试（含覆盖率采集）
dotnet test qDesktop.sln -c Release \
  --collect:"XPlat Code Coverage" \
  --settings tests/qDesktop.Core.Tests/coverlet.runsettings

# 仅运行测试
dotnet test qDesktop.sln -c Release
```

- 覆盖率报告位置：`TestResults/<guid>/coverage.cobertura.xml`
- 采集范围限定为 `qDesktop.Core`（见 `tests/qDesktop.Core.Tests/coverlet.runsettings`）
- **阶段 0 仅采集、不设阈值门禁**；阈值（行覆盖率 ≥ 60%）于阶段 3 在 CI 中启用

---

## 代码格式

```bash
# 校验（CI 门禁；有偏差时返回非零退出码）
dotnet format qDesktop.sln --verify-no-changes

# 自动修复
dotnet format qDesktop.sln
```

换行符统一为 **LF**，由 `.editorconfig` 与 `.gitattributes` 双重固定，避免本地与 CI 格式校验结果漂移。

---

## 日志

- 位置：`%APPDATA%\qDesktop\logs\qDesktop-YYYYMMDD.log`
- 策略：按天滚动，保留 7 天
- 级别：默认 `Information`，可在 `src/qDesktop.App/appsettings.json` 中改为 `Debug`
- 降级：`%APPDATA%` 不可写时回落到 `%TEMP%\qDesktop\logs\`；两者均不可写则不写文件日志，应用仍可启动

日志**不会**写入程序输出目录。

---

## 工程约定

- **分支命名**：`feature/<阶段号>-<短名>`，例如 `feature/phase-0-project-scaffold`
- **提交信息**：Conventional Commits（`feat` / `fix` / `chore` / `docs` / `refactor` / `test` / `ci` / `build` / `perf` / `style`）
- **静态分析**：`TreatWarningsAsErrors=true`，分析器规则豁免须在 [`specs/tech-stack.md`](specs/tech-stack.md) 2.5.1 节登记

---

## 文档

| 文档 | 内容 |
|---|---|
| [`specs/mission.md`](specs/mission.md) | 使命、目标用户、价值主张、范围边界、成功标准 |
| [`specs/tech-stack.md`](specs/tech-stack.md) | 技术选型与已确认决策 |
| [`specs/roadmap.md`](specs/roadmap.md) | 19 个阶段的划分、依赖与验收标准 |
| [`specs/2026-10-08-project-scaffold/`](specs/2026-10-08-project-scaffold/) | 阶段 0 的需求 / 计划 / 验证标准 |
| [`specs/2026-10-09-layered-window-host/`](specs/2026-10-09-layered-window-host/) | 阶段 1 的需求 / 计划 / 验证标准 |

---

## 许可证

MIT（选型依据见 [`specs/tech-stack.md`](specs/tech-stack.md) 2.6；`LICENSE` 文件于阶段 18 补充）
