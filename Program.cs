using System.Diagnostics;
using System.Linq;
using System;
using System.IO;
using CommandLine;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Logging;

namespace DirectorySync
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<NewOptions,SyncOptions>(args)
                .WithParsed<NewOptions>(o => NewSyncJob(o))
                .WithParsed<SyncOptions>(o => RunSyncJob(o))
                .WithNotParsed(errs => Console.WriteLine(errs));
        }

        static void NewSyncJob(NewOptions options) {
            if (!Directory.Exists(options.PathA)) throw new ArgumentException($"PathA ({options.PathA}) does not exist");
            if (!Directory.Exists(options.PathB)) throw new ArgumentException($"PathB ({options.PathB}) does not exist");

            SyncJob syncJob, oldSyncJob;
            if (File.Exists(options.SyncJobFile)) {
                oldSyncJob = SyncJob.Load(options.SyncJobFile);                
                syncJob = new SyncJob(options.PathA, options.PathB, oldSyncJob.StatusLines, options.LogDirectory, options.Debug);
            } else {
                syncJob = new SyncJob(options.PathA, options.PathB, options.LogDirectory, options.Debug);
            }
            
            syncJob.Save(options.SyncJobFile);
        }

        static void RunSyncJob(SyncOptions options) {

            var syncJob = SyncJob.Load(options.SyncJobFile);
            
            var logger = new LoggerFactory()
                .AddSerilog(new LoggerConfiguration()
                    .MinimumLevel.Is(syncJob.Debug ? LogEventLevel.Debug : LogEventLevel.Information)                
                    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: "{Message:lj}{NewLine}")
                    .WriteTo.File(syncJob.LogPath, rollingInterval: Serilog.RollingInterval.Day)
                    .CreateLogger())
                .CreateLogger("sync_run");

            var syncEngine = new SyncEngine<SyncStatusLine>(logger, (a,b) => FileMatcher(a ,b));
            var fileSyncer = new FileSyncer(logger);                
            
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
            
            PrintOperations();
            if (!GetUserConfirmation())
                return;

            fileSyncer.Sync(syncJob, changeset);
            syncJob.Save(options.SyncJobFile);

            if (options.Realtime) {
                logger.LogInformation("");
                logger.LogInformation("Entering realtime file system monitoring...");
                var fileMonitor = new RealtimeFileMonitor(fileSyncer);
                fileMonitor.Monitor(syncJob);

                while(true) {
                    System.Threading.Thread.Sleep(1);
                }
            }

            SyncItem<SyncStatusLine> CreateSyncItem(FileInfo fileInfo, string basePath) {
                string key = Path.GetRelativePath(basePath, fileInfo.FullName);
                var syncLine = new SyncStatusLine { 
                    Key = key,
                    LastModified = fileInfo.LastWriteTimeUtc
                };
                return new SyncItem<SyncStatusLine>(fileInfo.FullName, key, syncLine);
            }            

            SyncItem<SyncStatusLine> FileMatcher(SyncItem<SyncStatusLine> a, SyncItem<SyncStatusLine> b) {
                logger.LogDebug($"Conflict Resolution: A = {a.Item.LastModified.ToString()}, B = {b.Item.LastModified.ToString()}");
                return a.Item.LastModified > b.Item.LastModified ? a : a.Item.LastModified < b.Item.LastModified ? b : null;
            }

            void PrintOperations() {
                Log("changeset size = " + changeset.Count());
                if (changeset.Count() > 0) {
                    Log("Operations to perform:");
                    int maxKeyLen = changeset.Select(s => s.Item.Key.Length).Max() + 2;
                    foreach (var op in changeset) {
                        int padding = maxKeyLen - op.Item.Key.Length;
                        string opLine = $"{new string(' ', 4)}{op.Item.Key}{new string(' ', padding)}";
                        if (op.CopyToA) opLine += $"copyToA{new string(' ', 5)}";
                        if (op.CopyToB) opLine += $"copyToB{new string(' ', 5)}";
                        if (op.DeleteFromA) opLine += $"deleteFromA{new string(' ', 2)}";
                        if (op.DeleteFromB) opLine += $"deleteFromB{new string(' ', 2)}";
                        if (!(op.CopyToA || op.CopyToB || op.DeleteFromA || op.DeleteFromB)) opLine += $"{new string(' ', 12)}";
                        if (op.AddToStatus) opLine += $"addToStatus{new string(' ', 7)}";
                        if (op.UpdateStatus) opLine += $"updateStatus{new string(' ', 6)}";
                        if (op.DeleteFromStatus) opLine += $"deleteFromStatus{new string(' ', 2)}";
                        if (!(op.AddToStatus || op.UpdateStatus || op.DeleteFromStatus || op.DeleteFromB)) opLine += $"{new string(' ', 18)}";
                        opLine += op.Reason;
                        Log(opLine);
                    }
                } else {
                    Log("\tDirectories are in-sync");
                }

                void Log(string message) {
                    if (options.Force)
                        logger.LogDebug(message);
                    else
                        logger.LogInformation(message);
                }
            }

            bool GetUserConfirmation() {
                if (!options.Force) {
                    logger.LogInformation("");
                    logger.LogInformation("Confirm? (yes/NO): ");
                    string confirm = Console.ReadLine();
                    if (confirm.ToLower() != "yes") {
                        logger.LogDebug("User cancelled sync");
                        return false;
                    }
                    logger.LogDebug("Sync confirmed");
                } else {
                    logger.LogDebug("Force option set. Confirmation skipped");
                }
                return true;
            }
        }        
    }
}
