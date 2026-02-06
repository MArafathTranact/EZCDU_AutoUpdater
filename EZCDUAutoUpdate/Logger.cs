using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZCDUAutoUpdate
{
    public static class Logger
    {
        public static object _locked = new object();
        //private static string folder = ServiceConfiguration.GetFileLocation("Trace");
        //private static int logsize = int.Parse(ServiceConfiguration.GetFileLocation("LogSize"));
        //private static string ExceptionLog = ServiceConfiguration.GetFileLocation("ExceptionLog");
        public static Queue<string> logmessages = new Queue<string>();
        //private static bool IsloggingStopped = false;
        private static ILogger logger { get; set; }
        static Logger()
        {
            logger = LogManager.GetCurrentClassLogger();

            //Task.Factory.StartNew(() =>
            //{
            //    LogEnqueuedMessage();
            //});
        }

        private static void LogEnqueuedMessage()
        {

            //while (true)
            //{
            //    try
            //    {
            //        if (logmessages.Count > 0)
            //            LogMessages(logmessages.Dequeue());
            //        else
            //        {
            //            Thread.Sleep(1000);
            //        }


            //    }
            //    catch (Exception ex)
            //    {
            //        IsloggingStopped = true;
            //        LogMessages($"Dequeue Error : {ex.Message}");
            //    }

            //}

        }

        private static void LogInfo(string information)
        {
            try
            {
                logger.Info(information);
            }
            catch (Exception)
            {
            }
        }

        private static void LogError(string message, Exception exception)
        {
            try
            {
                logger.Error(exception, message);
            }
            catch (Exception)
            {
            }
        }

        private static void LogWarning(string message)
        {
            try
            {
                logger.Warn(message);
            }
            catch (Exception)
            {
            }
        }

        public static void LogWithNoLock(string message)
        {
            LogInfo(message);
        }

        public static void LogExceptionWithNoLock(string message, Exception exception)
        {
            LogError(message, exception);
        }

        public static void LogWarningWithNoLock(string message)
        {
            LogWarning(message);
        }



        public static void LogMessages(string message)
        {
            //try
            //{
            //    if (!string.IsNullOrEmpty(message))
            //    {
            //        CreateLogFile(folder);

            //        FileInfo fi = new FileInfo(folder);
            //        var size = fi.Length >> 20;
            //        var fileMode = size >= logsize ? FileMode.Truncate : FileMode.Append;

            //        using (var fs = new FileStream(folder, fileMode, FileAccess.Write, FileShare.Write))
            //        using (var sw = new StreamWriter(fs))
            //        {
            //            sw.WriteLine(message);
            //        }
            //    }

            //}
            //catch (Exception ex)
            //{
            //    LogExceptionMessages($"Message : {message} ; Exception : {ex.Message}");
            //}

        }

        public static void LogExceptionMessages(string message)
        {
            //try
            //{
            //    CreateLogFile(ExceptionLog);

            //    FileInfo fi = new FileInfo(ExceptionLog);
            //    var size = fi.Length >> 20;
            //    var fileMode = size >= logsize ? FileMode.Truncate : FileMode.Append;

            //    using (var fs = new FileStream(ExceptionLog, fileMode, FileAccess.Write, FileShare.Write))
            //    using (var sw = new StreamWriter(fs))
            //    {
            //        sw.WriteLine(message);
            //    }

            //}
            //catch (Exception ex)
            //{
            //    //LogWithNoLock($"{message}");
            //}

        }

        private static void CreateLogFile(string folder)
        {
            if (!File.Exists(folder))
            {
                using (File.Create(folder)) { }
            }
        }

    }
}
