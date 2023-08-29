using System;
using System.Security;

namespace Barometer.Services {
    internal class LogWriter {

        string ParentDirectory;
        string LogFolderName;
        string FullPath;
        StreamWriter streamWriter;
        public LogWriter(string LogFolderName, string ParentDirectory) { 
            this.ParentDirectory = ParentDirectory;
            this.LogFolderName = LogFolderName;
            this.FullPath = Path.Combine(ParentDirectory, LogFolderName);
        }

        public void WriteToFile(string fileName, string value) {
            if (!Directory.Exists(FullPath)) {
                Directory.CreateDirectory(FullPath);
            }

            StreamWriter stream = File.AppendText(Path.Combine(FullPath, fileName));
            stream.WriteLine(value);
            stream.Close();
        }

        public void SetParentDirectory(string ParentDirectory) {
            this.ParentDirectory=ParentDirectory;
        }

        public void SetLogFolderName(string LogFolderName) {
            this.LogFolderName=LogFolderName;
        }

        public void SetFullPath(string FullPath) {
            this.FullPath=FullPath;
        }

    }
}
