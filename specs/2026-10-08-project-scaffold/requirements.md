# 阶段 0 · 项目脚手架与工程规范 — 需求文档（Requirements）

> 所属分支：`feature/phase-0-project-scaffold`
> 对应路线图阶段：`specs/roadmap.md` 阶段 0
> 关联文档：`./plan.md`（开发计划）、`./validation.md`（验证标准）
> 编写依据：`specs/mission.md`、`specs/tech-stack.md`
> 文档版本：v1.0 ｜ 创建日期：2026-10-08 ｜ 状态：待确认

---

## 1. 业务背景

### 1.1 项目定位

qDesktop 的目标是为 Windows 用户提供**轻量、开源、可自由组织**的桌面图标管理工具，把混乱的系统桌面转化为可分层、可收纳、可自动隐藏的个人工作空间（见 `mission.md` 第 1 节）。其核心差异化能力为**自由层级**与**边缘隐藏**。

### 1.2 本阶段为何存在

阶段 0 是全部 19 个阶段的**唯一公共地基**。路线图 0.1 节确立的拆分原则要求「每阶段可独立验证」，而后续 18 个阶段均需在统一的解决方案结构、构建配置、日志与 CI 之上推进。若脚手架阶段缺失或约定不统一，将导致：

| 风险 | 后果 |
|---|---|
| 项目边界不清 | 业务层直接调用 P/Invoke，破坏 `tech-stack.md` 2.3 节的架构约束 |
| 构建配置缺失 | 后续阶段零散添加依赖与警告抑制，警告噪声淹没真实缺陷 |
| 无 CI 门禁 | 阶段交付质量无法在合并前被机器校验，依赖人工检查 |
| 无日志设施 | 阶段 2（桌面层探测）、阶段 10（钩子健康检查）等关键路径的失败无法留痕排查 |

**结论**：阶段 0 不产出任何面向用户的可见功能，其价值在于**消除后续阶段的返工成本**。

### 1.3 业务价值

1. 为 19 个阶段提供统一落点，使每个阶段可独立构建、独立验证、独立合并。
2. 用机器校验（构建 + 测试 + 格式）替代人工检查，降低单人/小规模开发的维护负担。
3. 通过架构约束（引用方向）在编译期拦截越界调用，保护 `qDesktop.Interop` 的隔离设计。

---

## 2. 功能范围

### 2.1 范围内（In Scope）

