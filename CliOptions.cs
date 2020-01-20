using CommandLine;

namespace DirectorySync
{
    [Verb("new", HelpText = "Create a new sync set")]
    public class NewOptions
    {           
        [Value(0, Required = true, MetaName = "PathA", HelpText = "Directory path for the left side of the compare")]
        public string PathA { get; set; }

        [Value(1, Required = true, MetaName = "PathB", HelpText = "Directory path for the right side of the compare")]
        public string PathB { get; set; }

        [Option(shortName: 'o', longName: "output", HelpText = "File path of the file to save the sync job to")]
        public string SyncJobFile { get; set; }

        [Option(shortName: 'l', longName: "logdir", HelpText = "File to write audit logging to")]
        public string LogDirectory { get; set; }

        [Option(shortName: 'd', longName: "debug", HelpText = "Enable debug level logging")]
        public bool Debug { get; set; } 

        [Option(shortName: 'c', longName: "log-file-count", HelpText = "Limit the number of daily log files to keep")]
        public int LogFileLimit { get; set; }
    }

    [Verb("sync", HelpText = "Run a folder sync")]
    public class SyncOptions {
        [Option(shortName: 'i', longName: "input")]
        public string SyncJobFile { get; set; }

        [Option(shortName: 'q', longName: "quiet")]
        public bool Quiet { get; set; }

        [Option(shortName: 'r', longName: "realtime")]
        public bool Realtime { get; set; }

        [Option(shortName: 'f', longName: "force")]
        public bool Force { get; set; }

        [Option(shortName: 'd', longName: "debug", HelpText = "Enable debug level logging")]
        public bool Debug { get; set; }
    }
}