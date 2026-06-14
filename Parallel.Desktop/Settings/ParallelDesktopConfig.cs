// Copyright 2026 Entex Interactive

using System.IO;
using Parallel.Core.IO;
using Parallel.Core.Settings;

namespace Parallel.Desktop.Settings
{
    public class ParallelDesktopConfig : ParallelConfig
    {
        private static string ConfigFile { get; } = Path.Combine(PathBuilder.ProgramData, "Desktop-Configuration.json");

        public new static ParallelDesktopConfig Load()
        {
            Log.Debug($"Loading config file: {ConfigFile}");
            if (!File.Exists(ConfigFile)) return new ParallelDesktopConfig();

            string json = File.ReadAllText(ConfigFile);
            ParallelDesktopConfig? config = JsonConvert.DeserializeObject<ParallelDesktopConfig>(json);
            return config ?? new ParallelDesktopConfig();
        }

        /// <summary>
        /// Saves settings to a file.
        /// </summary>
        public new void Save()
        {
            Log.Debug($"Saving config file: {ConfigFile}");
            if (!Directory.Exists(PathBuilder.ProgramData)) Directory.CreateDirectory(PathBuilder.ProgramData);
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}