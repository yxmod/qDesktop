using System.IO;

namespace qDesktop.App;

/// <summary>
/// 应用运行期路径解析。
/// </summary>
/// <remarks>
/// 所有可写数据统一落在 <c>%APPDATA%\qDesktop\</c>，不得写入程序安装目录
/// （依据 specs/2026-10-08-project-scaffold/requirements.md 5.1 第 5 条）。
/// </remarks>
internal static class AppPaths
{
    private const string ApplicationFolderName = "qDesktop";

    private const string LogFolderName = "logs";

    /// <summary>应用数据根目录：<c>%APPDATA%\qDesktop</c>。</summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ApplicationFolderName);

    /// <summary>
    /// 实际生效的日志目录；<see langword="null"/> 表示文件日志不可用（应用降级运行）。
    /// </summary>
    /// <remarks>
    /// 优先 <c>%APPDATA%\qDesktop\logs</c>；不可写时降级到 <c>%TEMP%\qDesktop\logs</c>；
    /// 两者均不可写时返回 <see langword="null"/>。结果在类型初始化时确定一次。
    /// </remarks>
    public static string? EffectiveLogDirectory { get; } = ResolveLogDirectory();

    private static string? ResolveLogDirectory()
    {
        string[] candidates =
        [
            Path.Combine(DataDirectory, LogFolderName),
            Path.Combine(Path.GetTempPath(), ApplicationFolderName, LogFolderName),
        ];

        foreach (var candidate in candidates)
        {
            // 仅接受绝对路径，避免在相对路径下误把日志写进程序目录。
            if (string.IsNullOrWhiteSpace(candidate) || !Path.IsPathRooted(candidate))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            catch (Exception ex) when (ex is IOException
                                           or UnauthorizedAccessException
                                           or NotSupportedException
                                           or ArgumentException)
            {
                // 该候选目录不可用，继续尝试下一个。
            }
        }

        return null;
    }
}
