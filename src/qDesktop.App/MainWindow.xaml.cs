using System.Windows;

namespace qDesktop.App;

/// <summary>
/// 阶段 0 的空白宿主窗口，作为后续阶段桌面层窗口的替换基线。
/// </summary>
/// <remarks>
/// 关闭本窗口即结束应用（<see cref="Application.ShutdownMode"/> 默认为
/// <see cref="ShutdownMode.OnLastWindowClose"/>）。阶段 9 引入托盘驻留后，
/// 关闭行为将改为隐藏至托盘。
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>初始化 <see cref="MainWindow"/>。</summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
