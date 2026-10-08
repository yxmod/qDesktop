# 阶段 0 · 项目脚手架与工程规范 — 开发计划（Plan）

> 所属分支：`feature/phase-0-project-scaffold`
> 关联需求：`./requirements.md`　关联验证：`./validation.md`
> 文档版本：v1.0 ｜ 创建日期：2026-10-08

---

## 0. 计划说明

### 0.1 分组原则

任务按**可独立验证的交付单元**分组，每个任务组（Task Group, TG）满足：

1. 组内任务可连续执行，不依赖后续组的产出；
2. 组结束时存在**可观测的产物**（文件、可执行命令输出、可启动的程序），而非「代码写完了」；
3. 组的完成判据可在 `./validation.md` 中找到对应的验证条目。

### 0.2 执行顺序与依赖

```
TG1 ──> TG2 ──> TG3 ──> TG4 ──> TG5 ──> TG6 ──> TG7
 └───────────────────────────────────────────────┘
          （TG2 起可并行推进，但 TG7 必须最后执行）
```

| 任务组 | 名称 | 前置依赖 | 预估 |
|---|---|---|---|
| TG1 | 解决方案与项目骨架 | — | 0.25 天 |
| TG2 | 构建配置与代码规范 | TG1 | 0.25 天 |
| TG3 | 应用宿主、DI 与日志 | TG2 | 0.25 天 |
| TG4 | 空白 WPF 宿主窗口 | TG3 | 0.1 天 |
| TG5 | 单元测试与覆盖率骨架 | TG2 | 0.15 天 |
| TG6 | CI 流水线 | TG5 | 0.25 天 |
| TG7 | 工程文档与提交 | TG1–TG6 | 0.1 天 |

**总预估**：约 1 天（对齐 `roadmap.md` 阶段 0 的 1 天预估）。

### 0.3 任务状态标记

沿用 `roadmap.md` 0.2 节：⬜ 未开始 ｜ 🟨 进行中 ｜ ✅ 已完成 ｜ ⛔ 被阻塞

---

## 任务组 1 · 解决方案与项目骨架 ⬜

**目标**：建立四个项目的物理结构与引用关系，使 `dotnet build` 能通过（即使无任何业务代码）。

### 任务

1. **1.1** 在仓库根目录创建解决方案 `qDesktop.sln`。
2. **1.2** 创建 `src/qDesktop.App`（WPF 应用宿主）：
   - `OutputType=WinExe`、`TargetFramework=net8.0-windows`、`UseWPF=true`、`UseWindowsForms=true`。
3. **1.3** 创建 `src/qDesktop.Core`（类库）：
   - `TargetFramework=net8.0`（不启用 WPF），`AllowUnsafeBlocks=true`（为后续可能的位运算预留，与 tech-stack 一致）。
4. **1.4** 创建 `src/qDesktop.Interop`（类库）：
   - `TargetFramework=net8.0-windows`，`AllowUnsafeBlocks=true`（P/Invoke 必需）。
5. **1.5** 创建 `tests/qDesktop.Core.Tests`（xUnit 测试项目）：
   - `TargetFramework=net8.0-windows`。
6. **1.6** 建立引用关系（仅允许以下四条）：
   - `App → Core`
   - `App → Interop`
   - `Core.Tests → Core`
   - **不建立** `Core → Interop`、`Core → WPF` 任何引用。
7. **1.7** 在每个项目内放置一个占位类型（如 `AssemblyMarker`），保证空程序集可被引用。

### 交付物

- `qDesktop.sln`
- `src/qDesktop.App/`、`src/qDesktop.Core/`、`src/qDesktop.Interop/`、`tests/qDesktop.Core.Tests/`

### 完成判据

- `dotnet build qDesktop.sln` 成功，输出 `win-x64` 目标。
- 解决方案中不存在 `x86` / `arm64` 配置（`dotnet build -p:Platform=x86` 应无对应配置可解析）。

**验证对照**：`validation.md` V1.1、V1.2、V1.3

---

## 任务组 2 · 构建配置与代码规范 ⬜

**目标**：把警告级别、格式规范、版本号统一收敛到单一入口，避免后续阶段零散配置。

### 任务

1. **2.1** 创建 `Directory.Build.props`（仓库根）：
   - `LangVersion=12`、`Nullable=enable`、`ImplicitUsings=enable`
   - `TreatWarningsAsErrors=true`
   - `AnalysisLevel=latest-recommended`
   - `RuntimeIdentifier=win-x64`（仅 App 项目实际使用，其余项目通过条件属性控制）
   - 统一 `Version`、`Authors`、`Company` 等元数据占位
