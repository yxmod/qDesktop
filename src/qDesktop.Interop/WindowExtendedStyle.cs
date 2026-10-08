namespace qDesktop.Interop;

/// <summary>
/// 窗口扩展样式位（<c>GWL_EXSTYLE</c> 可读写的 <c>WS_EX_*</c> 值）。
/// </summary>
/// <remarks>
/// 本枚举仅覆盖 qDesktop 在阶段 1 及其直接后续阶段（阶段 2 桌面层嵌入、阶段 9 隐藏不占位）
/// 所需的样式位，不追求对 <c>WS_EX_*</c> 的完整覆盖。
/// 取值依据：WinUser.h。
/// </remarks>
[Flags]
public enum WindowExtendedStyle : long
{
    /// <summary>无扩展样式。</summary>
    None = 0,

    /// <summary>
    /// <c>WS_EX_TRANSPARENT</c>：窗口不接收鼠标命中测试，点击穿透到下层窗口。
    /// </summary>
    Transparent = 0x00000020,

    /// <summary>
    /// <c>WS_EX_TOOLWINDOW</c>：工具窗口，不在任务栏显示按钮，也不出现在 <c>Alt+Tab</c> 列表中。
    /// </summary>
    ToolWindow = 0x00000080,

    /// <summary>
    /// <c>WS_EX_LAYERED</c>：分层窗口，支持透明度与逐像素 Alpha 合成。
    /// </summary>
    /// <remarks>
    /// WPF 在 <c>Window.AllowsTransparency</c> 为 <see langword="true"/> 时
    /// 已自行置位本样式；上层应以「读—判断—按需写」的方式幂等处理，不得盲目重复置位
    /// （见 specs/2026-10-09-layered-window-host/requirements.md 3.4）。
    /// </remarks>
    Layered = 0x00080000,

    /// <summary>
    /// <c>WS_EX_NOACTIVATE</c>：窗口被点击时不成为前台窗口，不夺取键盘焦点。
    /// </summary>
    NoActivate = 0x08000000,
}
