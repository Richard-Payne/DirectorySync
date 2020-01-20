using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DirectorySync {
    public class DirectoryParser {

        public List<FileInfo> Parse(string directory) {
            
            IEnumerable<FileInfo> files = Directory.GetFiles(directory).Select(f => new FileInfo(f));

            foreach(var subDirectory in Directory.GetDirectories(directory)) {
                files = files.Union(Parse(subDirectory));
            }

            return files.ToList();
        }
    }
}