// Copyright 2026 Entex Interactive

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Parallel.Cli.Utils
{
    public abstract class TextWriter
    {
        public static string CreateTxtFile(string text)
        {
            string parentDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Parallel");
            if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

            string fileName = Path.Combine(parentDir, DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss") + ".json");
            File.WriteAllText(fileName, text);
            return fileName;
        }

        public static string CreateTxtFile(params string[] lines)
        {
            string parentDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Parallel");
            if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

            string fileName = Path.Combine(parentDir, DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss") + ".txt");
            File.WriteAllLines(fileName, lines);
            return fileName;
        }

        public static string CreateJsonFile(params string[] lines)
        {
            string parentDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Parallel");
            if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

            string fileName = Path.Combine(parentDir, DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss") + ".json");
            JArray json = JArray.FromObject(lines);

            File.WriteAllText(fileName, JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented));
            return fileName;
        }

        public static string CreateJsonFile(JArray json)
        {
            string parentDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Parallel");
            if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

            string fileName = Path.Combine(parentDir, DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss") + ".json");
            File.WriteAllText(fileName, JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented));
            return fileName;
        }
    }
}