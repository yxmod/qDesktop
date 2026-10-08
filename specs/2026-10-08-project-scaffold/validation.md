# 阶段 0 · 项目脚手架与工程规范 — 验证标准（Validation）

> 所属分支：`feature/phase-0-project-scaffold`
> 关联需求：`./requirements.md`　关联计划：`./plan.md`
> 文档版本：v1.0 ｜ 创建日期：2026-10-08

---

## 1. 验证总则

### 1.1 判定原则

1. **可观测**：每条验证必须产出可复现的事实（命令退出码、文件存在性、进程状态），禁止使用「看起来正常」「应该没问题」等主观描述。
2. **可复现**：验证步骤须在**干净的克隆**上可重复执行，不依赖开发者本机遗留状态。
3. **可证伪**：关键验证项须给出**反例**（故意破坏后应失败），以证明校验逻辑真实生效，而非恰好通过。
4. **双版本覆盖**：涉及系统行为的验证须在 Windows 10 1903+ 与 Windows 11 上分别执行（阶段 0 仅涉及启动行为）。

### 1.2 验证分类

| 类型 | 执行方式 | 是否阻断合并 |
|---|---|---|
| **自动化验证**（V1–V5） | CI 流水线自动执行 | ✅ 阻断 |
| **手工验证**（M1–M4） | 评审者本地执行并记录 | ✅ 阻断 |
| **文档一致性检查** | 人工比对 | ✅ 阻断 |

### 1.3 验证环境基线

| 项 | 要求 |
|---|---|
| 操作系统 | Windows 10 1903+ / Windows 11（至少各验证一次启动） |
| 架构 | x64 |
| SDK | 本地任意可构建 `net8.0-windows` 的 SDK；CI 固定 `8.0.x` |
| 仓库状态 | 干净克隆，无未提交改动 |

---

## 2. 自动化验证

### V1 · 构建与项目结构

| 编号 | 验证项 | 执行命令 | 通过判据 |
|---|---|---|---|
| V1.1 | 解决方案可构建 | `dotnet build qDesktop.sln -c Release` | 退出码 0 |
| V1.2 | 零警告零错误 | 同上，检查输出 | 输出包含 `0 Warning(s)` 与 `0 Error(s)` |
| V1.3 | 无 x86 / arm64 配置 | `dotnet build qDesktop.sln -p:Platform=x86` | 失败或明确无该平台配置（**预期非零退出**，属反例验证） |
| V1.4 | `TreatWarningsAsErrors` 生效 | 在任一 `.cs` 中临时引入未使用变量后构建 | 构建**失败**（反例验证），随后还原 |
| V1.5 | 无 `global.json` | `test -f global.json` | 不存在（依据 `requirements.md` 3.1） |
| V1.6 | 引用方向正确 | 检查各 `.csproj` 的 `<ProjectReference>` | 仅存在 `App→Core`、`App→Interop`、`Core.Tests→Core`；**不存在** `Core→Interop` 或 `Core→WPF` |
| V1.7 | 目标框架正确 | 检查 `.csproj` / `Directory.Build.props` | App 与 Interop 为 `net8.0-windows`，Core 为 `net8.0` |
| V1.8 | 构建产物为 `win-x64` | 检查输出路径 | 含 `win-x64` 标识 |

### V2 · 日志链路

