using System.Reflection;

using FluentAssertions;

namespace qDesktop.Core.Tests;

/// <summary>
/// 阶段 0 的测试骨架样例。
/// </summary>
/// <remarks>
/// 本类仅验证测试基础设施（xUnit + FluentAssertions + coverlet 采集链路）可用，
/// 不承载业务语义。领域模型的测试自阶段 3 起引入。
/// </remarks>
public sealed class AssemblyMarkerTests
{
    [Fact]
    public void AssemblyName_ShouldMatchCoreAssemblyIdentity()
    {
        // Arrange
        var coreAssembly = typeof(AssemblyMarker).Assembly;

        // Act
        var actual = AssemblyMarker.AssemblyName;

        // Assert
        actual.Should().Be(coreAssembly.GetName().Name);
    }

    [Fact]
    public void CoreAssembly_ShouldNotReferenceInteropOrWpf()
    {
        // 架构约束的可执行断言：qDesktop.Core 不得依赖 Interop 或 WPF
        // （见 specs/2026-10-08-project-scaffold/requirements.md 第 4 节）。
        var referenced = typeof(AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .ToArray();

        referenced.Should().NotContain("qDesktop.Interop");
        referenced.Should().NotContain(name => name!.StartsWith("PresentationFramework", StringComparison.Ordinal));
        referenced.Should().NotContain(name => name!.StartsWith("WindowsBase", StringComparison.Ordinal));
    }
}
