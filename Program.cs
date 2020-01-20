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
                syncJob = new SyncJob(options.PathA, options.PathB, oldSyncJob.StatusLines, options.LogDirectory, oldSyncJob.CurrentPid, options.LogFileLimit);
            } else {
                syncJob = new SyncJob(options.PathA, options.PathB, options.LogDirectory, options.LogFileLimit);
            }
            
            syncJob.Save(options.SyncJobFile);
        }

        static void RunSyncJob(SyncOptions options) {

            var syncJob = SyncJob.Load(options.SyncJobFile);

            // only allow one instance running against any sync file
            if (syncJob.CurrentPid > 0 && Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Select(p => p.Id).Contains(syncJob.CurrentPid)) {
                Console.WriteLine($"Unable to run sync on {options.SyncJobFile}. Sync already running in process {syncJob.CurrentPid}");
                return;
            }

            syncJob.CurrentPid = Process.GetCurrentProcess().Id;
            syncJob.Save(options.SyncJobFile);
            
            try {
                var logger = new LoggerFactory()
                    .AddSerilog(new LoggerConfiguration()
                        .MinimumLevel.Is(options.Debug ? LogEventLevel.Debug : LogEventLevel.Information)                
                        .WriteTo.Console(restrictedToMinimumLevel: options.Realtime ? LogEventLevel.Warning : LogEventLevel.Information, outputTemplate: "{Message:lj}{NewLine}")
                        .WriteTo.File(syncJob.LogPath, rollingInterval: Serilog.RollingInterval.Day, retainedFileCountLimit: syncJob.LogFileLimit)
                        .CreateLogger())
                    .CreateLogger("sync_run");

                var syncEngine = new SyncEngine<FileStatusLine>(logger, FileMatcher);
                var fileSyncer = new FileSyncer(logger);                
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var sourceFiles = new DirectoryParser(logger).Parse(syncJob.PathA)
                                    .Select(fi => CreateSyncItem(fi, syncJob.PathA))
                                    .ToHashSet();
                logger.LogInformation($"Parsing PathA ({syncJob.PathA}) took {stopwatch.Elapsed.TotalSeconds} seconds");
                stopwatch.Restart();
                var destFiles = new DirectoryParser(logger).Parse(syncJob.PathB)
                                    .Select(fi => CreateSyncItem(fi, syncJob.PathB))
                                    .ToHashSet();
                logger.LogInformation($"Parsing PathB ({syncJob.PathB}) took {stopwatch.Elapsed.TotalSeconds} seconds");
                stopwatch.Restart();
                var statusLines = syncJob.StatusLines
                                    .Select(sl => new SyncItem<FileStatusLine>("", sl.Key, sl))
                                    .ToHashSet();                
                logger.LogInformation($"Parsing Job Status took {stopwatch.Elapsed.TotalSeconds} seconds");
                Console.WriteLine(new string(' ', Console.WindowWidth));
                stopwatch.Restart();
                var changeset = syncEngine.GetChangeSet(sourceFiles, destFiles, statusLines);
                logger.LogInformation($"Calculating changeset took {stopwatch.Elapsed.TotalSeconds} seconds");
                
                PrintOperations();
                if (!GetUserConfirmation())
                    return;

                stopwatch.Restart();
                fileSyncer.Sync(syncJob, changeset);
                logger.LogInformation($"File sync took {stopwatch.Elapsed.TotalSeconds} seconds");
                syncJob.Save(options.SyncJobFile);

                if (options.Realtime) {
                    logger.LogInformation("");
                    logger.LogInformation("Entering realtime file system monitoring...");
                    var fileMonitor = new RealtimeFileMonitor(logger, syncEngine, fileSyncer);
                    fileMonitor.Monitor(syncJob, options.SyncJobFile);

                    while(true) {
                        System.Threading.Thread.Sleep(1);
                    }
                }

                SyncItem<FileStatusLine> CreateSyncItem(FileInfo fileInfo, string basePath) {
                    string key = Path.GetRelativePath(basePath, fileInfo.FullName);
                    var syncLine = new FileStatusLine { 
                        Key = key,
                        LastModified = fileInfo.LastWriteTimeUtc
                    };
                    return new SyncItem<FileStatusLine>(fileInfo.FullName, key, syncLine);
                }

                SyncItem<FileStatusLine> FileMatcher(SyncItem<FileStatusLine> a, SyncItem<FileStatusLine> b) {
                    logger.LogDebug($"Conflict Resolution: A = {a.Item.LastModified.ToString()}, B = {b.Item.LastModified.ToString()}");
                    return a.Item.LastModified > b.Item.LastModified ? a : a.Item.LastModified < b.Item.LastModified ? b : null;
                }

                void PrintOperations() {
                    if (changeset.Count() > 0) {
                        Log("Operations to perform:");
                        int maxKeyLen = changeset.Select(s => s.Item.Key.Length).Max() + 2;
                        foreach (var op in changeset) {
                            if (op.GetFileOp() == "" && op.GetStatusOp() == "")
                                continue;
                            Log(op.ToString(maxKeyLen));
                        }
                    } else {
                        Log("\tDirectories are in-sync");
                    }

                    void Log(string message) {
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
            } finally {
                syncJob.CurrentPid = 0;
                syncJob.Save(options.SyncJobFile);
            }                
        }        
    }
}
