using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace qDesktop.App;

/// <summary>
/// 将 <see cref="bool"/> 转换为 <see cref="Visibility"/>：<see langword="true"/> →
/// <see cref="Visibility.Visible"/>，<see langword="false"/> → <see cref="Visibility.Hidden"/>。
/// </summary>
/// <remarks>
/// <para>
/// 与内置 <c>BooleanToVisibilityConverter</c> 的唯一差异：<see langword="false"/> 时返回
/// <see cref="Visibility.Hidden"/> 而非 <see cref="Visibility.Collapsed"/>，以对齐
/// specs/2026-10-09-layered-window-host/requirements.md 3.6 的「Visible ↔ Hidden」语义。
/// </para>
/// <para>
/// <b>临时产物</b>：随 <c>LayeredWindowHostDemoWindow</c> 一并在阶段 4 删除。
/// </para>
/// </remarks>
public sealed class BoolToHiddenVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Hidden;

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
