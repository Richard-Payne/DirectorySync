using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class SyncJob {
    public SyncJob(string pathA, string pathB) 
        : this(pathA, pathB, new List<SyncStatusLine>())
    { }

    [JsonConstructor]
    public SyncJob(string pathA, string pathB, IList<SyncStatusLine> statusLines) {
        this.PathA = pathA ?? throw new ArgumentNullException(nameof(pathA));
        this.PathB = pathB ?? throw new ArgumentNullException(nameof(pathB));
        this.StatusLines = statusLines ?? new List<SyncStatusLine>();
    }

    public string PathA { get; }
    public string PathB { get; }
    public IList<SyncStatusLine> StatusLines { get; } 

    public void Save(string outputFile) {
        var json = JsonConvert.SerializeObject(this);
        File.WriteAllText(outputFile, json);
    }

    public static SyncJob Load(string inputFile) {
        var json = File.ReadAllText(inputFile);
        return JsonConvert.DeserializeObject<SyncJob>(json);
    }
}

public class SyncStatusLine {
    public string Key { get; set; }
    public DateTime LastModified {  get; set; }
}