| 编号 | 验证项 | 执行方式 | 通过判据 |
|---|---|---|---|
| V2.1 | 日志文件生成 | 启动应用后检查 `%APPDATA%\qDesktop\logs\` | 存在至少一个 `.log` 文件 |
| V2.2 | 启动日志写入 | 打开该日志文件 | 含启动完成日志行，且含时间戳与级别 |
| V2.3 | 日志不落程序目录 | 检查 `src/qDesktop.App/bin/` | 不存在 `.log` 文件 |
| V2.4 | 滚动与保留策略 | 检查 Serilog 配置 | 按天滚动，保留 7 天（对齐 `tech-stack.md` 2.7） |

### V3 · 应用可运行性

| 编号 | 验证项 | 执行方式 | 通过判据 |
|---|---|---|---|
| V3.1 | 应用可启动并显示窗口 | `dotnet run --project src/qDesktop.App` | 显示一个标题为 `qDesktop` 的空窗口 |
| V3.2 | 关闭后进程退出 | 关闭窗口后检查进程列表 | 无残留 `qDesktop.App` 进程 |
| V3.3 | 双系统可启动 | 分别在 Win10 1903+ 与 Win11 启动 | 均正常显示窗口（对齐 `roadmap.md` 阶段 0 验收标准） |

### V4 · 测试与覆盖率

| 编号 | 验证项 | 执行命令 | 通过判据 |
|---|---|---|---|
| V4.1 | 测试可执行 | `dotnet test qDesktop.sln` | 退出码 0，至少 1 个测试通过 |
| V4.2 | 覆盖率报告生成 | `dotnet test --collect:"XPlat Code Coverage"` | `TestResults/**/coverage.cobertura.xml` 存在 |
| V4.3 | 无覆盖率门禁 | 检查 CI 配置与测试配置 | 不存在因覆盖率不足而失败的逻辑（依据 `requirements.md` 3.3） |
| V4.4 | 测试框架齐备 | 检查 `Core.Tests.csproj` | 含 xUnit、FluentAssertions、NSubstitute、coverlet.collector |

### V5 · CI 流水线

| 编号 | 验证项 | 执行方式 | 通过判据 |
|---|---|---|---|
| V5.1 | YAML 合法 | 检查 `.github/workflows/ci.yml` | 语法合法，可被 GitHub 解析（推送后首次运行确认） |
| V5.2 | 触发范围正确 | 检查 `on:` 节 | 同时含 `pull_request`（→`main`）与 `push`（`main`），依据 `requirements.md` 3.2 |
| V5.3 | 四阶段齐备 | 检查 `steps` | 依次含 restore / build / test / `dotnet format --verify-no-changes` |
| V5.4 | 格式校验可失败 | 本地临时打乱缩进后执行 `dotnet format --verify-no-changes` | 返回**非零退出码**（反例验证），随后还原 |
| V5.5 | 产物上传 | 检查 `upload-artifact` 步骤 | 测试结果与覆盖率报告被上传 |
| V5.6 | 签名步骤占位 | 检查工作流 | 存在默认关闭的签名步骤（注释或 `if: false`），密钥以 Secrets 占位 |
| V5.7 | CI 环境固定 | 检查 `setup-dotnet` | 版本为 `8.0.x`，且**未**新增 `global.json` |

---

## 3. 手工验证

### M1 · 干净克隆可构建

在**新目录**克隆分支并执行 `dotnet build` 与 `dotnet run`。

- **通过判据**：无任何本机遗留配置（NuGet 缓存除外）即可完成构建与启动。
- **失败信号**：报错提示缺少某文件、依赖某本地路径、或依赖未提交的配置。

### M2 · 反例验证汇总

逐项执行 V1.3、V1.4、V5.4 的反例，确认校验逻辑真实生效。

- **通过判据**：三项均按预期失败，且还原后恢复通过。
- **说明**：本项用于排除「配置写了但未生效」的假阳性。

### M3 · 日志可读性抽查

打开 `%APPDATA%\qDesktop\logs\` 下的日志文件，人工确认格式可读、含时间戳、级别与消息。

- **通过判据**：无需额外工具即可判断日志来源与时间。

### M4 · 启动性能基线记录

记录冷启动到窗口可见的耗时（秒表或日志时间戳差值），写入 PR 描述。

- **通过判据**：数值被记录在案（阶段 0 **不设**性能阈值，仅建立基线，供阶段 16 对比）。

---

## 4. 合并检查清单（Definition of Done）

代码可以合并到 `main` 当且仅当以下**全部**为 ✅：

### 4.1 需求与范围

- [ ] `requirements.md` 第 2.1 节的 10 项范围（S1–S10）全部交付
- [ ] 未触碰第 2.2 节范围外内容（无栅栏/图标/托盘/打包相关代码）
- [ ] 四项关键决策（3.1–3.4）在实现中被如实执行

### 4.2 构建与架构

- [ ] V1.1–V1.8 全部通过
- [ ] `qDesktop.Core` 未引用 WPF 与 Interop（架构约束，编译期可证）
- [ ] 解决方案中不存在 x86 / arm64 配置
- [ ] 仓库中不存在 `global.json`

### 4.3 运行与日志

- [ ] V2.1–V2.4 全部通过
- [ ] V3.1–V3.3 全部通过（含双系统启动）

### 4.4 测试与 CI

- [ ] V4.1–V4.4 全部通过
- [ ] V5.1–V5.7 全部通过
- [ ] 覆盖率链路可用但**无阈值门禁**

### 4.5 反例验证

- [ ] M2 的三项反例均按预期失败并已还原

### 4.6 工程约定

- [ ] 所有提交信息符合 Conventional Commits（`feat`/`fix`/`chore`/`docs`/`test`/`ci` 等前缀）
- [ ] 分支名为 `feature/phase-0-project-scaffold`
- [ ] `README.md` 含构建与运行说明
- [ ] PR 描述引用 `plan.md` / `requirements.md` / `validation.md` 三份文档
- [ ] `dotnet format --verify-no-changes` 通过

### 4.7 文档一致性

- [ ] 三份文档的描述与实际实现一致（实现中若有偏差，已回改文档）

---

## 5. 路线图验收标准对照

| `roadmap.md` 阶段 0 验收标准 | 对应验证项 | 状态 |
|---|---|---|
| `dotnet build` 零警告零错误（`TreatWarningsAsErrors=true`） | V1.2、V1.4 | ✅ 0 警告 0 错误；反例构建失败 |
| 应用启动后显示一个空窗口，日志文件正常写入 | V3.1、V2.1、V2.2 | ✅ 窗口标题 `qDesktop`；日志写入 `%APPDATA%\qDesktop\logs\` |
| CI 在 PR 上成功执行并通过格式校验 | V5.1–V5.5 | 🟨 YAML 与步骤齐备、本地等价命令全通过；首次运行待 PR 打开后确认 |
| 四个项目引用方向正确，`Core` 不引用 `Interop` 与 WPF | V1.6 | ✅ 由项目引用强制，并有单元测试持续断言 |
| 构建产物为 `win-x64`，无 x86 / arm64 配置，可在 Win10 1903+ 与 Win11 启动 | V1.3、V1.8、V3.3 | 🟨 V1.3 / V1.8 通过；V3.3 双系统启动仅在本机 Windows 验证，另一系统待补 |

---

## 6. 边界与回归

### 6.1 边界场景

| 场景 | 期望行为 | 验证项 |
|---|---|---|
| 无 `%APPDATA%\qDesktop\` 目录时首次启动 | 自动创建目录并写入日志，不抛异常 | V2.1 |
| `%APPDATA%` 不可写 | 应用不崩溃，降级为不写日志或写临时目录（行为须被记录） | 手工补充 |
| 在非 Windows 上执行 `dotnet build` | 明确失败（目标框架为 `windows`），不产生误导性错误 | 手工补充 |
| 无网络环境构建 | 若 NuGet 包已缓存则成功；否则失败并给出明确提示 | 手工补充 |

### 6.2 回归风险

| 风险 | 说明 | 缓解 |
|---|---|---|
| `TreatWarningsAsErrors` 在后续阶段引入新依赖后产生大量警告 | 阶段 1 起引入 Win32 互操作代码 | 在阶段 1 前评估是否对特定规则（如 CA1416 平台兼容性）做定向豁免，豁免须在 `tech-stack.md` 登记 |
| 本地 SDK 与 CI SDK 版本漂移 | 本地 10.0.401 vs CI 8.0.x | CI 固定版本保证可复现；若出现差异，优先以 CI 为准 |
| 后续阶段误将 P/Invoke 写入 `Core` | 破坏架构约束 | V1.6 在每次 PR 中持续校验 |

---

## 7. 阻塞与回退

### 7.1 阻塞判定

出现以下任一情况，本阶段标记为 ⛔ 被阻塞并暂停合并：

1. `dotnet build` 在干净克隆上无法通过，且原因不可定位。
2. `qDesktop.Core` 无法在不引用 WPF 的前提下满足后续阶段需要（须回到 `tech-stack.md` 第 1 节重新评估）。
3. CI 环境无法安装可构建 `net8.0-windows` 的 SDK。

### 7.2 回退策略

| 情况 | 回退动作 |
|---|---|
| CI 暂时不可用（仓库未托管） | 以本地等价命令（V1–V4）作为临时门禁，CI 于仓库托管后补验（V5 转为待办，不阻断本次合并） |
| SDK 版本导致构建失败 | 按 `requirements.md` 3.1 的代价表处理；**不**通过新增 `global.json` 规避（除非重新决策并更新需求文档） |
| 覆盖率采集失败 | 保留测试骨架，临时移除采集参数；须在阶段 3 前修复（阶段 3 才启用阈值门禁） |

> **注**：CI 因仓库未配置 remote 而无法运行时，V5 各项标注为「待仓库托管后验证」，不视为本阶段未完成；其余 V1–V4 与 M1–M4 仍须全部通过。

---

*本文档为阶段 0 验证标准的权威来源。验证项增删须同步更新第 4 节合并检查清单与第 5 节对照表。*

---

## 附录 A · 实施记录

> 执行日期：2026-10-08 ｜ 分支：`feature/phase-0-project-scaffold`

### A.1 自动化验证执行结果

| 编号 | 结果 | 证据 |
|---|---|---|
| V1.1 / V1.2 | ✅ | `dotnet build qDesktop.sln -c Release` → `0 警告 0 错误` |
| V1.3 | ✅ | `-p:Platform=x86` → `error MSB4126`（退出码 1）；`arm64` 同 |
| V1.4 | ✅ | 引入未使用变量 → `error CS0219`（反例生效，已还原） |
| V1.5 | ✅ | 仓库无 `global.json` |
| V1.6 / V1.7 | ✅ | 仅 3 条 `ProjectReference`；TFM 为 `net8.0` / `net8.0-windows` |
| V1.8 | ✅ | 输出路径 `bin/Release/net8.0-windows/win-x64/` |
| V2.1–V2.4 | ✅ | `%APPDATA%\qDesktop\logs\qDesktop-YYYYMMDD.log` 生成，含时间戳与级别；`bin/` 下无 `.log`；按天滚动、保留 7 天 |
| V3.1 / V3.2 | ✅ | 窗口标题 `qDesktop`；关闭后进程以退出码 0 结束，无残留进程 |
| V3.3 | 🟨 | 仅在本机 Windows 验证；另一系统待补 |
| V4.1 / V4.2 | ✅ | 2 个测试通过；`TestResults/**/coverage.cobertura.xml` 生成 |
| V4.3 / V4.4 | ✅ | 无覆盖率门禁；测试项目含 4 类框架包 |
| V5.1–V5.3 | ✅ | YAML 可解析；`on:` 含 `pull_request` 与 `push`（均限 `main`）；四阶段齐备 |
| V5.4 | ✅ | 打乱缩进 → 退出码 2（反例生效，已还原） |
| V5.5–V5.7 | ✅ | 产物上传、签名步骤默认关闭且以 Secrets 占位、`setup-dotnet` 固定 `8.0.x` 且无 `global.json` |
| M1 | ✅ | 干净克隆后 LF 一致、构建零警告、测试通过、格式校验通过、应用可启动 |
| M2 | ✅ | V1.3 / V1.4 / V5.4 三项反例均按预期失败并已还原 |
| M3 | ✅ | 日志格式为「时间戳 [级别] 来源 / 消息」，人工可读 |
| M4 | ✅ | 热启动至窗口可见：547 / 689 / 773 ms（3 次，轮询粒度 25 ms） |

### A.2 与规格的偏差（已回改文档）

1. **解决方案文件格式**：`.NET 10 SDK` 的 `dotnet new sln` 默认生成 `.slnx`，但 CI 固定的 `.NET 8 SDK` 无法解析该格式，故显式使用经典 `.sln`（`--format sln`）。
2. **新增 `.gitattributes`，并将 `end_of_line` 固定为 LF**：本机 `core.autocrlf=true` 与 CI runner 的 `core.autocrlf=false` 会使同一提交的换行符不同，导致 `dotnet format --verify-no-changes` 随机失败。此为可复现性的必要条件。
3. **新增 `Platforms=AnyCPU;x64` 白名单**（`Directory.Build.props`）：`dotnet new sln` 生成的 `.sln` 含 `x86` 配置，且 `-p:Platform=x86` 会被静默接受，不满足 1.1 节的「可证伪」原则。已重写 `.sln` 并加入项目级平台白名单，使 V1.3 真实生效。
4. **App 项目移除 `System.Windows.Forms` 全局 using**：`UseWindowsForms=true` + `ImplicitUsings` 会注入该全局 using，与 WPF 的 `Application` 等类型产生 `CS0104` 二义性。后续阶段仅需 WinForms 的 `NotifyIcon`，按需显式引用即可。
5. **`CA1707` 定向豁免**：与 xUnit 的 `Method_Scenario_Expectation` 命名惯例冲突，仅在 `tests/**` 关闭，已登记于 `specs/tech-stack.md` 2.5.1。
6. **新增 `tests/qDesktop.Core.Tests/coverlet.runsettings`**：以配置而非命令行参数固定采集范围为 `qDesktop.Core`，便于本地与 CI 共用同一份设置。
7. **覆盖率基线为「0 可计行」**：`qDesktop.Core` 在阶段 0 仅含常量定义，无 IL 可计行，故 `lines-valid=0`。链路可用性已由报告成功产出证明；有意义的覆盖率数值自阶段 3 起出现。

### A.3 遗留项

| 项 | 说明 | 处理时点 |
|---|---|---|
| V3.3 双系统启动 | 本机仅安装一个 Windows 版本，未覆盖另一系统 | 首次 Release 前补充 |
| V5.1 / V5.3 的 CI 首次运行 | 需 PR 打开后由流水线实际执行确认 | 本 PR 评审期间 |
| `global.json` 与 SDK 漂移 | 本地 10.0.401 / CI 8.0.x，按决策 3.1 不做锁定 | 出现差异时以 CI 为准 |

