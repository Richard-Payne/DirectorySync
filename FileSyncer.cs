using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DirectorySync {
    public class FileSyncer
    {
        private readonly ILogger logger;

        public FileSyncer(ILogger logger)
        {
            this.logger = logger;
        }

        public void Sync(SyncJob job, IList<SyncOperation<SyncStatusLine>> ops)
        {
            foreach (var op in ops)
            {
                string fileA = Path.Join(job.PathA, op.Item.Key);
                string fileB = Path.Join(job.PathB, op.Item.Key);
                if (op.CopyToA)
                {
                    logger.LogInformation($"Copy B -> A: {op.Item.Key}");       
                    Directory.CreateDirectory(Path.GetDirectoryName(fileA));
                    File.Copy(fileB, fileA, true);                    
                }
                if (op.CopyToB)
                {
                    logger.LogInformation($"Copy A -> B: {op.Item.Key}");
                    Directory.CreateDirectory(Path.GetDirectoryName(fileB));
                    File.Copy(fileA, fileB, true);
                }
                if (op.DeleteFromA)
                {
                    logger.LogInformation($"Delete From A: {op.Item.Key}");
                    File.Delete(fileA);
                }
                if (op.DeleteFromB)
                {
                    logger.LogInformation($"Delete From B: {op.Item.Key}");
                    File.Delete(fileB);
                }
                if (op.AddToStatus)
                {
                    var statusLine = job.StatusLines.Where(sl => sl.Key == op.Item.Key).SingleOrDefault();
                    if (statusLine != null) throw new DuplicateNameException($"The key already exists in the sync job status. {op.Item.Key}");
                    job.StatusLines.Add(new SyncStatusLine { Key = op.Item.Key, LastModified = op.Item.Item.LastModified });
                }
                if (op.DeleteFromStatus)
                {
                    var statusLine = job.StatusLines.Where(sl => sl.Key == op.Item.Key).Single();
                    job.StatusLines.Remove(statusLine);
                }
                if (op.UpdateStatus)
                {
                    var statusLine = job.StatusLines.Where(sl => sl.Key == op.Item.Key).Single(); ;
                    statusLine.LastModified = op.Item.Item.LastModified;
                }
            }
        }
    }
}