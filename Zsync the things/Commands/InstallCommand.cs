using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NDepend.Path;
using Nito.AsyncEx;

namespace Zsync_the_things.Commands
{
    public class InstallCommand : BaseCommand
    {
        private IAbsoluteDirectoryPath _folder;

        public InstallCommand() {
            IsCommand("install", "Installs Downloaded Game");
            HasAdditionalArguments(1, "Folder to Install");
        }

        public override int Run(string[] remainingArguments) {
            AsyncContext.Run(() => DoWork(remainingArguments[0].ToAbsoluteDirectoryPath()));

            return 0;
        }

        private async Task DoWork(IAbsoluteDirectoryPath installFolder) {
            Console.WriteLine("Uncompressing...");
            foreach (var file in installFolder.DirectoryInfo.EnumerateFiles("*.jar", SearchOption.AllDirectories))
            {
                file.Rename(Path.GetFileNameWithoutExtension(file.Name) + ".gz");
            }
            new Process().RunInlineAndWait(new ProcessStartInfo("bin\\gunzip.exe",
                "-r \"" + installFolder.DirectoryName + "\"") {
                    UseShellExecute = false,
                    WorkingDirectory = installFolder.ParentDirectoryPath.ToString()
                });
            Console.WriteLine("Cleaning Directory");
            foreach (var file in installFolder.DirectoryInfo.EnumerateFiles("*.zsync", SearchOption.AllDirectories)) {
                file.Delete();
            }
            foreach (var file in installFolder.DirectoryInfo.EnumerateFiles("*.zs-old", SearchOption.AllDirectories)) {
                file.Delete();
            }
        }
    }
}