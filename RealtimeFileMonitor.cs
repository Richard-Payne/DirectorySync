using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DirectorySync
{
    public class RealtimeFileMonitor
    {
        private readonly ILogger logger;
        private readonly SyncEngine<FileStatusLine> syncEngine;
        private readonly FileSyncer syncer;
        private readonly object syncOpLock = new object();

        public RealtimeFileMonitor(ILogger logger, SyncEngine<FileStatusLine> syncEngine, FileSyncer syncer)
        {
            this.logger = logger;
            this.syncEngine = syncEngine;
            this.syncer = syncer;
        }

        public void Monitor(SyncJob syncJob, string syncJobFile) {
            var fswA = CreateFsWatcher(syncJob, syncJobFile, syncJob.PathA);
            var fswB = CreateFsWatcher(syncJob, syncJobFile, syncJob.PathB);
        }

        FileSystemWatcher CreateFsWatcher(SyncJob syncJob, string syncJobFile, string path) {
            var fsw = new FileSystemWatcher(path);
            fsw.IncludeSubdirectories = true;
            fsw.EnableRaisingEvents = true;
            fsw.Changed += (sender, e) => FswUpdate(syncJob, syncJobFile, e);
            fsw.Created += (sender, e) => FswUpdate(syncJob, syncJobFile, e);
            fsw.Deleted += (sender, e) => FswUpdate(syncJob, syncJobFile, e);
            fsw.Renamed += (sender, e) => FswRename(syncJob, e);
            fsw.Error += (sender, e) => logger.LogError(e.GetException(), "Error receiving file system events");
            return fsw;
        }

        void FswUpdate(SyncJob syncJob, string syncJobFile, FileSystemEventArgs e) {
            logger.LogInformation($"FileSystem Event: {e.ChangeType.ToString()} {e.FullPath}");
            lock(syncOpLock) {
                bool isA = e.FullPath.StartsWith(syncJob.PathA);

                string key = Path.GetRelativePath(isA ? syncJob.PathA : syncJob.PathB, e.FullPath);
                string counterPath = isA ? syncJob.PathB : syncJob.PathA;
                string counterKey = Path.GetRelativePath(counterPath, e.FullPath);

                SyncItem<FileStatusLine> itemEvent = null;
                if (e.ChangeType != WatcherChangeTypes.Deleted) {
                    itemEvent = new SyncItem<FileStatusLine>(e.FullPath, key, new FileStatusLine {  Key = key, LastModified = (new FileInfo(e.FullPath)).LastWriteTimeUtc });
                }

                SyncItem<FileStatusLine> itemCounter = null;
                logger.LogDebug($"counterPath = {counterPath}");
                if (File.Exists(Path.Join(counterPath, key))) {
                    logger.LogDebug($"counterPath exists");
                    itemCounter = new SyncItem<FileStatusLine>(e.FullPath, key, new FileStatusLine {  Key = key, LastModified = (new FileInfo(e.FullPath)).LastWriteTimeUtc });
                }

                SyncItem<FileStatusLine> itemStatus =  null;
                var statusLine = syncJob.StatusLines.Where(sl => sl.Key == key).SingleOrDefault();
                if (statusLine != null) {
                    itemStatus = new SyncItem<FileStatusLine>("", key, statusLine);
                }

                SyncItem<FileStatusLine> itemA, itemB;
                if (isA) {
                    itemA = itemEvent;
                    itemB = itemCounter;
                } else {
                    itemA = itemCounter;
                    itemB = itemEvent;
                }

                var op = syncEngine.GetOpForKey(itemA, itemB, itemStatus);
                var ops = new [] { op };

                if (op != null && (op.GetFileOp() != "" || op.GetStatusOp() != "")) {
                    logger.LogInformation("Operation to perform:");
                    logger.LogInformation(op.ToString());
                } else {
                    logger.LogDebug("No changes to make");
                }

                try {
                    syncer.Sync(syncJob, ops);
                    syncJob.Save(syncJobFile);
                } catch(Exception ex) {
                    logger.LogError(ex, "Error performing sync");
                }
            }
        }

        void FswRename(SyncJob syncJob, RenamedEventArgs e) {
            lock(syncOpLock) {
                bool isA = e.FullPath.StartsWith(syncJob.PathA);

                string oldKey = Path.GetRelativePath(isA ? syncJob.PathA : syncJob.PathB, e.OldFullPath);
                string key = Path.GetRelativePath(isA ? syncJob.PathA : syncJob.PathB, e.FullPath);

                var oldItemStatus = syncJob.StatusLines.Where(sl => sl.Key == oldKey).SingleOrDefault();
                var itemStatus = syncJob.StatusLines.Where(sl => sl.Key == key).SingleOrDefault();

                if (isA)                     
                    File.Move(Path.Join(syncJob.PathB, oldKey), Path.Join(syncJob.PathB, key));
                else
                    File.Move(Path.Join(syncJob.PathA, oldKey), Path.Join(syncJob.PathA, key));

                syncJob.StatusLines.Remove(oldItemStatus);
                syncJob.StatusLines.Add(new FileStatusLine { Key = key, LastModified = new FileInfo(e.FullPath).LastWriteTimeUtc });                
            }
        }
    }
}