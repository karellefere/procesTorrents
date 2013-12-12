using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace processTorrents
{
    class LogFile
    {
        private StreamWriter _logFile;

        public LogFile()
        {
            _logFile = new StreamWriter("processTorrents.log", true);
        }

        public void Write(string message)
        {
            _logFile.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " : " + message);
        }

        public void Close()
        {
            _logFile.Close();
        }
    }  
}
