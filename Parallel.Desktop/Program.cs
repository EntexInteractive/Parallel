// Copyright 2026 Entex Interactive

using Avalonia;
using System;
using System.IO;
using System.Reflection;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Desktop.Settings;

namespace Parallel.Desktop
{
    sealed class Program
    {
        internal static ParallelDesktopConfig Settings = new ParallelDesktopConfig();

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();

            string logDir = Path.Combine(PathBuilder.TempDirectory, "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(Path.Combine(logDir, $"{assembly.Name}.log")).WriteTo.Console().CreateLogger();
            Log.Information($"{assembly.Name} [Version {assembly.Version}]");

            if (!ParallelConfig.CanStartNewInstance())
            {
                Log.Warning("Can't start new instance of Parallel!");

                //MessageBoxManager.GetMessageBoxStandard("Warning", "An instance of Parallel is already running!", ButtonEnum.Ok, Icon.Warning);
                return;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Settings = ParallelDesktopConfig.Load();
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
        }
    }
}