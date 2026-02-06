using NLog;
using NLog.Config;

namespace EZCDUAutoUpdate
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            LogManager.Configuration = new XmlLoggingConfiguration("AutoUpdateNLog.config");
            ApplicationConfiguration.Initialize();
            Application.Run(new CDUAutoUpdate());
        }
    }
}