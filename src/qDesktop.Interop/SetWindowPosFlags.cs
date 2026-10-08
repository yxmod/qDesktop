namespace qDesktop.Interop;

/// <summary>
/// <c>SetWindowPos</c> 的行为标志（<c>SWP_*</c>）。
/// </summary>
/// <remarks>
/// 仅在 <see cref="WindowStyleService"/> 内部使用，不对业务层暴露。
/// </remarks>
[Flags]
internal enum SetWindowPosFlags : uint
{
    /// <summary><c>SWP_NOSIZE</c>：保持当前尺寸。</summary>
    NoSize = 0x0001,

    /// <summary><c>SWP_NOMOVE</c>：保持当前位置。</summary>
    NoMove = 0x0002,

    /// <summary><c>SWP_NOZORDER</c>：保持当前 Z 序。</summary>
    NoZOrder = 0x0004,

    /// <summary><c>SWP_NOACTIVATE</c>：不激活窗口。</summary>
    NoActivate = 0x0010,

    /// <summary><c>SWP_FRAMECHANGED</c>：强制重新计算并应用非客户区（含扩展样式变更）。</summary>
    FrameChanged = 0x0020,
}
