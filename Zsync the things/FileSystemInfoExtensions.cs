using NDepend.Path;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Zsync_the_things
{
    public static class FileSystemInfoExtensions
    {
        public static void Rename(this FileSystemInfo item, string newName)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            FileInfo fileInfo = item as FileInfo;
            if (fileInfo != null)
            {
                fileInfo.Rename(newName);
                return;
            }

            DirectoryInfo directoryInfo = item as DirectoryInfo;
            if (directoryInfo != null)
            {
                directoryInfo.Rename(newName);
                return;
            }

            throw new ArgumentException("Item", "Unexpected subclass of FileSystemInfo " + item.GetType());
        }

        public static void Rename(this FileInfo file, string newName)
        {
            // Validate arguments.
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (newName == null)
            {
                throw new ArgumentNullException("newName");
            }
            else if (newName.Length == 0)
            {
                throw new ArgumentException("The name is empty.", "newName");
            }
            else if (newName.IndexOf(Path.DirectorySeparatorChar) >= 0
                || newName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                throw new ArgumentException("The name contains path separators. The file would be moved.", "newName");
            }

            // Rename file.
            string newPath = Path.Combine(file.DirectoryName, newName);
            file.MoveTo(newPath);
        }

        public static void Rename(this DirectoryInfo directory, string newName)
        {
            // Validate arguments.
            if (directory == null)
            {
                throw new ArgumentNullException("directory");
            }
            else if (newName == null)
            {
                throw new ArgumentNullException("newName");
            }
            else if (newName.Length == 0)
            {
                throw new ArgumentException("The name is empty.", "newName");
            }
            else if (newName.IndexOf(Path.DirectorySeparatorChar) >= 0
                || newName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                throw new ArgumentException("The name contains path separators. The directory would be moved.", "newName");
            }

            // Rename directory.
            string newPath = Path.Combine(directory.Parent.FullName, newName);
            directory.MoveTo(newPath);
        }
        public static string GetFileHash(this IPath filePath, HashType type = HashType.MD5)
        {
            return GetFileHash(filePath.ToString(), type);
        }

        public static string GetFileHash(this string filePath, HashType type = HashType.MD5)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            HashAlgorithm hasher;
            switch (type)
            {
                case HashType.SHA1:
                default:
                    hasher = SHA1.Create();
                    break;
                case HashType.SHA256:
                    hasher = SHA256.Create();
                    break;
                case HashType.SHA384:
                    hasher = SHA384.Create();
                    break;
                case HashType.SHA512:
                    hasher = SHA384.Create();
                    break;
                case HashType.MD5:
                    hasher = MD5.Create();
                    break;
                case HashType.RIPEMD160:
                    hasher = RIPEMD160.Create();
                    break;
            }
            try
            {
                using (var stream = File.OpenRead(filePath))
                    return hasher.ComputeHash(stream).ByteArrayToString();
            }
            finally
            {
                hasher.Dispose();
            }
        }
        public static string ByteArrayToString(this byte[] ba)
        {
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
