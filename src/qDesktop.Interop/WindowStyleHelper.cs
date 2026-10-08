namespace qDesktop.Interop;

/// <summary>
/// <see cref="WindowExtendedStyle"/> 的纯逻辑位运算辅助。
/// </summary>
/// <remarks>
/// 无副作用、不接触窗口句柄，便于在任意上下文中复用与推演。
/// 不使用 <see cref="Enum.HasFlag"/>，以避免其装箱开销。
/// </remarks>
public static class WindowStyleHelper
{
    /// <summary>置位指定样式位，返回新值（<paramref name="current"/> 不被修改）。</summary>
    public static WindowExtendedStyle WithFlag(WindowExtendedStyle current, WindowExtendedStyle flag)
        => current | flag;

    /// <summary>清除指定样式位，返回新值（<paramref name="current"/> 不被修改）。</summary>
    public static WindowExtendedStyle WithoutFlag(WindowExtendedStyle current, WindowExtendedStyle flag)
        => current & ~flag;

    /// <summary>判断是否**包含全部** <paramref name="flag"/> 位。</summary>
    public static bool HasFlag(WindowExtendedStyle current, WindowExtendedStyle flag)
        => (current & flag) == flag;
}
