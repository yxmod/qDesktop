using System.Runtime.InteropServices;

namespace qDesktop.Interop;

/// <summary>
/// qDesktop 全部 Win32 P/Invoke 声明的唯一落点。
/// </summary>
/// <remarks>
/// <para>
/// 架构约束：业务层（<c>qDesktop.App</c>）不得直接声明 <c>DllImport</c> / <c>LibraryImport</c>，
/// 只能调用本程序集暴露的托管封装（见 specs/tech-stack.md 2.3 与
/// specs/2026-10-09-layered-window-host/requirements.md 5.1 第 2 条）。
/// </para>
/// <para>
/// 使用 <see cref="LibraryImportAttribute"/>（编译期源生成）而非 <c>DllImport</c>，
/// 以获得无运行时封送开销的调用桩。
/// </para>
/// </remarks>
internal static partial class NativeMethods
{
    /// <summary><c>GWL_EXSTYLE</c>：<c>GetWindowLongPtr</c> / <c>SetWindowLongPtr</c> 的扩展样式索引。</summary>
    internal const int GwlExStyle = -20;

    /// <summary><c>HWND_NOTOPMOST</c>：把窗口置于所有非置顶窗口之上（Z 序参数）。</summary>
    internal static readonly IntPtr HwndNotTopMost = new(-2);

    /// <summary>读取窗口扩展样式（按进程位宽分派 32 / 64 位入口）。</summary>
    internal static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        => IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));

    /// <summary>写入窗口扩展样式并返回旧值（按进程位宽分派 32 / 64 位入口）。</summary>
    internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        => IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static partial IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static partial int GetWindowLong32(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static partial IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static partial int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>取当前前台窗口句柄；无前台窗口时返回零。</summary>
    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetForegroundWindow();

    /// <summary>设置窗口位置、尺寸与 Z 序；本项目的用途是借 <c>SWP_FRAMECHANGED</c> 使样式变更即时生效。</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        SetWindowPosFlags uFlags);
}
