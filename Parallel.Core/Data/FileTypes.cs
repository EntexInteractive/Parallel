// Copyright 2026 Entex Interactive

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parallel.Core.Data
{
    /// <summary>
    /// The type of file.
    /// </summary>
    public enum FileCategory
    {
        Document,
        Photo,
        Music,
        Video,
        Other
    };

    /// <summary>
    /// Represents file types. This class cannot be inherited.
    /// </summary>
    public static class FileTypes
    {
        /// <summary>
        /// Gets the category of a file based off the extension.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static FileCategory GetFileCategory(string extension)
        {
            return extension.ToLower() switch
            {
                // Documents
                ".asp" => FileCategory.Document,
                ".aspx" => FileCategory.Document,
                ".bak" => FileCategory.Document,
                ".c" => FileCategory.Document,
                ".cab" => FileCategory.Document,
                ".cer" => FileCategory.Document,
                ".cfg" => FileCategory.Document,
                ".cfm" => FileCategory.Document,
                ".cgi" => FileCategory.Document,
                ".class" => FileCategory.Document,
                ".cpl" => FileCategory.Document,
                ".cpp" => FileCategory.Document,
                ".cs" => FileCategory.Document,
                ".css" => FileCategory.Document,
                ".csv" => FileCategory.Document,
                ".cur" => FileCategory.Document,
                ".dat" => FileCategory.Document,
                ".db" => FileCategory.Document,
                ".dbf" => FileCategory.Document,
                ".dll" => FileCategory.Document,
                ".dmp" => FileCategory.Document,
                ".doc" => FileCategory.Document,
                ".docx" => FileCategory.Document,
                ".drv" => FileCategory.Document,
                ".fnt" => FileCategory.Document,
                ".fon" => FileCategory.Document,
                ".h" => FileCategory.Document,
                ".htm" => FileCategory.Document,
                ".html" => FileCategory.Document,
                ".icns" => FileCategory.Document,
                ".ico" => FileCategory.Document,
                ".ini" => FileCategory.Document,
                ".lnk" => FileCategory.Document,
                ".java" => FileCategory.Document,
                ".jar" => FileCategory.Document,
                ".js" => FileCategory.Document,
                ".json" => FileCategory.Document,
                ".jsp" => FileCategory.Document,
                ".log" => FileCategory.Document,
                ".mdb" => FileCategory.Document,
                ".msi" => FileCategory.Document,
                ".odt" => FileCategory.Document,
                ".otf" => FileCategory.Document,
                ".part" => FileCategory.Document,
                ".pdf" => FileCategory.Document,
                ".php" => FileCategory.Document,
                ".pl" => FileCategory.Document,
                ".ppt" => FileCategory.Document,
                ".pptx" => FileCategory.Document,
                ".py" => FileCategory.Document,
                ".rss" => FileCategory.Document,
                ".sav" => FileCategory.Document,
                ".sh" => FileCategory.Document,
                ".sql" => FileCategory.Document,
                ".swift" => FileCategory.Document,
                ".sys" => FileCategory.Document,
                ".tar" => FileCategory.Document,
                ".tmp" => FileCategory.Document,
                ".ttf" => FileCategory.Document,
                ".txt" => FileCategory.Document,
                ".vb" => FileCategory.Document,
                ".xhtml" => FileCategory.Document,
                ".xls" => FileCategory.Document,
                ".xlsx" => FileCategory.Document,
                ".xml" => FileCategory.Document,
                ".zip" => FileCategory.Document,

                // Photos
                ".tif" => FileCategory.Photo,
                ".tiff" => FileCategory.Photo,
                ".bmp" => FileCategory.Photo,
                ".jpg" => FileCategory.Photo,
                ".jpeg" => FileCategory.Photo,
                ".png" => FileCategory.Photo,
                ".eps" => FileCategory.Photo,
                ".raw" => FileCategory.Photo,
                ".arw" => FileCategory.Photo,
                ".svg" => FileCategory.Photo,

                // Music
                ".m4a" => FileCategory.Music,
                ".flac" => FileCategory.Music,
                ".mp3" => FileCategory.Music,
                ".wav" => FileCategory.Music,
                ".wma" => FileCategory.Music,
                ".aac" => FileCategory.Music,

                // Video
                ".mp4" => FileCategory.Video,
                ".mov" => FileCategory.Video,
                ".wmv" => FileCategory.Video,
                ".avi" => FileCategory.Video,
                ".avchd" => FileCategory.Video,
                ".flv" => FileCategory.Video,
                ".f4v" => FileCategory.Video,
                ".webm" => FileCategory.Video,

                _ => FileCategory.Other
            };
        }

        public static FileCategory FromString(string type)
        {
            return type switch
            {
                "Document" => FileCategory.Document,
                "Photo" => FileCategory.Photo,
                "Music" => FileCategory.Music,
                "Video" => FileCategory.Video,

                _ => FileCategory.Other
            };
        }

        public static string ToString(FileCategory type)
        {
            return type switch
            {
                FileCategory.Document => "Document",
                FileCategory.Photo => "Photo",
                FileCategory.Music => "Music",
                FileCategory.Video => "Video",

                _ => "Other"
            };
        }
    }
}