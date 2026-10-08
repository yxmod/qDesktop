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
/// 等视图类型：窗口属性的实际应用由视图在绑定回调中完成
/// （见 specs/2026-10-09-layered-window-host/plan.md TG4.3）。
/// </para>
/// <para>
/// <b>临时产物</b>：本视图模型随 <c>LayeredWindowHostDemoWindow</c> 一并在阶段 4 删除。
/// </para>
/// </remarks>
public partial class LayeredWindowHostViewModel : ObservableObject, IDisposable
{
    /// <summary>可见性守护时长：窗口隐藏后自动恢复为可见的秒数。</summary>
    private const int VisibilityGuardSeconds = 3;

    /// <summary>
    /// 可见性守护定时器。使用 <see cref="DispatcherTimer"/> 以保证回调回到 UI 线程，
    /// 避免绑定更新跨线程；它是调度原语而非视图类型，不影响本类型脱离视图构造。
    /// </summary>
    private readonly DispatcherTimer _visibilityGuardTimer;

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
        _visibilityGuardTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(VisibilityGuardSeconds),
        };
        _visibilityGuardTimer.Tick += OnVisibilityGuardTick;

        UpdateStatusText();
    }

    /// <summary>切换点击穿透。</summary>
    [RelayCommand]
    private void ToggleClickThrough() => IsClickThrough = !IsClickThrough;

    /// <summary>切换始终置顶。</summary>
    [RelayCommand]
    private void ToggleTopmost() => IsTopmost = !IsTopmost;

    /// <summary>切换可见性；隐藏时启动守护定时器，到期自动恢复为可见。</summary>
    [RelayCommand]
    private void ToggleVisibility()
    {
        IsVisible = !IsVisible;

        if (!IsVisible)
        {
            _visibilityGuardTimer.Stop();
            _visibilityGuardTimer.Start();
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
        _visibilityGuardTimer.Stop();
        _visibilityGuardTimer.Tick -= OnVisibilityGuardTick;

        GC.SuppressFinalize(this);
    }

    partial void OnIsClickThroughChanged(bool value) => UpdateStatusText();

    partial void OnIsTopmostChanged(bool value) => UpdateStatusText();

    partial void OnIsVisibleChanged(bool value)
    {
        if (value)
        {
            // 恢复可见后立即停表，避免守护定时器在已可见状态下再次触发。
            _visibilityGuardTimer.Stop();
        }

        UpdateStatusText();
    }

    private void OnVisibilityGuardTick(object? sender, EventArgs e)
    {
        _visibilityGuardTimer.Stop();
        IsVisible = true;
    }

    private void UpdateStatusText()
    {
        var visibility = IsVisible
            ? "显示"
            : $"隐藏（{VisibilityGuardSeconds} 秒后自动恢复）";

        StatusText = $"点击穿透：{(IsClickThrough ? "开启" : "关闭")}　｜　置顶：{(IsTopmost ? "开启" : "关闭")}　｜　可见性：{visibility}";
    }
}
