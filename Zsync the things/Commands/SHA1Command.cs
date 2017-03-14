using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zsync_the_things.Commands
{
    public class SHA1Command : BaseCommand
    {
        private IAbsoluteFilePath _file;

        public SHA1Command()
        {
            IsCommand("sha1", "gets the sha1 hash of the specified file");
            HasAdditionalArguments(1, "file to get sha1 of");
        }

        public override int Run(string[] remainingArguments)
        {
            var hash = remainingArguments[0].ToAbsoluteFilePath().GetFileHash(HashType.SHA1);

            Console.WriteLine(hash);
            return 0;
        }
    }
}