| 编号 | 范围项 | 说明 |
|---|---|---|
| S1 | 解决方案与项目结构 | 建立 `qDesktop.sln` 及 4 个子项目，明确引用方向 |
| S2 | 目标框架与构建属性 | `net8.0-windows`、`UseWPF`、`UseWindowsForms`、`AllowUnsafeBlocks`、`win-x64` |
| S3 | 统一构建配置 | `Directory.Build.props`、`.editorconfig`、`.gitignore` |
| S4 | 应用宿主与依赖注入 | `Microsoft.Extensions.Hosting` + DI 容器装配 |
| S5 | 结构化日志 | Serilog 文件 Sink，输出至 `%APPDATA%\qDesktop\logs\` |
| S6 | 空白 WPF 宿主 | 可启动、可显示空窗口、关闭即退出的最小应用 |
| S7 | 单元测试骨架 | `xUnit` + `FluentAssertions` + `NSubstitute`，含一个可通过的样例测试 |
| S8 | 覆盖率采集 | `coverlet` 采集 `qDesktop.Core` 覆盖率并输出报告（仅报告，不设阈值） |
| S9 | CI 流水线 | GitHub Actions：`restore → build → test → dotnet format --verify-no-changes` |
| S10 | 工程约定落地 | 分支命名与提交信息规范写入 `.editorconfig` / 文档 |

### 2.2 范围外（Out of Scope）

以下内容**明确不在阶段 0 内**，避免范围蔓延：

- **不做** 任何栅栏渲染、图标读取、边缘隐藏、托盘、设置面板等业务功能（属阶段 1 及以后）。
- **不做** 桌面层适配层（`IDesktopHostAdapter`）实现——阶段 0 仅创建 `qDesktop.Interop` 空程序集占位。
- **不做** 打包、安装包、绿色版与签名步骤（属阶段 17）。
- **不做** 多显示器与 DPI 感知模式设置（属阶段 14）。
- **不做** UI 自动化测试（`tech-stack.md` 2.5 节已明确以人工验收清单替代）。
- **不做** 引入任何第三方 UI 组件库（`tech-stack.md` 第 3 节排除）。
- **不做** 数据库、自动更新、遥测上报（`mission.md` 5.2 节范围外）。
- **不引入** `global.json` 锁定 SDK（决策见 3.1）。

---

## 3. 技术决策

### 3.1 SDK 版本策略：保留 `net8.0` 目标，不锁定 SDK

**决策**：目标框架保持 `net8.0-windows`（对齐 `tech-stack.md` 2.1 节的 .NET 8 LTS 选型）；**不添加 `global.json`**，本地构建使用开发机已安装的 SDK（当前为 .NET SDK **10.0.401**）。

**理由**：

1. `tech-stack.md` 的 .NET 8 选型理由是「LTS 与运行时稳定性」，而非特定 SDK 工具链版本；目标框架不变即可满足该约束。
2. .NET 10 SDK 具备构建 `net8.0-windows` 目标的能力（通过目标包），无需在开发机额外安装 SDK。
3. 避免 `global.json` 在团队/CI 环境不一致时直接导致构建失败（`global.json` 的 SDK 解析失败为硬失败）。

**已识别的代价与缓解**：

| 代价 | 缓解措施 |
|---|---|
| 本地 SDK 与 CI SDK 可能不一致，存在「本地绿、CI 红」的漂移风险 | CI 中通过 `actions/setup-dotnet` 固定 `8.0.x`，保证流水线可复现；本地不锁 |
| 后续若升级 SDK 导致分析器行为变化，可能产生新警告 | `TreatWarningsAsErrors=true` 会在构建期立即暴露，而非静默通过 |
| 目标框架升级（如 v1.x 升 .NET 10）需人工同步文档 | 在 `tech-stack.md` 2.1 节登记目标框架为唯一权威来源，变更须先改文档 |

> **说明**：本地不锁 SDK 与 CI 固定 `8.0.x` 并不矛盾——前者保证开发机无需额外安装，后者保证流水线输出可复现。CI 固定版本不通过 `global.json` 实现，因此不违反本决策。

### 3.2 CI 触发范围：PR 与 push 到 `main` 均触发

**决策**：工作流在 `pull_request`（面向 `main`）与 `push`（`main` 分支）两个事件上均触发。

**理由**：

1. 分支模型为「`main` 保护 + 特性分支」（`tech-stack.md` 2.6 节）。PR 触发用于**合并前门禁**；push 触发用于保证 `main` 主干始终处于可构建状态。
2. 若仅 PR 触发，直接推送或 PR 合入后的主干状态将无机器校验，一旦合入引入缺陷，后续阶段的基线不可信。
3. 单人/小规模开发场景下，push 触发的额外开销可接受（阶段 0 流水线耗时短）。

### 3.3 覆盖率策略：接入采集，仅报告不阻断

**决策**：接入 `coverlet` 采集 `qDesktop.Core` 的覆盖率并生成报告，但**不在 CI 中设置覆盖率阈值门禁**。

**理由**：

1. 阶段 0 的 `qDesktop.Core` 为**空程序集**（业务模型属阶段 3），此时设置 60% 阈值会导致流水线立即失败，属无意义阻断。
2. `mission.md` 6.2 节与 `roadmap.md` 阶段 3 要求 `qDesktop.Core` 行覆盖率 ≥ 60%，该阈值的**生效时点应为阶段 3**（首次出现业务代码时）。
3. 阶段 0 的目标是**建立测量基线**，确认覆盖率采集链路本身可用；阈值门禁推迟到阶段 3 引入。

> **后续动作（非本阶段交付）**：阶段 3 结束前须在 CI 中启用 `qDesktop.Core` 覆盖率阈值门禁。

### 3.4 工程约定：Conventional Commits + `feature/*` 分支

**决策**：

- **分支命名**：特性分支统一为 `feature/<阶段号>-<短名>`，例如 `feature/phase-0-project-scaffold`。
- **提交信息**：遵循 Conventional Commits，使用 `feat` / `fix` / `chore` / `docs` / `refactor` / `test` / `ci` / `build` / `perf` / `style` 前缀。
- **格式校验**：由 `dotnet format --verify-no-changes` 在 CI 中强制；提交信息规范由人工与评审保证。

**理由**：

1. `tech-stack.md` 2.6 节要求「阶段交付以 PR 合入」，规范化命名使 PR 与路线图阶段一一对应，便于追溯。
2. 统一前缀为阶段 18 自动生成 CHANGELOG 提供结构化输入（`roadmap.md` 阶段 18 任务 5）。
3. 提交信息格式不做工具强制（不引入 commitlint / husky 等额外依赖），符合「轻量」定位。

### 3.5 沿用 `tech-stack.md` 的既定选型（不重复论证）

| 项目 | 选型 | 依据 |
|---|---|---|
| 语言 | C# 12 | `tech-stack.md` 2.1 |
| UI 框架 | WPF（.NET 8 内置） | `tech-stack.md` 2.2 |
| MVVM | CommunityToolkit.Mvvm 8.x | `tech-stack.md` 2.2 |
| DI / 宿主 | Microsoft.Extensions.DependencyInjection / Hosting | `tech-stack.md` 2.2 |
| 配置 | Microsoft.Extensions.Configuration + JSON | `tech-stack.md` 2.2 |
| 日志 | Serilog + 文件 Sink | `tech-stack.md` 2.7 |
| 测试 | xUnit + FluentAssertions + NSubstitute | `tech-stack.md` 2.5 |
| 静态分析 | .NET Analyzer + `TreatWarningsAsErrors` | `tech-stack.md` 2.5 |
| 平台架构 | `win-x64`（首版唯一） | `tech-stack.md` 2.1 |
| 许可证 | MIT | `tech-stack.md` 2.6 |

---

## 4. 架构约束（编译期强制）

引用方向**必须**满足以下三条，且由项目引用（而非人工约定）保证：

```
qDesktop.App ──> qDesktop.Core
qDesktop.App ──> qDesktop.Interop
qDesktop.Core ──✗ qDesktop.Interop
qDesktop.Core ──✗ 任何 WPF 程序集
tests/qDesktop.Core.Tests ──> qDesktop.Core
```

| 项目 | 职责 | 允许的依赖 |
|---|---|---|
| `src/qDesktop.App` | WPF 宿主、视图、视图模型、DI 装配 | Core、Interop |
| `src/qDesktop.Core` | 领域模型与业务逻辑，**无 UI 依赖** | 仅 BCL |
| `src/qDesktop.Interop` | 全部 P/Invoke 与 Win32 声明 | 仅 BCL |
| `tests/qDesktop.Core.Tests` | Core 单元测试 | Core、测试框架 |

> 约束来源：`tech-stack.md` 2.3 节「所有 P/Invoke 声明集中在 `qDesktop.Interop` 程序集内，禁止在业务层直接调用」。

---

## 5. 约束与假设

### 5.1 硬约束

1. `dotnet build` 必须零警告零错误（`TreatWarningsAsErrors=true`）。
2. 解决方案中不得出现 `x86` / `arm64` 目标配置。
3. `qDesktop.Core` 不得引用 WPF 或 Interop 程序集。
4. 应用须可在 Windows 10 1903+ 与 Windows 11 上启动。
5. 日志文件须写入 `%APPDATA%\qDesktop\logs\`，不得写入程序目录。

### 5.2 环境假设

| 假设 | 当前实测值 | 影响 |
|---|---|---|
| 开发机已安装可构建 `net8.0-windows` 的 SDK | ✅ .NET SDK 10.0.401 | 决策 3.1 成立 |
| 仓库将托管至 GitHub | ⚠️ 当前**未配置 git remote** | CI 工作流先落盘，待推送后生效 |
| 开发机为 Windows | ✅ win32 | 可本地执行 WPF 构建 |

### 5.3 开放问题（待后续阶段解决）

| 问题 | 建议解决时点 |
|---|---|
| GitHub 仓库地址与远端配置 | 阶段 0 提交后、CI 首次运行前 |
| 覆盖率阈值门禁的启用 | 阶段 3 |
| ARM64 支持时机 | 进入 M4 前（`tech-stack.md` 5.2） |

---

## 6. 交付物清单

| 编号 | 交付物 | 形式 |
|---|---|---|
| D1 | `qDesktop.sln` + 4 个项目 | 源码 |
| D2 | `Directory.Build.props`、`.editorconfig`、`.gitignore` | 配置 |
| D3 | 应用宿主装配（DI + 配置 + 日志） | 源码 |
| D4 | 空白 WPF 主窗口，可启动 | 源码 |
| D5 | 单元测试骨架与样例测试 | 源码 |
| D6 | `.github/workflows/ci.yml` | 配置 |
| D7 | 可运行的构建与日志证据 | 验证记录（见 `validation.md`） |

---

*本文档为阶段 0 需求的权威来源。范围或决策变更须先修改本文档，再同步 `./plan.md` 与 `./validation.md`。*
