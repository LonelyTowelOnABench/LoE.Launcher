using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NDepend.Path;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace Zsync_the_things.Commands
{
    public class DownloadCommand : BaseCommand
    {
        private IAbsoluteDirectoryPath _folder;
        private Uri _url;
        private bool _noCleanup;
        private static Version MinimumControlFileVersion = new Version(0, 2);

        public DownloadCommand() {
            IsCommand("download", "Downloads files via zsync");
            HasRequiredOption("out=", "The location of the folder", (s) => _folder = s.ToAbsoluteDirectoryPath());
            HasRequiredOption("url=", "The url of the main control file", (s) => _url = new Uri(s));
            HasOption("noCleanup", "Prevent Cleaning old zs files", (s) => _noCleanup = true);
        }

        public override int Run(string[] remainingArguments)
        {
            AsyncContext.Run(() => DoWork());
            Console.ReadLine();
            return 0;
        }

        private async Task DoWork()
        {
            string result = "";
            var toProcess = await DownloadControlFiles();
            await UpdateFiles(toProcess, 3);
            //if(!_noCleanup)
            Cleanup(toProcess);
        }

        private void Cleanup(DownloadData toProcess) {
            Console.WriteLine("Cleaning Up");
            foreach (var tuple in toProcess.ToProcess)
            {
                var oldZsFile = tuple.InstallPath.GetBrotherFileWithName(tuple.InstallPath + ".zs-old").GetAbsolutePathFrom(_folder);
                if(oldZsFile.Exists)
                    oldZsFile.FileInfo.Delete();
                var oldControlFile = tuple.InstallPath.GetBrotherFileWithName(tuple.InstallPath + ".zsync").GetAbsolutePathFrom(_folder);
                if (oldControlFile.Exists)
                    oldControlFile.FileInfo.Delete();
            }
        }

        private async Task UpdateFiles(DownloadData toProcess, int retries = 0)
        {
            int tries = 0;
            var queue = new Queue<ControlFileItem>(toProcess.ToProcess);
            while (tries <= retries)
            {
                tries++;
                var reProcess = new Queue<ControlFileItem>();

                while (queue.Any())
                {
                    var item = queue.Dequeue();
                    var uri = item.GetContentUri(toProcess.ControlFile).ToString().Substring(0, item.GetContentUri(toProcess.ControlFile).ToString().Length - 10);
                    Console.WriteLine("Processing: " + uri);

                    var arguments = "-u \"" + uri + "\" -o \"" + Path.GetFileNameWithoutExtension(item.InstallPath.FileName) + "\" -i \"" + Path.GetFileNameWithoutExtension(item.InstallPath.FileName) + "\" \"" +
                                       item.InstallPath.ToString() + "\"";
                    Console.WriteLine(arguments);
                    var resp = new Process().RunInlineAndWait(new ProcessStartInfo("bin\\zsync.exe",
                        arguments) {
                            UseShellExecute = false,
                            WorkingDirectory = item.InstallPath.ParentDirectoryPath.ToString()
                        });
                    if (resp != 0)
                    {
                        reProcess.Enqueue(item);
                    }
                }

                queue = reProcess;
                if (!reProcess.Any())
                    break;
                await Task.Delay(1000);

            }

            if (queue.Any())
            {
                Console.WriteLine("Failed to get all files!");
                throw new Exception("Failed to get all files!");
            }
        }

        private async Task<DownloadData> DownloadControlFiles()
        {
            string result;
            using (var client = new HttpClient())
            {
                result = await client.GetStringAsync(_url);
            }
            var data = new DownloadData(JsonConvert.DeserializeObject<MainControlFile>(result));
            if (data.ControlFile.Version.CompareTo(MinimumControlFileVersion) < 0 || data.ControlFile.Version.CompareTo(new MainControlFile().Version) > 0)
                throw new Exception("Control File Format not supported");

            Console.WriteLine("Downloading Control File");
            foreach (var item in data.ControlFile.Content)
            {
                using (var client = new HttpClient()) {
                    var childFileWithName = item.InstallPath.GetAbsolutePathFrom(_folder);
                    var realFile = item.GetUnzippedFileName().GetAbsolutePathFrom(_folder);

                    if (realFile.Exists &&
                        childFileWithName.ToString().GetFileHash(HashType.MD5) == item.FileHash)
                        continue;

                    if (realFile.Exists)
                    {
                        new Process().RunInlineAndWait(new ProcessStartInfo("bin\\gzip.exe",
                            "\"" + realFile + "\"") {
                                UseShellExecute = false,
                                WorkingDirectory = _folder.ParentDirectoryPath.ToString()
                            });
                        realFile.FileInfo.Rename(realFile.FileNameWithoutExtension + ".jar");
                    }

                    data.ToProcess.Add(item);

                    var str = await client.GetByteArrayAsync(item.GetContentUri(data.ControlFile));
                    Directory.CreateDirectory(childFileWithName.ParentDirectoryPath.ToString());
                    File.WriteAllBytes(childFileWithName.GetBrotherFileWithName(Path.GetFileNameWithoutExtension(childFileWithName.FileName)).ToString(), str);
                }
            }
            return data;
        }
    }

    public class DownloadData
    {
        public MainControlFile ControlFile { get; private set; }
        public List<ControlFileItem> ToProcess { get; set; }

        public DownloadData(MainControlFile controlFile)
        {
            ControlFile = controlFile;
            ToProcess = new List<ControlFileItem>();
        }
    }
}