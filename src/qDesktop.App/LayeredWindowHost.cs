using System.Windows;
using System.Windows.Interop;

using qDesktop.Interop;

using Serilog;

namespace qDesktop.App;

/// <summary>
/// 无边框分层窗口基类：默认「无边框、半透明、始终置顶、无任务栏按钮、不抢焦点」。
/// </summary>
/// <remarks>
/// <para>
/// 本类验证并固化栅栏渲染（阶段 4）与边缘隐藏（阶段 10–11）共同依赖的窗口前提，
/// 是 specs/roadmap.md 阶段 1 的核心交付物。
/// </para>
/// <para>
/// 扩展样式在 <see cref="FrameworkElement.SourceInitialized"/> 中**一次性**应用；
/// 窗口过程钩子在同一时机挂载、在 <see cref="Window.Closed"/> 时移除，
/// 以避免句柄泄漏（见 specs/2026-10-09-layered-window-host/requirements.md 3.4、3.5）。
/// </para>
/// <para>
/// 全部 Win32 调用经 <see cref="WindowStyleService"/> 转发，本程序集内不出现任何原生互操作声明。
/// </para>
/// </remarks>
public class LayeredWindowHost : Window
{
    /// <summary><see cref="IsClickThrough"/> 的依赖属性定义。</summary>
    public static readonly DependencyProperty IsClickThroughProperty = DependencyProperty.Register(
        nameof(IsClickThrough),
        typeof(bool),
        typeof(LayeredWindowHost),
        new FrameworkPropertyMetadata(false, OnIsClickThroughChanged));

    private HwndSource? _hwndSource;
    private WindowStyleService? _styleService;
    private bool _isInitialized;
    private bool _isRevertingShowInTaskbar;

    /// <summary>初始化 <see cref="LayeredWindowHost"/> 并设定默认窗口样式。</summary>
    public LayeredWindowHost()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        Background = System.Windows.Media.Brushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
    }

    /// <summary>
    /// 获取或设置是否开启点击穿透；开启后鼠标点击直接落到下层窗口。
    /// </summary>
    /// <remarks>
    /// 可在窗口句柄创建前设置：此时仅记录期望状态，待
    /// <see cref="FrameworkElement.SourceInitialized"/> 后统一应用（见
    /// specs/2026-10-09-layered-window-host/plan.md TG3.3）。
    /// </remarks>
    public bool IsClickThrough
    {
        get => (bool)GetValue(IsClickThroughProperty);
        set => SetValue(IsClickThroughProperty, value);
    }

    /// <summary>
    /// 当前窗口扩展样式中 <c>WS_EX_TRANSPARENT</c> 的**实际**取值，用于校验属性值与系统样式一致。
    /// </summary>
    public bool IsClickThroughApplied
        => _styleService?.HasExtendedStyle(WindowExtendedStyle.Transparent) ?? false;

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        // ShowInTaskbar 在窗口显示后变更会导致 WPF 销毁并重建 HWND，
        // 已应用的扩展样式随旧句柄一并丢失。故约束为初始化期设定：
        // 运行时变更一律记录警告并回滚（见 requirements.md 3.4）。
        if (e.Property == ShowInTaskbarProperty && _isInitialized && !_isRevertingShowInTaskbar)
        {
            _isRevertingShowInTaskbar = true;

            try
            {
                Log.ForContext<LayeredWindowHost>().Warning(
                    "ShowInTaskbar 不支持在窗口显示后运行时切换，已忽略本次变更（保持 {Value}）",
                    e.OldValue);

                SetCurrentValue(ShowInTaskbarProperty, e.OldValue);
            }
            finally
            {
                _isRevertingShowInTaskbar = false;
            }

            return;
        }

        base.OnPropertyChanged(e);
    }

    private static void OnIsClickThroughChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((LayeredWindowHost)d).ApplyClickThrough((bool)e.NewValue);

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        _hwndSource.AddHook(WindowProcedureHook);

        _styleService = new WindowStyleService(_hwndSource.Handle);

        // 工具窗口 + 不激活：消除任务栏条目、Alt+Tab 条目与点击夺焦。
        _styleService.AddExtendedStyle(WindowExtendedStyle.ToolWindow | WindowExtendedStyle.NoActivate);

        // WS_EX_LAYERED 由 WPF 在 AllowsTransparency=true 时自行置位；仅在不含该位时补写，
        // 保持「样式由谁负责」的归属清晰（见 requirements.md 3.4）。
        if (!_styleService.HasExtendedStyle(WindowExtendedStyle.Layered))
        {
            _styleService.AddExtendedStyle(WindowExtendedStyle.Layered);
        }

        _styleService.Flush();

        // 应用句柄创建前记录的期望穿透状态（TG3.3）。
        ApplyClickThrough(IsClickThrough);

        _isInitialized = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        SourceInitialized -= OnSourceInitialized;
        Closed -= OnClosed;

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WindowProcedureHook);
            _hwndSource = null;
        }

        _styleService = null;
    }

    private void ApplyClickThrough(bool enabled)
    {
        if (_styleService is null)
        {
            // 句柄尚未创建：期望状态已由依赖属性保存，SourceInitialized 时统一应用。
            return;
        }

        _styleService.SetExtendedStyleFlag(WindowExtendedStyle.Transparent, enabled);
        _styleService.Flush();
    }

    private IntPtr WindowProcedureHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // 消息级防激活：与 WS_EX_NOACTIVATE 构成双重保障，避免不同 Windows 版本下行为漂移
        // （见 requirements.md 3.5）。
        if (msg == WindowMessages.MouseActivate)
        {
            // WS_EX_NOACTIVATE 会抑制「点击激活 → 自动提升 Z 序」，若不显式提升，
            // 关闭 Topmost 后窗口将无法通过点击回到最前。此处只提升、不激活，仍不夺焦点。
            _styleService?.BringToTop();

            handled = true;
            return new IntPtr(WindowMessages.MouseActivateNoActivate);
        }

        return IntPtr.Zero;
    }
}
