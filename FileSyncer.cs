using System.Data;
using System.Security.AccessControl;
using System.Net;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class FileSyncer {
    public void Sync(SyncJob job, IList<SyncOperation<SyncStatusLine>> ops) {
        foreach(var op in ops) {
            if (op.CopyToA) {
                File.Copy(op.Item.Id, Path.Join(job.PathA, op.Item.Key), true);
            }
            if (op.CopyToB) {
                File.Copy(op.Item.Id, Path.Join(job.PathB, op.Item.Key), true);
            }
            if (op.DeleteFromA) {
                File.Delete(Path.Join(job.PathA, op.Item.Key));
            }
            if (op.DeleteFromB) {
                File.Delete(Path.Join(job.PathB, op.Item.Key));
            }
            if (op.AddToStatus) {
                var statusLine = job.StatusLines.Where(sl => sl.Key == op.Item.Key).SingleOrDefault();
                if (statusLine != null) throw new DuplicateNameException($"The key already exists in the sync job status. {op.Item.Key}");
                job.StatusLines.Add(new SyncStatusLine { Key = op.Item.Key, LastModified = op.Item.Item.LastModified });
            }
            if (op.DeleteFromStatus) {
                var statusLine = job.StatusLines.Where(sl => sl.Key == op.Item.Key).Single();
                job.StatusLines.Remove(statusLine);
            }
            if (op.UpdateStatus) {
                var statusLine = job.StatusLines.Where(sl => sl.Key == op.Item.Key).Single();;
                statusLine.LastModified = op.Item.Item.LastModified;
            }
        }
    }
}