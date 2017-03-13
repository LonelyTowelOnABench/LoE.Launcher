using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NDepend.Path;

namespace Zsync_the_things
{
    public static class Extensions
    {
        public static int RunInlineAndWait(this Process p, ProcessStartInfo startInfo)
        {
            p.StartInfo = startInfo;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();
            return p.ExitCode;
        }

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(this string commandLine) {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++) {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            } finally {
                Marshal.FreeHGlobal(argv);
            }
        }

        public static IRelativeFilePath GetUnzippedFileName(this ControlFileItem item)
        {
            return item.InstallPath.GetBrotherFileWithName(Path.GetFileNameWithoutExtension(item.InstallPath.FileNameWithoutExtension));
        }

        public static Uri GetContentUri(this ControlFileItem item, MainControlFile controlData)
        {
            return new Uri(controlData.RootUri, item.RelativeContentUrl);
        }
    }
}
