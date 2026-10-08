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
    /// <c>WS_EX_NOACTIVATE</c> 会抑制「点击激活 → 系统自动提升 Z 序」的行为。若不显式提升，
    /// 关闭 <c>Topmost</c> 后窗口将无法通过点击回到最前（见
    /// specs/2026-10-09-layered-window-host/requirements.md 3.5）。
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
}
