using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DirectorySync {
    public class SyncJob
    {
        public SyncJob(string pathA, string pathB, string logPath, bool debug)
            : this(pathA, pathB, new List<FileStatusLine>(), logPath, debug)
        { }

        [JsonConstructor]
        public SyncJob(string pathA, string pathB, IList<FileStatusLine> statusLines, string logPath, bool debug)
        {
            this.Debug = debug;
            this.LogPath = logPath;
            this.PathA = pathA ?? throw new ArgumentNullException(nameof(pathA));
            this.PathB = pathB ?? throw new ArgumentNullException(nameof(pathB));
            this.StatusLines = statusLines ?? new List<FileStatusLine>();
        }

        public string PathA { get; }
        public string PathB { get; }
        public IList<FileStatusLine> StatusLines { get; }
        public string LogPath { get; }
        public bool Debug { get; }

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