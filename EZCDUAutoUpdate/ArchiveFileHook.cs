using Serilog.Sinks.File;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZCDUAutoUpdate
{
    public class ArchiveFileHook : FileLifecycleHooks
    {
        private readonly string _archiveDirectory;
        private readonly int _rollOutDays;

        public ArchiveFileHook(string archiveDirectory, int rollOutDays)
        {
            _archiveDirectory = archiveDirectory;
            if (!Directory.Exists(_archiveDirectory)) Directory.CreateDirectory(_archiveDirectory);
            this._rollOutDays = rollOutDays;
        }

        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
            try
            {
                string fileName = Path.GetFileName(path);

                if (fileName.Contains("_00") || fileName.Contains("_0"))
                {
                    string folder = Path.GetDirectoryName(path);
                    string staticPath = Path.Combine(folder, "AutoUpdateLog.txt");

                    underlyingStream.Dispose();

                    if (File.Exists(staticPath))
                    {
                        LogEvents($" Archieving the AutoUpdateLog file.");
                        // 1. Create timestamped name for the ZIP
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_hhmm_tt");
                        string zipPath = Path.Combine(_archiveDirectory, $"AutoUpdateLog-{timestamp}.zip");

                        // 2. Create the ZIP and add the log file to it
                        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                        {
                            // This puts the 100MB file inside the ZIP
                            archive.CreateEntryFromFile(staticPath, "AutoUpdateLog.txt");
                        }

                        LogEvents($" AutoUpdateLog trace file moved to archieve folder.");
                        // 3. Delete the original 100MB file now that it's zipped
                        File.Delete(staticPath);
                    }

                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    CleanupOldArchives(_rollOutDays);

                    return new FileStream(staticPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Zipping Error: " + ex.Message);
            }

            return base.OnFileOpened(path, underlyingStream, encoding);
        }

        private void CleanupOldArchives(int daysToKeep)
        {
            try
            {
                // 1. Calculate the cutoff date (30 days ago)
                DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                // 2. Get the directory info for your archive folder
                DirectoryInfo directory = new DirectoryInfo(_archiveDirectory);

                // 3. Find all zip files
                FileInfo[] files = directory.GetFiles("*.zip");


                foreach (FileInfo file in files)
                {
                    // 4. If the file was last modified before the cutoff, delete it
                    if (file.LastWriteTime < cutoffDate)
                    {

                        file.Delete();
                        // Optional: Log that a file was purged for maintenance

                        LogEvents($" Deleting {file.Name} ,{_rollOutDays} days older file from archieved folder.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail silently or log to Event Viewer to avoid crashing the service
                System.Diagnostics.Trace.WriteLine("Cleanup Error: " + ex.Message);
            }
        }

        private static void LogEvents(string input)
        {
            Logger.LogWithNoLock($"{input}");
        }
    }
}
