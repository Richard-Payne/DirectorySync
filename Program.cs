using System;
using System.IO;
using CommandLine;

namespace fs_sync
{
    class Program
    {
        public class Options
        {
            [Value(0)]
            public string SourcePath { get; set; }

            [Value(1)]
            public string DestinationPath { get; set; }
        }

        private static Options options;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {
                options = o;
            });

            var sourceFiles = new DirectoryParser().Parse(options.SourcePath);
            var destFiles = new DirectoryParser().Parse(options.DestinationPath);

            
        }
    }
}
