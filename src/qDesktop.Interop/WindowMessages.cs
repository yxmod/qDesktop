namespace qDesktop.Interop;

/// <summary>
/// 窗口消息与其返回码常量。
/// </summary>
/// <remarks>
/// 仅收录 qDesktop 窗口过程钩子需要判别的消息。取值依据：WinUser.h。
/// </remarks>
public static class WindowMessages
{
    /// <summary><c>WM_MOUSEACTIVATE</c>：鼠标在非活动窗口中按下时发送，用于决定是否激活该窗口。</summary>
    public const int MouseActivate = 0x0021;

    /// <summary>
    /// <c>MA_NOACTIVATE</c>：<c>WM_MOUSEACTIVATE</c> 的返回值，表示不激活窗口且不丢弃本次鼠标事件。
    /// </summary>
    public const int MouseActivateNoActivate = 3;
}
