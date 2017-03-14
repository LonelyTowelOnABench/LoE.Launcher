using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDepend.Path;
using Newtonsoft.Json;

namespace Zsync_the_things.Commands
{
    public class CreateCommand : BaseCommand
    {
        private IAbsoluteDirectoryPath _folder;
        private IAbsoluteFilePath _file;
        private string _url;
        private IAbsoluteDirectoryPath _tools = @"C:\cygwin\bin".ToAbsoluteDirectoryPath();

        public CreateCommand()
        {
            IsCommand("create", "Create a Zsync Download Control File");
            HasRequiredOption("in|f|folder=", "The folder that you will make a control for", (s) => _folder = s.ToAbsoluteDirectoryPath());
            //HasRequiredOption("out=", "The location of the control file", (s) => _file = s.ToAbsoluteFilePath());
            HasRequiredOption("url=", "The root url of the download", (s) => _url = s);

            HasOption("tools=", "The folder to get gzip and zsyncmake from. Defaults to C:\\cygwin\\bin", s => _tools = s.ToAbsoluteDirectoryPath());
        }

        public override int Run(string[] remainingArguments)
        {
            var cf = new MainControlFile()
            {
                Content = ListAllItems().ToList()
            };
            Console.WriteLine($"Generated zsync controls for {cf.Content.Count} files");
            File.WriteAllText(_folder.GetChildFileWithName(".zsync-control.jar").ToString(), JsonConvert.SerializeObject(cf, Formatting.Indented));
            return 0;
        }

        private IEnumerable<ControlFileItem> ListAllItems()
        {
            var originalHashes = new Dictionary<string, string>();
            Console.Write("generating md5 hashes...");
            foreach (var file in _folder.DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var fileHash = file.FullName.GetFileHash();
                originalHashes.Add(file.FullName + ".jar.zsync.jar", fileHash);
            }
            Console.WriteLine($"\b\b\b - {originalHashes.Count} files");
            GZipFiles();
            ZSyncMake();
            foreach (var file in _folder.DirectoryInfo.EnumerateFiles("*.jar.zsync", SearchOption.AllDirectories)) {
                file.MoveTo(file.Directory.FullName.ToAbsoluteDirectoryPath().GetChildFileWithName(file.Name + ".jar").ToString());
            }
            return _folder.DirectoryInfo.EnumerateFiles("*.zsync.jar", SearchOption.AllDirectories).Select(file =>
            {
                var substring = file.FullName.Substring(_folder.ToString().Length);
                var baseUri = new Uri(_url);
                var contentUrl = new Uri(baseUri, substring.Replace("\\", "/")
                                             .Replace(" ", "%20"));
                var relativeFilePath = file.FullName.ToAbsoluteFilePath().GetRelativePathFrom(_folder);
                var originalHash = originalHashes[file.FullName];
                return new ControlFileItem()
                {
                    RelativeContentUrl = baseUri.MakeRelativeUri(contentUrl),
                    InstallPath = relativeFilePath,
                    FileHash = originalHash
                };
            });
        }

        private void ZSyncMake()
        {
            Console.WriteLine("generating zsync control files");
            var zsync = new ZSyncMake(_tools.GetChildFileWithName("zsyncmake.exe").ToString());
            zsync.Make(_folder.ToString());
        }

        private void GZipFiles()
        {
            DoTheZipping();
            DoTheRenaming();
        }

        private void DoTheRenaming()
        {
            Console.Write("renaming all .gz to .jar...");
            int count = 0;
            foreach (var file in _folder.DirectoryInfo.EnumerateFiles("*.gz",SearchOption.AllDirectories))
            {
                file.MoveTo(file.Directory.FullName.ToAbsoluteDirectoryPath().GetChildFileWithName(Path.GetFileNameWithoutExtension(file.Name) + ".jar").ToString());
                count++;
            }
            Console.WriteLine($"\b\b\b - {count} files");
        }

        private void DoTheZipping()
        {
            Console.WriteLine("gzipping files...");
            new Process().RunInlineAndWait(new ProcessStartInfo(_tools.GetChildFileWithName("gzip.exe").ToString(),
                "-r \"" + _folder.DirectoryName + "\"") {
                    UseShellExecute = false,
                    WorkingDirectory = _folder.ParentDirectoryPath.ToString()
                });
        }
    }
}