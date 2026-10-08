namespace qDesktop.Interop;

/// <summary>
/// 程序集占位标记类型。
/// </summary>
/// <remarks>
/// Win32 互操作代码自阶段 1（无边框透明窗口骨架）起引入，当前包含窗口扩展样式
/// （<c>WS_EX_*</c>）的读写封装；桌面层探测等更复杂的互操作自阶段 2 起追加。
/// 本类型仅用于保证程序集可被引用并提供稳定的程序集标识。
/// </remarks>
public static class AssemblyMarker
{
    /// <summary>程序集逻辑名称，用于诊断与测试断言。</summary>
    public const string AssemblyName = "qDesktop.Interop";
}
