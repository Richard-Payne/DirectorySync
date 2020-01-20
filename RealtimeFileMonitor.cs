using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DirectorySync
{
    public class RealtimeFileMonitor
    {
        private readonly FileSyncer syncer;
        private readonly object syncOpLock = new object();

        public RealtimeFileMonitor(FileSyncer syncer)
        {
            this.syncer = syncer;
        }

        public void Monitor(SyncJob syncJob) {
            var fswA = CreateFsWatcher(syncJob, syncJob.PathA);
            var fswB = CreateFsWatcher(syncJob, syncJob.PathB);
        }

        FileSystemWatcher CreateFsWatcher(SyncJob syncJob, string path) {
            var fsw = new FileSystemWatcher(path);
            fsw.IncludeSubdirectories = true;
            fsw.EnableRaisingEvents = true;
            fsw.Changed += (sender, e) => FswUpdate(syncJob, e);
            fsw.Created += (sender, e) => FswUpdate(syncJob, e);
            fsw.Deleted += (sender, e) => FswUpdate(syncJob, e);
            fsw.Renamed += (sender, e) => FswRename(syncJob, e);
            return fsw;
        }

        void FswUpdate(SyncJob syncJob, FileSystemEventArgs e) {
            lock(syncOpLock) {
                var ops = new List<SyncOperation<SyncStatusLine>>();

                bool isA = e.FullPath.StartsWith(syncJob.PathA);
                string key = Path.GetRelativePath(isA ? syncJob.PathA : syncJob.PathB, e.FullPath);
                var itemStatus = syncJob.StatusLines.Where(sl => sl.Key == key).SingleOrDefault();

                var op = new SyncOperation<SyncStatusLine>(new SyncItem<SyncStatusLine>(e.FullPath, key, itemStatus));
                switch (e.ChangeType) {
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Changed:
                        if (isA)
                            op.CopyToB = true;
                        else
                            op.CopyToA = true;
                        op.AddToStatus = itemStatus == null;
                        op.UpdateStatus = itemStatus != null;                        
                        break;

                    case WatcherChangeTypes.Deleted:
                        if (isA)
                            op.DeleteFromB = true;
                        else 
                            op.DeleteFromA = true;
                        op.DeleteFromStatus = true;
                        break;
                }
                ops.Add(op);

                syncer.Sync(syncJob, ops);
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
                syncJob.StatusLines.Add(new SyncStatusLine { Key = key, LastModified = new FileInfo(e.FullPath).LastWriteTimeUtc });                
            }
        }
    }
}