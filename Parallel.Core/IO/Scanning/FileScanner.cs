// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using FileInfo = System.IO.FileInfo;

namespace Parallel.Core.IO.Scanning
{
    /// <summary>
    /// Represents file system scanning.
    /// </summary>
    public class FileScanner
    {
        private static readonly Dictionary<string, Regex> _cache = new();
        private readonly RemoteVaultConfig? _config;
        private readonly IDatabase? _db;

        public FileScanner(ISyncManager syncManager)
        {
            _config = syncManager.RemoteVault;
            _db = syncManager.Database;
        }

        /// <summary>
        /// Scans for file changes in a directory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ignoreFolders"></param>
        /// <param name="force"></param>
        /// <returns>A list of files that have changed since the last backup.</returns>
        public async Task<LocalFile[]> GetFileChangesAsync(string path, string[] ignoreFolders, bool force)
        {
            if (!Directory.Exists(path)) return Array.Empty<LocalFile>();

            ConcurrentBag<string> scannedFiles = new();
            ConcurrentBag<LocalFile> changedFiles = new();

            HashSet<string> localFiles = FileScanner.GetFiles(path, ignoreFolders, ".").ToHashSet();
            IEnumerable<LocalFile> remoteFiles = _db is null ? [] : await _db.GetLatestFilesAsync(path, DateTime.UtcNow.AddHours(1), false);
            System.Threading.Tasks.Parallel.ForEach(remoteFiles, ParallelConfig.Options, (remoteFile) =>
            {
                if (File.Exists(remoteFile.Fullname))
                {
                    LocalFile localFile = new LocalFile(remoteFile.Fullname);
                    if (IsIgnored(localFile.Fullname, ignoreFolders))
                    {
                        Log.Debug($"Ignored -> {localFile.Fullname}");
                        localFile.Deleted = true;
                        changedFiles.Add(localFile);
                    }
                    else if (HasChanged(localFile, remoteFile) || force)
                    {
                        Log.Debug($"Changed -> {localFile.Fullname}");
                        changedFiles.Add(localFile);
                    }

                    scannedFiles.Add(localFile.Fullname);
                }
                else
                {
                    Log.Debug($"Deleted -> {remoteFile.Fullname}");
                    remoteFile.Deleted = true;
                    changedFiles.Add(remoteFile);
                }
            });

            // Adds the remaining files as created
            IEnumerable<string> remainingFiles = localFiles.Except(scannedFiles);
            System.Threading.Tasks.Parallel.ForEach(remainingFiles, ParallelConfig.Options, (filePath) =>
            {
                if (IsIgnored(filePath, ignoreFolders)) return;
                Log.Debug($"Created -> {filePath}");
                changedFiles.Add(new LocalFile(filePath));
            });

            Log.Information($"Found {changedFiles.Count:N0} changes in '{path}'");
            return changedFiles.ToArray();
        }

        /// <summary>
        /// Gets if a file has changed.
        /// </summary>
        /// <param name="source">The source file to compare.</param>
        /// <param name="target">The target file to compare to.</param>
        /// <returns>True is success, otherwise false.</returns>
        public static bool HasChanged(LocalFile source, LocalFile? target)
        {
            if (target is null || source.LastWrite.TotalMilliseconds <= target.LastWrite.TotalMilliseconds) return false;
            if (!source.TryGenerateCheckSums() || !target.TryGenerateCheckSums()) return false;
            return source.LocalCheckSum != target.LocalCheckSum;
        }

        public static bool IsSameFile(LocalFile source, LocalFile? target)
        {
            if (target is null) return false;
            if (!source.TryGenerateCheckSums() || !target.TryGenerateCheckSums()) return false;
            return source.LocalCheckSum == target.LocalCheckSum;
        }

        /// <summary>
        /// Gets the total size, in bytes, of a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        public static long GetDirectorySize(string path)
        {
            long size = 0;
            DirectoryInfo di = new(path);
            EnumerationOptions options = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            };

            foreach (FileInfo fi in di.EnumerateFiles("*", options))
            {
                size += fi.Length;
            }

            return size;
        }

        /// <summary>
        /// Gets an array of empty directories.
        /// </summary>
        /// <param name="path">The root directory to search.</param>
        /// <param name="recursive">If it should search recursively.</param>
        /// <returns>An array of empty directories.</returns>
        public static IEnumerable<DirectoryInfo> GetEmptyDirectories(string path, bool recursive = true)
        {
            List<DirectoryInfo> list = new();
            DirectoryInfo directory = new(path);
            EnumerationOptions options = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recursive
            };

            foreach (DirectoryInfo di in directory.EnumerateDirectories("*", options))
            {
                IEnumerable<FileInfo> files = di.EnumerateFiles("*", options);
                Log.Debug($"{files.Count()} files: {di.FullName}");
                if (!files.Any()) list.Add(di);
            }

