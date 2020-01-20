using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DirectorySync {
    public class SyncJob
    {
        public SyncJob(string pathA, string pathB, string logPath, int logFileLimit)
            : this(pathA, pathB, new List<FileStatusLine>(), logPath, -1, logFileLimit)
        { }

        [JsonConstructor]
        public SyncJob(string pathA, string pathB, IList<FileStatusLine> statusLines, string logPath, int currentPid, int logFileLimit)
        {
            this.LogPath = logPath;
            this.PathA = pathA ?? throw new ArgumentNullException(nameof(pathA));
            this.PathB = pathB ?? throw new ArgumentNullException(nameof(pathB));
            this.StatusLines = statusLines ?? new List<FileStatusLine>();
            this.CurrentPid = currentPid;
            this.LogFileLimit = logFileLimit;
        }

        public string PathA { get; }
        public string PathB { get; }
        public IList<FileStatusLine> StatusLines { get; }
        public string LogPath { get; }
        public int CurrentPid { get; set; }
        public int LogFileLimit { get; }

        public void Save(string outputFile)
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(outputFile, json);
        }

        public static SyncJob Load(string inputFile)
        {
            var json = File.ReadAllText(inputFile);
            return JsonConvert.DeserializeObject<SyncJob>(json);
        }
    }

    public class FileStatusLine
    {
        public string Key { get; set; }
        public DateTime LastModified { get; set; }

        public override string ToString()
            => $"[ Key = { Key }, LastModified = { LastModified } ]";
    }
}