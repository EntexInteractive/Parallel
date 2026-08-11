// Copyright 2026 Entex Interactive

using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Parallel.Cli.Utils;
using SQLitePCL;

namespace Parallel.Cli.Commands
{
    public class ZipCommand : Command
    {
        private readonly Argument<string> sourceArg = new("path", "The source path of files to zip.");
        private readonly Option<bool> keepOpt = new(["--keep", "-k"], "If the original files should be kept.");

        private Stopwatch _sw = new Stopwatch();
        private readonly List<Task> _tasks = new List<Task>();
        private int _totalTasks = 0;

        public ZipCommand() : base("zip", "Zips files in a directory.")
        {
            this.AddArgument(sourceArg);
            this.AddOption(keepOpt);
            this.SetHandler(async (path, keep) =>
            {
                _sw = Stopwatch.StartNew();
                CommandLine.WriteLine($"Scanning for files in {path}...", ConsoleColor.DarkGray);
                string[] files = Directory.EnumerateFiles(path, $"*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".gz")).ToArray();
                if (files.Length == 0)
                {
                    CommandLine.WriteLine("No files found to zip!", ConsoleColor.Yellow);
                    return;
                }

                CommandLine.WriteLine($"Zipping {files.Length:N0} files...", ConsoleColor.DarkGray);
                _totalTasks = files.Length;
                _tasks.AddRange(files.Select(file => Task.Run(() => CompressFile(file, keep))));
                await Task.WhenAll(_tasks);

                CommandLine.WriteLine($"Successfully zipped {files.Length:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
            }, sourceArg, keepOpt);
        }

        private void CompressFile(string path, bool keep)
        {
            if (File.Exists(path))
            {
                using (FileStream openFile = File.OpenRead(path))
                using (FileStream createFile = new FileStream($"{path}.gz", FileMode.OpenOrCreate))
                using (GZipStream gZip = new GZipStream(createFile, CompressionLevel.SmallestSize))
                {
                    openFile.CopyTo(gZip);
                }

                if (!keep)
                {
                    File.SetAttributes(path, ~FileAttributes.ReadOnly & File.GetAttributes(path));
                    File.Delete(path);
                }
            }

            CommandLine.ProgressBar(_tasks.Count(t => t.IsCompleted), _totalTasks, _sw.Elapsed);
        }
    }
}