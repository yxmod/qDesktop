namespace qDesktop.Interop;

/// <summary>
/// 面向业务层的窗口扩展样式托管封装。
/// </summary>
/// <remarks>
/// <para>
/// 本类型是 <c>qDesktop.App</c> 操作 <c>WS_EX_*</c> 的**唯一入口**：只暴露
/// 「读取 / 按需置位 / 按需清除 / 提交生效」四个动作，不泄露裸句柄算术与
/// <c>GWL_*</c> 索引。
/// </para>
/// <para>
/// 所有写操作均为**幂等**：读取当前样式，仅在结果确有变化时才调用
/// <c>SetWindowLongPtr</c>，避免覆盖 WPF 内部对样式的管理
/// （见 specs/2026-10-09-layered-window-host/requirements.md 3.4）。
/// </para>
/// </remarks>
public sealed class WindowStyleService
{
    private readonly IntPtr _handle;

    /// <summary>初始化 <see cref="WindowStyleService"/>。</summary>
    /// <param name="handle">目标窗口句柄，须为已创建的非零句柄。</param>
    /// <exception cref="ArgumentException"><paramref name="handle"/> 为零。</exception>
    public WindowStyleService(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            throw new ArgumentException("窗口句柄不能为零。", nameof(handle));
        }

        _handle = handle;
    }

    /// <summary>读取当前扩展样式。</summary>
    public WindowExtendedStyle GetExtendedStyle()
        => (WindowExtendedStyle)NativeMethods.GetWindowLongPtr(_handle, NativeMethods.GwlExStyle).ToInt64();

    /// <summary>判断当前扩展样式是否包含 <paramref name="flag"/> 的全部位。</summary>
    public bool HasExtendedStyle(WindowExtendedStyle flag)
        => WindowStyleHelper.HasFlag(GetExtendedStyle(), flag);

    /// <summary>按需置位：已包含全部目标位时不执行写入。</summary>
    public void AddExtendedStyle(WindowExtendedStyle flags)
    {
        var current = GetExtendedStyle();
        var updated = WindowStyleHelper.WithFlag(current, flags);

        if (updated != current)
        {
            NativeMethods.SetWindowLongPtr(_handle, NativeMethods.GwlExStyle, new IntPtr((long)updated));
        }
    }

    /// <summary>按需清除：不含任何目标位时不执行写入。</summary>
    public void RemoveExtendedStyle(WindowExtendedStyle flags)
    {
        var current = GetExtendedStyle();
        var updated = WindowStyleHelper.WithoutFlag(current, flags);

        if (updated != current)
        {
            NativeMethods.SetWindowLongPtr(_handle, NativeMethods.GwlExStyle, new IntPtr((long)updated));
        }
    }

    /// <summary>按 <paramref name="enabled"/> 置位或清除单个样式位。</summary>
    public void SetExtendedStyleFlag(WindowExtendedStyle flag, bool enabled)
    {
        if (enabled)
        {
            AddExtendedStyle(flag);
        }
        else
        {
            RemoveExtendedStyle(flag);
        }
    }

    /// <summary>
    /// 提交样式变更：补发一次 <c>SetWindowPos(SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE |
    /// SWP_NOZORDER | SWP_NOACTIVATE)</c>，使系统立即应用新的扩展样式且不激活窗口。
    /// </summary>
    public void Flush()
        => NativeMethods.SetWindowPos(
            _handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SetWindowPosFlags.NoMove
            | SetWindowPosFlags.NoSize
            | SetWindowPosFlags.NoZOrder
            | SetWindowPosFlags.NoActivate
            | SetWindowPosFlags.FrameChanged);

    /// <summary>
    /// 将窗口提升到 Z 序顶端，但**不激活**它。
    /// </summary>
    /// <remarks>
    /// <c>WS_EX_NOACTIVATE</c> 会抑制「点击激活 → 系统自动提升 Z 序」的行为，若不显式提升，
    /// 窗口无法通过点击回到最前（见 specs/2026-10-09-layered-window-host/requirements.md 3.5）。
    /// </remarks>
    public void BringToTop()
        => NativeMethods.SetWindowPos(
            _handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SetWindowPosFlags.NoMove
            | SetWindowPosFlags.NoSize
            | SetWindowPosFlags.NoActivate);

    /// <summary>
    /// 把窗口沉到当前前台窗口之下，仍不激活窗口。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 用于取消置顶时：清除 <c>WS_EX_TOPMOST</c> 只会把窗口放到「非置顶层的最上面」，即**正好压在
    /// 当前前台窗口之上**。而本窗口不参与激活，点击它不改变前台窗口，于是前台窗口永远等不到
    /// 「激活 → 提升」的时机，表现为「位于下层级的程序无法被放到上面」。
    /// </para>
    /// <para>
    /// 前台窗口若就是本窗口、或本身是置顶窗口，插入到它之下会无效，或按 <c>SetWindowPos</c> 的
    /// 规则连带把本窗口也变成置顶窗口；此时退回 <c>HWND_NOTOPMOST</c>。
    /// </para>
    /// </remarks>
    public void SinkBelowForeground()
    {
        var foreground = NativeMethods.GetForegroundWindow();

        if (foreground != IntPtr.Zero && foreground != _handle && !IsTopMost(foreground))
        {
            NativeMethods.SetWindowPos(
                _handle,
                foreground,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize | SetWindowPosFlags.NoActivate);
            return;
        }

        NativeMethods.SetWindowPos(
            _handle,
            NativeMethods.HwndNotTopMost,
            0,
            0,
            0,
            0,
            SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize | SetWindowPosFlags.NoActivate);
    }

    private static bool IsTopMost(IntPtr handle)
        => (NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64()
            & (long)WindowExtendedStyle.TopMost) != 0;
}
