using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Zsync_the_things
{
    class ZSyncMake
    {
        private readonly string _exePath;

        public HashSet<string> ExcludeFileNames = new HashSet<string> { "lock.lock", "serial" };

        public ZSyncMake(string zsyncMakeExePath)
        {
            _exePath = zsyncMakeExePath;
        }

        public void Make(string path)
        {
            if (Directory.Exists(path))
            {
                Console.WriteLine("Processing folder: {0}", path);
                CreateZsyncFiles(path.ToAbsoluteDirectoryPath());
            }
            else if (File.Exists(path))
                CreateZsyncFile(path.ToAbsoluteFilePath());
            else
                throw new FileNotFoundException();
        }

        private void CreateZsyncFiles(IAbsoluteDirectoryPath rootDirectory)
        {
            foreach (var path in Directory.EnumerateFiles(rootDirectory.ToString(), "*.*", SearchOption.AllDirectories)
                .Where(x => !x.EndsWith(".zsync", StringComparison.CurrentCultureIgnoreCase))
                .Select(x => x.ToAbsoluteFilePath())
                .Where(x => !ExcludeFileNames.Contains(x.FileName)))
            {
                CreateZsyncFile(path);
            }
        }

        private void CreateZsyncFile(IAbsoluteFilePath filePath)
        {
            var fileInfo = filePath.FileInfo;
            var absoluteFilePath = (fileInfo.FullName + ".zsync").ToAbsoluteFilePath();
            if (absoluteFilePath.Exists)
            {
                throw new Exception("an existing zsync file exists!");
            }

            var arg = (fileInfo.Length > 1048576L) ? string.Empty : "-b 128 ";
            var text = $"{arg}-Z -u \"{fileInfo.Name}\" -f \"{fileInfo.Name}\" \"{fileInfo.Name}\"";
            var startInfo = new ProcessStartInfo(_exePath, text)
            {
                WorkingDirectory = fileInfo.Directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var process = new Process();
            var result = process.RunInlineAndWait(startInfo);
            var output = process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
            if (result != 0)
            {
                throw new Exception($"ZsyncMake error: {result}\n{text}\n{output}");
            }
        }
    }
}
