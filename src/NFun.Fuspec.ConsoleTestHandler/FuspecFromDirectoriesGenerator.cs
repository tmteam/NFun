using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NFun.Fuspec.ConsoleTestHandler
{
    public static class FuspecFromDirectoriesGenerator
    {
        private const string FileExtension = "*.fuspec";

        public static string TryGetJoinedString()
        {
            var files = GetFilesWithExtension();
            var testsString = files.Select(File.ReadAllText).ToList();

            return string.Join(string.Empty, testsString);
        }

        private static IEnumerable<string> GetFilesWithExtension()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var files = Directory.EnumerateFiles(currentDirectory, FileExtension, SearchOption.AllDirectories).ToList();

            if (!files.Any())
                throw new ArgumentException($"No files with {nameof(FileExtension)} extension {FileExtension}");

            return files;
        }
    }
}