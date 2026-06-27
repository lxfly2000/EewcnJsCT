using System;
using System.IO;
using System.Text;

namespace EewcnJsCT
{
    public class Logger
    {
        private const string logFileName = "eewcnjs.log";
        private string logPath = "./"+logFileName;
        private StreamWriter fileStream;
        private Logger()
        {
            try
            {
                fileStream = new StreamWriter(logPath, false, Encoding.UTF8);
                fileStream.Close();
                fileStream.Dispose();
            }
            catch (Exception e)
            {
                //不能使用程序的当前目录
                logPath=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"/EewcnJsCT";
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);
                logPath+="/"+logFileName;
                fileStream=new StreamWriter(logPath, false, Encoding.UTF8);
                fileStream.Close();
                fileStream.Dispose();
            }
        }
        private static Logger singleton = null;

        public static Logger GetInstance()
        {
            if (singleton == null)
                singleton = new Logger();
            return singleton;
        }
        public void Log(string tag,string msg)
        {
            fileStream=new StreamWriter(logPath,true,Encoding.UTF8);
            string s=Value.timestampMSToChineseDateTime(DateTimeOffset.Now.ToUnixTimeMilliseconds())+"["+tag+"]"+msg+"\n";
            fileStream.Write(s);
            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();
        }
        
        public void info(string msg)
        {
            Log("INFO",msg);
        }
            
        public void warn(string msg)
        {
            Log("WARN",msg);
        }
            
        public void error(string msg)
        {
            Log("ERROR",msg);
        }
    }
}