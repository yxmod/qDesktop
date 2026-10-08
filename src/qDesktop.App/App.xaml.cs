using System.Globalization;
using System.IO;
using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace qDesktop.App;

/// <summary>
/// 应用入口与组合根（Composition Root）。
/// </summary>
/// <remarks>
/// 职责：
/// <list type="number">
///   <item>装配 <see cref="IHost"/>：配置、依赖注入、日志；</item>
///   <item>在启动时从容器解析并显示主窗口；</item>
///   <item>在退出时停止宿主并冲刷日志缓冲。</item>
/// </list>
/// 阶段 0 仅装配基础设施，不含任何业务功能（见 requirements.md 第 2.2 节）。
/// </remarks>
public partial class App : Application
{
    /// <summary>日志滚动文件保留天数（对齐 specs/tech-stack.md 2.7）。</summary>
    private const int LogRetentionDays = 7;

    private const string LogOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}";

    private IHost? _host;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = BuildHost(e.Args);
        _host.Start();

        Log.Information(
            "qDesktop 启动完成（版本 {Version}，日志目录 {LogDirectory}）",
            typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown",
            AppPaths.EffectiveLogDirectory ?? "(未启用文件日志)");

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("qDesktop 正在退出（退出码 {ExitCode}）", e.ApplicationExitCode);

        if (_host is not null)
        {
            try
            {
                _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException ex)
            {
                Log.Warning(ex, "宿主在 5 秒内未能正常停止");
            }
            finally
            {
                _host.Dispose();
                _host = null;
            }
        }

        Log.CloseAndFlush();

        base.OnExit(e);
    }

    /// <summary>构建应用宿主：装载配置、装配日志与依赖注入容器。</summary>
    private static IHost BuildHost(string[] args)
    {
        // 内容根固定为可执行文件所在目录，保证 appsettings.json 与程序一同定位，
        // 不依赖启动时的工作目录（双击启动与 dotnet run 的 CWD 并不一致）。
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });

        var minimumLevel = ResolveMinimumLevel(builder.Configuration);
        ConfigureFileLogging(minimumLevel, AppPaths.EffectiveLogDirectory);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: false);

        // 阶段 0 仅注册主窗口；业务服务自阶段 1 起逐步注册。
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }

    /// <summary>
    /// 读取 <c>Serilog:MinimumLevel</c>；未配置或无法解析时回落为 <see cref="LogEventLevel.Information"/>。
    /// </summary>
    private static LogEventLevel ResolveMinimumLevel(ConfigurationManager configuration)
    {
        var configured = configuration["Serilog:MinimumLevel"];

        return Enum.TryParse<LogEventLevel>(configured, ignoreCase: true, out var level)
            ? level
            : LogEventLevel.Information;
    }

    /// <summary>
    /// 装配 Serilog 文件日志。日志目录不可写时降级为不写文件日志，应用仍可启动
    /// （边界行为见 specs/2026-10-08-project-scaffold/validation.md 6.1）。
    /// </summary>
    private static void ConfigureFileLogging(LogEventLevel minimumLevel, string? logDirectory)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext();

        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            loggerConfiguration.WriteTo.File(
                Path.Combine(logDirectory, "qDesktop-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: LogRetentionDays,
                outputTemplate: LogOutputTemplate,
                formatProvider: CultureInfo.InvariantCulture,
                shared: false);
        }

        Log.Logger = loggerConfiguration.CreateLogger();
    }
}
