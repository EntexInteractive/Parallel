// Copyright 2026 Entex Interactive

using System.CommandLine;
using System.Reflection;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli
{
    internal static class Program
    {
        private static readonly LogEventTracker EventTracker = new LogEventTracker();
        internal static ParallelConfig Settings = new ParallelConfig();

        public static async Task Main(string[] args)
        {
            if (!ParallelConfig.CanStartNewInstance())
            {
                CommandLine.WriteLine("An instance of Parallel is already running!", ConsoleColor.Yellow);
                return;
            }

            Settings = ParallelConfig.Load();
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
#if DEBUG
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
#else
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(PathBuilder.LogFile).CreateLogger();
#endif

            Log.Information($"{assembly.Name} [Version {assembly.Version}]");
            RootCommand rootCommand = new("Parallel file manager - Easily back up and synchronize massive amounts of files, save system states, and free up drive space.");
            IEnumerable<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(Command).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (Type type in types) rootCommand.AddCommand((Command)Activator.CreateInstance(type)!);
            //await File.WriteAllTextAsync(Path.Combine(PathBuilder.TempDirectory, "Command.md"), MarkdownGenerator.Generate(rootCommand));

            try
            {
                await rootCommand.InvokeAsync(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            finally
            {
                // Clean successful logs
                await Log.CloseAndFlushAsync();
                Settings.Save();
            }
        }
    }
}