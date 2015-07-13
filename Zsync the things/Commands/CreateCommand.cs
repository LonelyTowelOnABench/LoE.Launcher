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

        public CreateCommand()
        {
            IsCommand("create", "Create a Zsync Download Control File");
            HasRequiredOption("in|f|folder=", "The folder that you will make a control for", (s) => _folder = s.ToAbsoluteDirectoryPath());
            //HasRequiredOption("out=", "The location of the control file", (s) => _file = s.ToAbsoluteFilePath());
            HasRequiredOption("url=", "The root url of the download", (s) => _url = s);
        }

        public override int Run(string[] remainingArguments)
        {
            var cf = new MainControlFile()
            {
                Content = ListAllItems().ToList()
            };
            File.WriteAllText(_folder.GetChildFileWithName(".zsync-control.jar").ToString(), JsonConvert.SerializeObject(cf, Formatting.Indented));
            return 0;
        }

        private IEnumerable<ControlFileItem> ListAllItems()
        {
            var originalHashes = new Dictionary<string, string>();
            foreach (var file in _folder.DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var fileHash = file.FullName.GetFileHash(HashType.MD5);
                originalHashes.Add(file.FullName + ".jar.zsync.jar", fileHash);
                Console.WriteLine(file.FullName + ".jar.zsync.jar " + fileHash);
            }
            GZipFiles();
            ZSyncMake();
            foreach (var file in _folder.DirectoryInfo.EnumerateFiles("*.jar.zsync", SearchOption.AllDirectories)) {
                file.MoveTo(file.Directory.FullName.ToAbsoluteDirectoryPath().GetChildFileWithName(file.Name + ".jar").ToString());
            }
            return _folder.DirectoryInfo.EnumerateFiles("*.zsync.jar", SearchOption.AllDirectories).Select(file =>
            {
                var substring = file.FullName.Substring(_folder.ToString().Length);
                var contentUrl = new Uri(_url +
                                         substring
                                             .Replace("\\", "/")
                                             .Replace(" ", "%20"));
                Console.WriteLine(file.FullName);
                var relativeFilePath = file.FullName.ToAbsoluteFilePath().GetRelativePathFrom(_folder);
                var originalHash = originalHashes[file.FullName];
                return new ControlFileItem()
                {
                    ContentUrl =
                        contentUrl,
                    InstallPath = relativeFilePath,
                    FileHash = originalHash
                };
            });
        }

        private void ZSyncMake()
        {
            new Process().RunInlineAndWait(new ProcessStartInfo("bin\\synq\\synq.exe",
                "zsyncmake \"" + _folder.ToString() + "\"") {
                    UseShellExecute = false
                });
        }

        private void GZipFiles()
        {
            DoTheZipping();
            DoTheRenaming();
        }

        private void DoTheRenaming()
        {
            foreach (var file in _folder.DirectoryInfo.EnumerateFiles("*.gz",SearchOption.AllDirectories))
            {
                file.MoveTo(file.Directory.FullName.ToAbsoluteDirectoryPath().GetChildFileWithName(Path.GetFileNameWithoutExtension(file.Name) + ".jar").ToString());
            }
        }

        private void DoTheZipping()
        {
            new Process().RunInlineAndWait(new ProcessStartInfo("bin\\gzip.exe",
                "-r \"" + _folder.DirectoryName + "\"") {
                    UseShellExecute = false,
                    WorkingDirectory = _folder.ParentDirectoryPath.ToString()
                });
        }
    }
}