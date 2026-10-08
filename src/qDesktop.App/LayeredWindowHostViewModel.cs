using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace qDesktop.App;

/// <summary>
/// 阶段 1 窗口属性验证 Demo 的视图模型。
/// </summary>
/// <remarks>
/// <para>
/// 承载 Demo 窗口的三个切换项（点击穿透、置顶、可见性）与状态文本。
/// 本类型**不引用** <see cref="System.Windows.Window"/> / <see cref="System.Windows.Controls.Control"/>
/// 等视图类型：窗口属性的实际应用由视图在属性变更回调中完成
/// （见 specs/2026-10-09-layered-window-host/plan.md TG4.3）。
/// </para>
/// <para>
/// <b>防锁死守护</b>：阶段 1 尚无托盘图标与全局热键，三个切换项各自都可能把验证者锁在
/// 无法操作窗口的状态（穿透后不可点击；隐藏后不可见；取消置顶后可能被其他窗口完全遮挡）。
/// 故每一项在进入「风险状态」时启动一个倒计时守护，到期自动恢复为安全状态，
/// 并在状态文本中实时显示剩余秒数（见 requirements.md 3.6）。
/// </para>
/// <para>
/// <b>临时产物</b>：本视图模型随 <c>LayeredWindowHostDemoWindow</c> 一并在阶段 4 删除。
/// </para>
/// </remarks>
public partial class LayeredWindowHostViewModel : ObservableObject, IDisposable
{
    /// <summary>可见性守护时长：窗口隐藏后自动恢复为可见的秒数（对齐 requirements.md 3.6 的「约 3 秒」）。</summary>
    private const int VisibilityGuardSeconds = 3;

    /// <summary>穿透 / 置顶守护时长：留出足够时间完成人工观察，又不至于长时间无法操作。</summary>
    private const int LockoutGuardSeconds = 10;

    /// <summary>
    /// 守护定时器，1 秒一跳。使用 <see cref="DispatcherTimer"/> 以保证回调回到 UI 线程，
    /// 避免绑定更新跨线程；它是调度原语而非视图类型，不影响本类型脱离视图构造。
    /// </summary>
    private readonly DispatcherTimer _guardTimer;

    private int _guardRemainingSeconds;
    private string? _guardLabel;
    private Action? _guardRevertAction;
    private bool _isDisposed;

    /// <summary>是否开启点击穿透。</summary>
    [ObservableProperty]
    private bool _isClickThrough;

    /// <summary>是否始终置顶。</summary>
    [ObservableProperty]
    private bool _isTopmost = true;

    /// <summary>窗口是否可见；置为 <see langword="false"/> 后由守护定时器自动恢复。</summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>面向验证者的状态说明文本。</summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>初始化 <see cref="LayeredWindowHostViewModel"/>。</summary>
    public LayeredWindowHostViewModel()
    {
        _guardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _guardTimer.Tick += OnGuardTick;

        UpdateStatusText();
    }

    /// <summary>切换点击穿透；开启后启动守护，到期自动关闭（避免无法再次点击窗口）。</summary>
    [RelayCommand]
    private void ToggleClickThrough()
    {
        IsClickThrough = !IsClickThrough;

        if (IsClickThrough)
        {
            StartGuard("点击穿透", LockoutGuardSeconds, () => IsClickThrough = false);
        }
        else
        {
            StopGuard();
        }
    }

    /// <summary>切换始终置顶；关闭后启动守护，到期自动恢复（避免窗口被遮挡后无法操作）。</summary>
    [RelayCommand]
    private void ToggleTopmost()
    {
        IsTopmost = !IsTopmost;

        if (!IsTopmost)
        {
            StartGuard("置顶", LockoutGuardSeconds, () => IsTopmost = true);
        }
        else
        {
            StopGuard();
        }
    }

    /// <summary>切换可见性；隐藏后启动守护，到期自动恢复（避免窗口不可见后无法唤回）。</summary>
    [RelayCommand]
    private void ToggleVisibility()
    {
        IsVisible = !IsVisible;

        if (!IsVisible)
        {
            StartGuard("可见性", VisibilityGuardSeconds, () => IsVisible = true);
        }
        else
        {
            StopGuard();
        }
    }

    /// <summary>释放守护定时器，防止窗口关闭后回调已释放的视图模型。</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _guardTimer.Stop();
        _guardTimer.Tick -= OnGuardTick;
        _guardRevertAction = null;

        GC.SuppressFinalize(this);
    }

    partial void OnIsClickThroughChanged(bool value) => UpdateStatusText();

    partial void OnIsTopmostChanged(bool value) => UpdateStatusText();

    partial void OnIsVisibleChanged(bool value) => UpdateStatusText();

    private void StartGuard(string label, int seconds, Action revertAction)
    {
        _guardLabel = label;
        _guardRevertAction = revertAction;
        _guardRemainingSeconds = seconds;

        _guardTimer.Stop();
        _guardTimer.Start();

        UpdateStatusText();
    }

    private void StopGuard()
    {
        _guardTimer.Stop();
        _guardLabel = null;
        _guardRevertAction = null;
        _guardRemainingSeconds = 0;

        UpdateStatusText();
    }

    private void OnGuardTick(object? sender, EventArgs e)
    {
        if (_guardRevertAction is null)
        {
            _guardTimer.Stop();
            return;
        }

        _guardRemainingSeconds--;

        if (_guardRemainingSeconds > 0)
        {
            UpdateStatusText();
            return;
        }

        // 先停表并清状态，再执行恢复，避免恢复动作触发的属性变更重入守护逻辑。
        var revertAction = _guardRevertAction;
        StopGuard();
        revertAction();
    }

    private void UpdateStatusText()
    {
        var visibility = IsVisible ? "显示" : "隐藏";

        var text = $"点击穿透：{(IsClickThrough ? "开启" : "关闭")}　｜　置顶：{(IsTopmost ? "开启" : "关闭")}　｜　可见性：{visibility}";

        if (_guardRevertAction is not null)
        {
            text += $"{Environment.NewLine}守护中：{_guardLabel}将在 {_guardRemainingSeconds} 秒后自动恢复（阶段 1 无托盘与热键，防止锁死）";
        }

        StatusText = text;
    }
}
