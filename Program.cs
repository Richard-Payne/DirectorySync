using System.Linq;
using System;
using System.IO;
using CommandLine;
using System.Collections.Generic;

namespace fs_sync
{
    class Program
    {

        [Verb("new", HelpText = "Create a new sync set")]
        public class Options
        {           
            [Value(0)]
            public string PathA { get; set; }

            [Value(1)]
            public string PathB { get; set; }

            [Option(shortName: 'o', longName: "output")]
            public string StatusFile { get; set; }
        }

        private static Options options;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => options = o);

            if (!Directory.Exists(options.PathA)) throw new ArgumentException($"PathA ({options.PathA}) does not exist");
            if (!Directory.Exists(options.PathB)) throw new ArgumentException($"PathB ({options.PathB}) does not exist");

            SyncItem<FileInfo> CreateSyncItem(FileInfo fileInfo, string basePath) {
                string key = Path.GetRelativePath(basePath, fileInfo.FullName);
                return new SyncItem<FileInfo>(fileInfo.FullName, key, fileInfo);
            }

            var sourceFiles = new DirectoryParser().Parse(options.PathA)
                                .Select(fi => CreateSyncItem(fi, options.PathA))
                                .ToHashSet();
            var destFiles = new DirectoryParser().Parse(options.PathB)
                                .Select(fi => CreateSyncItem(fi,options.PathB))
                                .ToHashSet();

            var syncer = new Syncer<FileInfo>((a, b) => {
                Console.WriteLine($"Conflict Resolution: A ={a.Item.LastWriteTimeUtc.ToString()}, B = {b.Item.LastWriteTimeUtc.ToString()}");
                return a.Item.LastWriteTimeUtc > b.Item.LastWriteTimeUtc ? a : a.Item.LastWriteTimeUtc < b.Item.LastWriteTimeUtc ? b : null;
            });
            var changeset = syncer.GetChangeSet(sourceFiles, destFiles, new HashSet<SyncItem<FileInfo>>());            
            //syncer.Sync(changeset);

            foreach (var op in changeset) {
                Console.Write($"{op.Item.Id}\t");
                if (op.CopyToA) Console.Write("copyToA\t");
                if (op.CopyToB) Console.Write("copyToB\t");
                if (op.AddToStatus) Console.Write("addToStatus\t");
                if (op.DeleteFromA) Console.Write("deleteFromA\t");
                if (op.DeleteFromB) Console.Write("deleteFromB\t");
                if (op.DeleteFromStatus) Console.Write("deleteFromStatus\t");                
                Console.WriteLine(op.Reason);
            }
        }
    }
}