2. **2.2** 创建 `.editorconfig`：
   - 采用 file-scoped namespace、4 空格缩进、`var` 偏好、命名规则（私有字段 `_camelCase`）。
   - 与 `dotnet format` 兼容，确保 `--verify-no-changes` 可通过。
3. **2.3** 创建 `.gitignore`：
   - 覆盖 `bin/`、`obj/`、`.vs/`、`*.user`、`TestResults/`、`artifacts/`。
4. **2.4** **不创建** `global.json`（决策依据：`requirements.md` 3.1）。
5. **2.5** 执行 `dotnet format` 一次，使仓库初始状态即为格式合规状态。

### 交付物

- `Directory.Build.props`、`.editorconfig`、`.gitignore`

### 完成判据

- 故意引入一个未使用变量 → `dotnet build` 报错（证明 `TreatWarningsAsErrors` 生效）。
- 故意打乱缩进 → `dotnet format --verify-no-changes` 返回非零退出码。
- 仓库中不存在 `global.json`。

**验证对照**：`validation.md` V1.4、V1.5、V1.6

---

## 任务组 3 · 应用宿主、DI 与日志 ⬜

**目标**：搭建 `Microsoft.Extensions.Hosting` 宿主，装配 DI 容器与 Serilog 文件日志。

### 任务

1. **3.1** 在 `qDesktop.App` 引入依赖：`Microsoft.Extensions.Hosting`、`Serilog.Extensions.Hosting`、`Serilog.Sinks.File`。
2. **3.2** 实现 `App.xaml.cs` 的宿主装配：
   - 构建 `Host.CreateApplicationBuilder`；
   - 注册 Serilog 为日志提供程序；
   - 在 `OnStartup` 启动宿主，在 `OnExit` 停止并刷新日志。
