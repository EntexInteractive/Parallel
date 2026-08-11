// Copyright 2026 Entex Interactive

using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using Parallel.Cli.Utils;

namespace Parallel.Cli.Commands
{
    public class UnzipCommand : Command
    {
        private readonly Argument<string> sourceArg = new("path", "The source path of files to unzip.");

        private Stopwatch? _sw;
        private readonly List<Task> _tasks = new List<Task>();
        private readonly int _totalTasks = 0;

        public UnzipCommand() : base("unzip", "Unzips files in a directory.")
        {
            this.AddArgument(sourceArg);
            this.SetHandler(HandleUnzipAsync, sourceArg);
        }

        private Task HandleUnzipAsync(string path)
        {
            _sw = Stopwatch.StartNew();
            CommandLine.WriteLine($"Scanning for files in {path}...", ConsoleColor.DarkGray);
            string[] files = Directory.GetFiles(path, $"*.gz", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                CommandLine.WriteLine("No files found to unzip!", ConsoleColor.Yellow);
                return Task.CompletedTask;
            }

            CommandLine.WriteLine($"Unzipping {files.Length:N0} files...", ConsoleColor.DarkGray);
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            System.Threading.Tasks.Parallel.ForEach(files, options, (file, ct) =>
            {
                using (FileStream openFile = File.OpenRead(file))
                using (FileStream createFile = new FileStream(file.Replace(".gz", string.Empty), FileMode.OpenOrCreate))
                using (GZipStream gZip = new GZipStream(openFile, CompressionMode.Decompress))
                {
                    gZip.CopyTo(createFile);
                }

                File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                File.Delete(file);
            });

            CommandLine.WriteLine($"Successfully unzipped {files.Length:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
            return Task.CompletedTask;
        }

        private void DecompressFile(string path, bool keep)
        {
            if (File.Exists(path))
            {
                using (FileStream openFile = File.OpenRead(path))
                using (FileStream createFile = new FileStream(path.Replace(".gz", string.Empty), FileMode.OpenOrCreate))
                using (GZipStream gZip = new GZipStream(openFile, CompressionMode.Decompress))
                {
                    gZip.CopyTo(createFile);
                }

                if (!keep)
                {
                    File.SetAttributes(path, ~FileAttributes.ReadOnly & File.GetAttributes(path));
                    File.Delete(path);
                }
            }

            CommandLine.ProgressBar(_tasks.Count(t => t.IsCompleted), _totalTasks, _sw?.Elapsed ?? TimeSpan.Zero);
        }
    }
}