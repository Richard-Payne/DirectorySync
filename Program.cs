using System.Linq;
using System;
using System.IO;
using CommandLine;
using System.Collections.Generic;
using Serilog;

namespace fs_sync
{
    class Program
    {

        [Verb("new", HelpText = "Create a new sync set")]
        public class NewOptions
        {           
            [Value(0, Required = true, MetaName = "PathA", HelpText = "Directory path for the left side of the compare")]
            public string PathA { get; set; }

            [Value(1, Required = true, MetaName = "PathB", HelpText = "Directory path for the right side of the compare")]
            public string PathB { get; set; }

            [Option(shortName: 'o', longName: "output", HelpText = "File path of the file to save the sync job to")]
            public string SyncJobFile { get; set; }
        }

        [Verb("sync", HelpText = "Run a folder sync")]
        public class SyncOptions {
            [Option(shortName: 'i', longName: "input")]
            public string SyncJobFile { get; set; }
        }

        private static SyncEngine<SyncStatusLine> syncEngine;
        private static FileSyncer fileSyncer;
        private static Logger logger;

        static void Main(string[] args)
        {
            syncEngine = new SyncEngine<SyncStatusLine>((a,b) => FileMatcher(a ,b));
            fileSyncer = new FileSyncer();

            logger = new LoggerConfiguration();

            Parser.Default.ParseArguments<NewOptions,SyncOptions>(args)
                .WithParsed<NewOptions>(o => NewSyncJob(o))
                .WithParsed<SyncOptions>(o => RunSyncJob(o))
                .WithNotParsed(errs => Console.WriteLine(errs));
        }

        static void NewSyncJob(NewOptions options) {
            if (!Directory.Exists(options.PathA)) throw new ArgumentException($"PathA ({options.PathA}) does not exist");
            if (!Directory.Exists(options.PathB)) throw new ArgumentException($"PathB ({options.PathB}) does not exist");

            var syncJob = new SyncJob(options.PathA, options.PathB);
            syncJob.Save(options.SyncJobFile);
        }

        static void RunSyncJob(SyncOptions options) {

            var syncJob = SyncJob.Load(options.SyncJobFile);

            SyncItem<SyncStatusLine> CreateSyncItem(FileInfo fileInfo, string basePath) {
                string key = Path.GetRelativePath(basePath, fileInfo.FullName);
                var syncLine = new SyncStatusLine { 
                    Key = key,
                    LastModified = fileInfo.LastWriteTimeUtc
                };
                return new SyncItem<SyncStatusLine>(fileInfo.FullName, key, syncLine);
            }

            var sourceFiles = new DirectoryParser().Parse(syncJob.PathA)
                                .Select(fi => CreateSyncItem(fi, syncJob.PathA))
                                .ToHashSet();
            var destFiles = new DirectoryParser().Parse(syncJob.PathB)
                                .Select(fi => CreateSyncItem(fi, syncJob.PathB))
                                .ToHashSet();
            var statusLines = syncJob.StatusLines
                                .Select(sl => new SyncItem<SyncStatusLine>("", sl.Key, sl))
                                .ToHashSet();

            var changeset = syncEngine.GetChangeSet(sourceFiles, destFiles, statusLines);            

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

            fileSyncer.Sync(syncJob, changeset);
            syncJob.Save(options.SyncJobFile);
        }

        private static SyncItem<SyncStatusLine> FileMatcher(SyncItem<SyncStatusLine> a, SyncItem<SyncStatusLine> b) {
            Console.WriteLine($"Conflict Resolution: A = {a.Item.LastModified.ToString()}, B = {b.Item.LastModified.ToString()}");
            return a.Item.LastModified > b.Item.LastModified ? a : a.Item.LastModified < b.Item.LastModified ? b : null;
        }
    }
}