3. **3.3** 配置日志路径解析器：
   - 输出目录 `%APPDATA%\qDesktop\logs\`；
   - 滚动文件，按天滚动，**默认保留 7 天**（对齐 `tech-stack.md` 2.7）；
   - Release 默认级别 `Information`，可通过配置切换 `Debug`。
4. **3.4** 创建应用级配置装载（`Microsoft.Extensions.Configuration` + JSON 提供程序），为后续阶段预留配置节。
5. **3.5** 在启动路径写入一条 `Information` 级别日志（如「qDesktop 启动完成」），作为日志链路可用性证据。

### 交付物

- 宿主装配代码
- 日志输出（运行时生成于 `%APPDATA%\qDesktop\logs\`）

### 完成判据

- 应用启动后，`%APPDATA%\qDesktop\logs\` 下生成至少一个 `.log` 文件，含启动日志行。
- 日志文件**不**生成在程序输出目录（`bin/`）。

**验证对照**：`validation.md` V2.1、V2.2、V2.3

---

## 任务组 4 · 空白 WPF 宿主窗口 ⬜

**目标**：提供一个可启动、可显示、可正常退出的最小窗口，作为后续阶段 1 的替换基线。

### 任务

1. **4.1** 创建 `MainWindow.xaml`（**普通窗口**，不启用分层/无边框/穿透——那些属阶段 1）。
2. **4.2** 移除模板默认的占位内容，仅保留空窗口（标题为 `qDesktop`）。
3. **4.3** 确认关闭窗口后进程正常退出（阶段 0 尚无托盘驻留，阶段 9 才改为隐藏到托盘）。

### 交付物

- 可运行的空白 WPF 应用

### 完成判据

- `dotnet run --project src/qDesktop.App` 可显示一个空窗口。
- 关闭窗口后进程结束，无残留进程。

**验证对照**：`validation.md` V3.1、V3.2

---

## 任务组 5 · 单元测试与覆盖率骨架 ⬜

**目标**：建立测试基础设施并验证覆盖率采集链路可用（**不设阈值门禁**）。

### 任务

1. **5.1** 在 `tests/qDesktop.Core.Tests` 引入：`xunit`、`xunit.runner.visualstudio`、`FluentAssertions`、`NSubstitute`、`coverlet.collector`。
2. **5.2** 编写一个样例测试（如验证占位类型的 `AssemblyMarker` 存在性），确保测试项目可被 `dotnet test` 发现并执行。
3. **5.3** 配置覆盖率采集命令：
   - `dotnet test --collect:"XPlat Code Coverage"`
   - 采集范围聚焦 `qDesktop.Core`。
4. **5.4** 确认覆盖率报告（`coverage.cobertura.xml`）正常生成，记录阶段 0 的基线数值（预期接近 0%，属正常）。
5. **5.5** **不配置**任何覆盖率阈值（决策依据：`requirements.md` 3.3）。

### 交付物

- 测试项目与样例测试
- 覆盖率采集配置

### 完成判据

- `dotnet test` 返回 0，样例测试通过。
- `TestResults/**/coverage.cobertura.xml` 生成成功。
- CI 或本地不存在因覆盖率不足而失败的逻辑。

**验证对照**：`validation.md` V4.1、V4.2、V4.3

---

## 任务组 6 · CI 流水线 ⬜

**目标**：建立 GitHub Actions 流水线，在 PR 与 `main` push 上执行完整校验。

### 任务

1. **6.1** 创建 `.github/workflows/ci.yml`。
2. **6.2** 配置触发条件：
   - `pull_request`（目标分支 `main`）
   - `push`（分支 `main`）
   - 依据：`requirements.md` 3.2
3. **6.3** 运行环境与步骤：
   - `runs-on: windows-latest`
   - `actions/setup-dotnet` 安装 `8.0.x`（**CI 固定版本以保证可复现**，不通过 `global.json` 实现）
   - `dotnet restore` → `dotnet build --no-restore` → `dotnet test --no-build --collect:"XPlat Code Coverage"` → `dotnet format --verify-no-changes`
4. **6.4** 上传测试结果与覆盖率报告为构建产物（`actions/upload-artifact`），便于排查。
5. **6.5** 缓存 NuGet 包（`actions/cache`）以缩短流水线耗时。
6. **6.6** 预留代码签名步骤的**注释占位**（默认关闭、密钥以 Secrets 占位），对齐 `tech-stack.md` 2.6 与 `roadmap.md` 阶段 17 任务 7。

### 交付物

- `.github/workflows/ci.yml`

### 完成判据

- YAML 语法合法，工作流可被 GitHub 解析（推送后首次运行验证）。
- 流水线四个阶段全部通过（本地等价命令先验证）。
- 工作流中包含默认关闭的签名步骤占位。

**验证对照**：`validation.md` V5.1、V5.2、V5.3

---

## 任务组 7 · 工程文档与提交 ⬜

**目标**：补齐最小工程说明，并按约定提交，形成可评审的 PR。

### 任务

1. **7.1** 更新根 `README.md`：补充「构建与运行」小节（前置条件、构建命令、运行命令、测试命令）。
2. **7.2** 确认 `specs/` 下三份文档与实现一致（若实现中产生偏差，回改文档而非留待后续）。
3. **7.3** 按 Conventional Commits 拆分提交，建议序列：
   - `chore: 建立解决方案与四个项目骨架`
   - `chore: 统一构建配置与代码规范`
   - `feat: 接入应用宿主、依赖注入与 Serilog 日志`
   - `feat: 添加空白 WPF 宿主窗口`
   - `test: 建立单元测试与覆盖率采集骨架`
   - `ci: 添加 GitHub Actions 构建校验流水线`
   - `docs: 补充构建与运行说明`
4. **7.4** 推送分支 `feature/phase-0-project-scaffold` 并创建面向 `main` 的 PR。
5. **7.5** 在 PR 描述中引用本阶段三份文档路径，并附 `validation.md` 的验收结论。

### 交付物

- 更新的 `README.md`
- 待评审的 PR

### 完成判据

- 提交信息符合 Conventional Commits。
- PR 描述包含三份文档链接与验证结论。
- 按 `validation.md` 第 4 节清单逐项勾选完毕。

**验证对照**：`validation.md` 第 4 节

---

## 附：任务组与路线图关键任务对照

| 路线图阶段 0 关键任务 | 对应任务组 |
|---|---|
| 1. 创建解决方案结构（4 个项目） | TG1 |
| 2. 配置 `net8.0-windows` / `UseWPF` / `AllowUnsafeBlocks` / `UseWindowsForms` / `win-x64` | TG1、TG2 |
| 3. 接入 `Microsoft.Extensions.Hosting`，完成 DI 与配置装配 | TG3 |
| 4. 接入 Serilog 文件日志至 `%APPDATA%\qDesktop\logs\` | TG3 |
| 5. 添加 `.editorconfig`、`.gitignore`、`Directory.Build.props` | TG2 |
| 6. 建立 GitHub Actions 工作流 | TG6 |

---

*本文档为阶段 0 开发计划的权威来源。任务增删须同步更新 0.2 节的依赖表与 `./validation.md` 的验证条目。*
