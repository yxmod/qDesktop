namespace qDesktop.Core;

/// <summary>
/// 程序集占位标记类型。
/// </summary>
/// <remarks>
/// 阶段 0 的 qDesktop.Core 尚无业务代码。本类型用于：
/// 1. 保证空程序集可被 <c>qDesktop.Core.Tests</c> 正常引用；
/// 2. 为单元测试骨架提供一个可断言的稳定锚点。
/// 领域模型将在阶段 3 引入，届时本类型可移除。
/// </remarks>
public static class AssemblyMarker
{
    /// <summary>程序集逻辑名称，用于诊断与测试断言。</summary>
    public const string AssemblyName = "qDesktop.Core";
}
