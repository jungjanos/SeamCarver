using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banjos4Hire
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
        Exception
    }

    public sealed class Logger
    {
        private static volatile Logger instance;
        private static object syncRoot = new Object();


        private StreamWriter sw;

        private Logger()
        {
        }

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new Logger();
                            instance.sw = new StreamWriter(@"c:\TMP\SeamCarver.log");
                            instance.sw.AutoFlush = true;
                        }
                    }
                }
                return instance;
            }

        }

        public void Write(LogType lt, string message)
        {
            switch (lt)
            {
                case LogType.Info:
                    {
                        instance.sw.WriteLine("Info      " + message);
                        instance.sw.Flush();
                        break;
                    }
                case LogType.Warning:
                    {
                        instance.sw.WriteLine("Warning   " + message);
                        instance.sw.Flush();
                        break;
                    }
                case LogType.Error:
                    {
                        instance.sw.WriteLine("Error     " + message);
                        instance.sw.Flush();
                        break;
                    }
                case LogType.Exception:
                    {
                        instance.sw.WriteLine("Exception " + message);
                        instance.sw.Flush();
                        break;
                    }
                default:
                    break;
            }
        }
    }
}