            Log.Debug($"Found {list.Count} empty directories");
            return list.OrderByDescending(d => d.FullName.Count(c => c == Path.DirectorySeparatorChar)).ToArray();
        }

        /// <summary>
        /// Gets an array of directories older than a specified time.
        /// <para>This includes all directories outside of Parallel's back up directories.</para>
        /// </summary>
        /// <param name="path">The root directory to search.</param>
        /// <param name="start">The time, as a <see cref="UnixTime"/>.</param>
        /// <param name="recursive">If it should search recursively.</param>
        /// <returns>An array of directories in order of oldest first.</returns>
        public static IEnumerable<DirectoryInfo> GetCleanableDirectories(string path, UnixTime start, bool recursive = true)
        {
            Dictionary<DirectoryInfo, DateTime> list = new();
            DirectoryInfo directory = new(path);
            EnumerationOptions options = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recursive
            };

            foreach (DirectoryInfo di in directory.EnumerateDirectories("*", options))
            {
                DateTime compare = di.CreationTime > di.LastWriteTime ? di.CreationTime : di.LastWriteTime;
                bool older = start.TotalMilliseconds >= new UnixTime(compare).TotalMilliseconds;
                bool exists = list.Keys.Any(d => di.FullName.StartsWith(d.FullName));
                if (!exists && older) list.Add(di, compare);
            }

            Log.Debug($"Found {list.Count} cleanable directories");
            return list.OrderBy(d => d.Value).ToDictionary().Keys;
        }

        public static IEnumerable<FileInfo> GetCleanableFiles(string path, UnixTime start, bool recursive = true)
        {
            Dictionary<FileInfo, DateTime> list = new();
            Log.Debug($"Searching '{path}' for files older than {start.ToString("g")}");
            foreach (string file in GetFiles(path, [], "*", recursive))
            {
                FileInfo fi = new FileInfo(file);
                DateTime compare = fi.CreationTime > fi.LastWriteTime ? fi.CreationTime : fi.LastWriteTime;
                bool older = start.TotalMilliseconds >= new UnixTime(compare).TotalMilliseconds;
                if (older) list.Add(fi, compare);
            }

            Log.Debug($"Found {list.Count} cleanable files");
            return list.OrderBy(d => d.Value).ToDictionary().Keys;
        }

        public static IEnumerable<string> GetFiles(string root, string searchPattern)
        {
            return GetFiles(root, [], searchPattern);
        }

        public static IEnumerable<string> GetFiles(string root, string[] exempt, string searchPattern = "*", bool recursive = true)
        {
            Stack<string> pending = new();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string current = pending.Pop();
                if (IsIgnored(current, exempt))
                {
                    Log.Debug($"Ignored -> {current}");
                    continue;
                }

                // Get files in current directory
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(current, searchPattern);
                }
                catch
                {
                    Log.Warning($"No file access -> {current}");
                    continue;
                }

                foreach (string file in files)
                {
                    // This is a better system. However, if a user adds something to ignore, with this code it will never mark it for deletion.
                    // if (IsIgnored(file, exempt))
                    // {
                    //     Log.Debug($"Ignored -> {file}");
                    //     continue;
                    // }

                    yield return file;
                }

                if (!recursive) continue;
                IEnumerable<string> subDirs;
                try
                {
                    subDirs = Directory.EnumerateDirectories(current);
                }
                catch
                {
                    Log.Warning($"No directory access -> {current}");
                    continue;
                }

                foreach (string dir in subDirs) pending.Push(dir);
            }
        }


        /// <summary>
        /// Scans a directory for duplicate files with the same name and size.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>A <see cref="KeyValuePair{TKey,TValue}"/> array of duplicate files, in order of most duplicate entries, where the key refers to the filename, and the values are an array of <see cref="LocalFile"/>s in order of oldest to newest.</returns>
        public static Dictionary<string, LocalFile[]> GetDuplicateFiles(string path)
        {
            ConcurrentDictionary<string, List<LocalFile>> dict = new();
            IEnumerable<string> files = GetFiles(path, "*");
            System.Threading.Tasks.Parallel.ForEach(files, ParallelConfig.Options, file =>
            {
                LocalFile entry = new(file);
                dict.AddOrUpdate(entry.Name, _ => [entry], (k, v) =>
                {
                    lock (v)
                    {
                        LocalFile? key = v.FirstOrDefault();
                        if (IsSameFile(entry, key)) v.Add(entry);
                    }

                    return v;
                });
            });

            return dict.Where(kv => kv.Value.Count > 1).OrderByDescending(kv => kv.Value.Count).ToDictionary(k => k.Key, v => v.Value.OrderBy(l => l.LastWrite.TotalMilliseconds).ToArray());
        }

        /// <summary>
        /// Checks if the given path is a directory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if path is a directory, otherwise false.</returns>
        public static bool IsDirectory(string path)
        {
            return Directory.Exists(path) && !File.Exists(path);
        }

        /// <summary>
        /// Checks if the given path is a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if path is a file, otherwise false.</returns>
        public static bool IsFile(string path)
        {
            return !Directory.Exists(path) && File.Exists(path);
        }

        /// <summary>
        /// Checks if the given path is set to be ignored.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="exempt"></param>
        /// <returns>True if ignored, otherwise false.</returns>
        public static bool IsIgnored(string path, string[] exempt)
        {
            string[] dirs = path.Split(Path.DirectorySeparatorChar);
            string fileName = Path.GetFileName(path);
            string extension = Path.GetExtension(path);

            foreach (string entry in exempt)
            {
                if (path.StartsWith(entry, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (entry.EndsWith('/'))
                {
                    string dirName = entry.TrimEnd('/');
                    if (dirs.Any(d => d.Equals(dirName, StringComparison.OrdinalIgnoreCase))) return true;
                }
                else if (entry.StartsWith("*"))
                {
                    if (extension.Equals(entry.Substring(1), StringComparison.OrdinalIgnoreCase)) return true;
                }
                else
                {
                    if (fileName.Equals(entry, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }

            return false;
        }
    }
}