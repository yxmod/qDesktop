using System.ComponentModel;
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
/// 可见性由本窗口在视图模型属性变更回调中应用，而非 XAML 绑定：窗口根元素上的
/// <c>Visibility</c> 绑定会被 <see cref="Window.Show"/> 设定的本地值顶掉，绑定随之中断。
/// 这正对应 plan.md TG4.3「窗口属性的实际应用由视图在绑定回调中完成」。
/// </para>
/// <para>
/// <b>临时产物</b>：本窗口及其视图模型于阶段 4 整体删除
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
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += OnWindowClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayeredWindowHostViewModel.IsVisible))
        {
            // 置为 Visible 即重新显示，置为 Hidden 即隐藏（等价于 Show / Hide）。
            Visibility = _viewModel.IsVisible ? Visibility.Visible : Visibility.Hidden;
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        // 停止守护定时器，避免窗口关闭后回调已释放的视图模型（plan.md TG4.5）。
        _viewModel.Dispose();
    }
}
