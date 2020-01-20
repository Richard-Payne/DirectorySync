using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DirectorySync {
    public class DirectoryParser {
        private readonly ILogger logger;

        public DirectoryParser(ILogger logger) {
            this.logger = logger;
        }

        public List<FileInfo> Parse(string directory) {
            
            Console.Write($"Parsing {directory}");
            Console.SetCursorPosition(0, Console.CursorTop);

            IEnumerable<FileInfo> files = Directory.GetFiles(directory).Select(f => new FileInfo(f));

            foreach(var subDirectory in Directory.GetDirectories(directory)) {
                files = files.Union(Parse(subDirectory));
            }

            return files.ToList();
        }
    }
}