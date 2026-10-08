using System.Windows;

namespace qDesktop.App;

/// <summary>
/// 阶段 1 窗口属性验证 Demo 窗口（无边框分层窗口）。
/// </summary>
/// <remarks>
/// <para>
/// 承载三个验证按钮（点击穿透 / 置顶 / 可见性）与状态文本，直接继承
/// <see cref="LayeredWindowHost"/>，使「窗口属性是否生效」的观察对象唯一。
/// </para>
/// <para>
/// <b>临时产物</b>：本窗口及其视图模型、转换器于阶段 4 整体删除
/// （见 specs/2026-10-09-layered-window-host/requirements.md 3.1）。
/// </para>
/// </remarks>
public partial class LayeredWindowHostDemoWindow : LayeredWindowHost
{
    private readonly LayeredWindowHostViewModel _viewModel;

    /// <summary>初始化 <see cref="LayeredWindowHostDemoWindow"/>。</summary>
    /// <param name="viewModel">绑定的视图模型，生命周期与本窗口对齐。</param>
    public LayeredWindowHostDemoWindow(LayeredWindowHostViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = viewModel;
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;

        // 停止守护定时器，避免窗口关闭后回调已释放的视图模型（plan.md TG4.5）。
        _viewModel.Dispose();
    }
}
