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

                switch (op.ItemOperation) {
                    case ItemOperation.CopyToA:
                        logger.LogInformation($"Copy B -> A: {op.Item.Key}");       
                        Directory.CreateDirectory(Path.GetDirectoryName(fileA));
                        Copy(fileB, fileA);
                        break;
                    case ItemOperation.CopyToB:
                        logger.LogInformation($"Copy A -> B: {op.Item.Key}");
                        Directory.CreateDirectory(Path.GetDirectoryName(fileB));
                        Copy(fileA, fileB);
                        break;
                    case ItemOperation.DeleteFromA:
                        logger.LogInformation($"Delete From A: {op.Item.Key}");
                        Delete(fileA);
                        break;
                    case ItemOperation.DeleteFromB:
                        logger.LogInformation($"Delete From B: {op.Item.Key}");
                        Delete(fileB);
                        break;
                }

                var matchedLines = job.StatusLines.Where(sl => sl.Key == op.Item.Key);
                FileStatusLine statusLine;
                switch (op.StatusOperation) {
                    case StatusOperation.AddToStatus:
                        statusLine = matchedLines.SingleOrDefault();
                        if (statusLine != null) throw new DuplicateNameException($"The key already exists in the sync job status. {op.Item.Key}");
                        job.StatusLines.Add(new FileStatusLine { Key = op.Item.Key, LastModified = op.Item.Item.LastModified });
                        break;
                    case StatusOperation.UpdateStatus:
                        statusLine = matchedLines.Single();
                        statusLine.LastModified = op.Item.Item.LastModified;                    
                        break;
                    case StatusOperation.DeleteFromStatus:
                        statusLine = matchedLines.Single();
                        job.StatusLines.Remove(statusLine);
                        break;
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