using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;

namespace DirectorySync {
    public class FileSyncer
    {
        private readonly ILogger logger;

        public FileSyncer(ILogger logger)
        {
            this.logger = logger;
        }

        public void Sync(SyncJob job, IList<SyncOperation<FileStatusLine>> ops)
        {
            foreach (var op in ops)
            {
                if (op?.Item == null) {
                    logger.LogInformation("Nothing to do");
                    continue;
                }

                string fileA = Path.Join(job.PathA, op.Item.Key);
                string fileB = Path.Join(job.PathB, op.Item.Key);
                if (op.CopyToA)
                {
                    logger.LogInformation($"Copy B -> A: {op.Item.Key}");       
                    Directory.CreateDirectory(Path.GetDirectoryName(fileA));
                    Copy(fileB, fileA);                    
                }
                if (op.CopyToB)
                {
                    logger.LogInformation($"Copy A -> B: {op.Item.Key}");
                    Directory.CreateDirectory(Path.GetDirectoryName(fileB));
                    Copy(fileA, fileB);
                }
                if (op.DeleteFromA)
                {
                    logger.LogInformation($"Delete From A: {op.Item.Key}");
                    Delete(fileA);
                }
                if (op.DeleteFromB)
                {
                    logger.LogInformation($"Delete From B: {op.Item.Key}");
                    Delete(fileB);
                }
                if (op.AddToStatus)
                {
                    var statusLine = job.StatusLines.Where(sl => sl.Key == op.Item.Key).SingleOrDefault();
                    if (statusLine != null) throw new DuplicateNameException($"The key already exists in the sync job status. {op.Item.Key}");
                    job.StatusLines.Add(new FileStatusLine { Key = op.Item.Key, LastModified = op.Item.Item.LastModified });
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

        private void Copy(string source, string dest) {
            try { 
                File.Copy(source, dest, true);
            } catch(Exception ex) {
                logger.LogError(ex, $"Error copying {source} to {dest}");
            }
        }

        private void Delete(string file) {
            try { 
                File.Delete(file);
            } catch(Exception ex) {
                logger.LogError(ex, $"Error deleting {file}");
            }
        }
    }
}