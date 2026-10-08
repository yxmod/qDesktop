# 阶段 1 · PR 描述（可直接复制）

> **用途**：GitHub 连接器无 PR 写权限（`403 Resource not accessible by integration`），
> 无法自动创建 PR。请按下述两段内容手工创建。
>
> **创建地址**：<https://github.com/yxmod/qDesktop/pull/new/feature/phase-1-layered-window-host>
> 　基线分支：`main`　来源分支：`feature/phase-1-layered-window-host`

---

## 一、标题（复制下面代码块内容）

```
阶段 1 · 无边框透明窗口骨架
```

---

## 二、正文（复制下面代码块全部内容）

```markdown
## 阶段 1 · 无边框透明窗口骨架

验证「分层 + 无边框 + 置顶 + 可穿透」四项窗口属性能否在 Windows 上稳定共存——这是栅栏渲染（阶段 4）与边缘隐藏（阶段 10–11）的物理基础。

### 规格文档

- [需求 requirements.md](https://github.com/yxmod/qDesktop/blob/feature/phase-1-layered-window-host/specs/2026-10-09-layered-window-host/requirements.md)
- [计划 plan.md](https://github.com/yxmod/qDesktop/blob/feature/phase-1-layered-window-host/specs/2026-10-09-layered-window-host/plan.md)
- [验证 validation.md](https://github.com/yxmod/qDesktop/blob/feature/phase-1-layered-window-host/specs/2026-10-09-layered-window-host/validation.md)

### 交付内容

| 交付物 | 位置 |
|---|---|
| 窗口扩展样式互操作基础设施（P/Invoke + 常量 + 托管封装，含 Z 序提升） | `src/qDesktop.Interop/` |
| `LayeredWindowHost`（无边框 / 半透明 / 置顶 / 无任务栏按钮 / 防激活 / 点击穿透开关） | `src/qDesktop.App/LayeredWindowHost.cs` |
| `LayeredWindowHostViewModel`（三个切换项 + 统一防锁死守护） | `src/qDesktop.App/` |
| `LayeredWindowHostDemoWindow`（三按钮验证窗口） | `src/qDesktop.App/LayeredWindowHostDemoWindow.xaml` |
| 宿主 DI 注册与启动路径切换 | `src/qDesktop.App/App.xaml.cs` |

### 验证结论

**自动化与静态验证（V1.1–V1.9）：全部通过**

- `dotnet build qDesktop.sln -c Release`：0 警告 0 错误（`TreatWarningsAsErrors=true`）
- `dotnet format --verify-no-changes`：退出码 0
- `dotnet test qDesktop.sln -c Release`：通过 2 / 失败 0
- P/Invoke 归属：全仓库仅命中 `src/qDesktop.Interop/NativeMethods.cs`，`App` 内零命中
- `qDesktop.Core` 无改动；引用方向与程序集数量未变；未新增测试项目
- 反例 V1.9：在 `App` 内临时注入 `DllImport` 后搜索**命中 App**（校验可被检出），已还原

**手工验证（V2–V6、M1–M4）：待人工执行**

窗口外观、点击穿透落点、防激活与双版本一致性依赖真实桌面会话的目视与交互，需评审者在本机执行后补记（见 validation.md 附录 A.2）。启动冒烟已覆盖 V5.1 的一部分：应用启动后持续存活、日志输出「qDesktop 启动完成」、无 DI 解析异常与 `XamlParseException`。

### 实现期试用后修正的缺陷（已回改文档）

1. **取消置顶后的 Z 序（经两轮试用后定型）**：`WS_EX_NOACTIVATE` 会一并抑制「点击激活 → 自动提升 Z 序」。首轮修正为无条件提升 Z 序，实测反而制造了相反问题——清除 `WS_EX_TOPMOST` 后窗口正好压在当前前台窗口之上，而点击它不改变前台窗口，于是前台窗口再也无法被点击提升到本窗口之上（须先激活第三个程序才能解除）。最终规则：**置顶时提升、非置顶时不提升，且取消置顶时主动沉到当前前台窗口之下**（requirements 3.5）。代价是取消置顶后点击本窗口不再把它带到最前，需重新开启置顶或等待守护恢复。
2. **开启穿透后完全锁死**：穿透开启后窗口不可点击，且无任务栏与 `Alt+Tab` 入口。现将防锁死守护从「仅可见性」扩展为三项统一策略：穿透开启 10 秒 → 自动关闭；置顶关闭 10 秒 → 自动恢复；可见性隐藏 3 秒 → 自动恢复。状态文本实时显示剩余秒数（requirements 3.6）。
3. **点击「切换可见性」窗口不消失**：窗口根元素上的 `Visibility` 绑定会被 `Window.Show()` 设定的本地值顶掉。现改由视图在视图模型 `PropertyChanged` 回调中直接设置 `Visibility`，并移除为此引入的转换器（plan TG4.3 / TG5.2）。

### 其它实施说明

- `IsClickThrough` 以 `DependencyProperty` 实现，以满足双向绑定与变更通知，对外语义不变。
- `plan.md` 建议的提交序列将「点击穿透」单列一次提交；因该逻辑与窗口基类同处一个文件，已合并为一次提交。
- `ShowInTaskbar` 运行时切换的警告经 Serilog 静态日志器记录，与 `App.xaml.cs` 的既有日志方式一致。

### 遗留项

| 项 | 处理时点 |
|---|---|
| 另一 Windows 版本（Win10 / Win11）实测补验 | 随阶段 2 双系统探测 |
| Demo 窗口与视图模型的删除 | 阶段 4 |
| 三个切换项的防锁死守护定时器的移除 | 阶段 9 |
| 标题栏编辑态的焦点策略（与 `WS_EX_NOACTIVATE` 冲突） | 阶段 4 |

### 说明

阶段 1 不产出面向最终用户的功能；`MainWindow` 仍保留注册但已不在启动路径显示（阶段 0 的「启动显示空窗口」验收在本分支上有意变更，登记于 validation.md 7.2）。
```
