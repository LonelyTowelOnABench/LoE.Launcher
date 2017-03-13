﻿using System;
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
    public static class FileSystemInfoExtensions
    {
        public static void Rename(this FileSystemInfo item, string newName) {
            if (item == null) {
                throw new ArgumentNullException("item");
            }

            FileInfo fileInfo = item as FileInfo;
            if (fileInfo != null) {
                fileInfo.Rename(newName);
                return;
            }

            DirectoryInfo directoryInfo = item as DirectoryInfo;
            if (directoryInfo != null) {
                directoryInfo.Rename(newName);
                return;
            }

            throw new ArgumentException("Item", "Unexpected subclass of FileSystemInfo " + item.GetType());
        }

        public static void Rename(this FileInfo file, string newName) {
            // Validate arguments.
            if (file == null) {
                throw new ArgumentNullException("file");
            } else if (newName == null) {
                throw new ArgumentNullException("newName");
            } else if (newName.Length == 0) {
                throw new ArgumentException("The name is empty.", "newName");
            } else if (newName.IndexOf(Path.DirectorySeparatorChar) >= 0
                  || newName.IndexOf(Path.AltDirectorySeparatorChar) >= 0) {
                throw new ArgumentException("The name contains path separators. The file would be moved.", "newName");
            }

            // Rename file.
            string newPath = Path.Combine(file.DirectoryName, newName);
            file.MoveTo(newPath);
        }

        public static void Rename(this DirectoryInfo directory, string newName) {
            // Validate arguments.
            if (directory == null) {
                throw new ArgumentNullException("directory");
            } else if (newName == null) {
                throw new ArgumentNullException("newName");
            } else if (newName.Length == 0) {
                throw new ArgumentException("The name is empty.", "newName");
            } else if (newName.IndexOf(Path.DirectorySeparatorChar) >= 0
                  || newName.IndexOf(Path.AltDirectorySeparatorChar) >= 0) {
                throw new ArgumentException("The name contains path separators. The directory would be moved.", "newName");
            }

            // Rename directory.
            string newPath = Path.Combine(directory.Parent.FullName, newName);
            directory.MoveTo(newPath);
        }
        public static string GetFileHash(this string filePath, HashType type = HashType.MD5) {
            if (!File.Exists(filePath))
                return string.Empty;

            if (type != HashType.MD5)
                throw new ArgumentException("only HashType.MD5 is supported", nameof(type));


            using (var md5 = MD5.Create()) {
                using (var stream = File.OpenRead(filePath)) {
                    return md5.ComputeHash(stream).ByteArrayToString();
                }
            }
        }
        public static string ByteArrayToString(this byte[] ba) {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
    public enum HashType
    {
        [Description("SHA-1")]
        SHA1,
        [Description("SHA-256")]
        SHA256,
        [Description("SHA-384")]
        SHA384,
        [Description("SHA-512")]
        SHA512,
        [Description("MD5")]
        MD5,
        [Description("RIPEMD-160")]
        RIPEMD160

    }
}