using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary; // Add reference to Windows Script Host Object Model

namespace EZCDUAutoUpdate
{
    public enum InstallationStatus
    {
        Initialized,
        ServiceCheck,
        DownLoading,
        DownLoadCompleted,
        DownLoadFailed,
        CheckDownLoadSizeSH256,
        CheckDownLoadSizeSH256Failed,
        ServiceFilesCopying,
        ServiceFilesCopyingCompleted,
        CDUInstalling,
        CDUUnInstalling,
        CDUUnInstalled,
        CDUInstalled,
        ServiceStarted,
        CDUReInstalling,
        CDUReInstalled,
        CDUUpdateCompleted

    }

    public partial class CDUAutoUpdate : Form
    {
        #region Properties

        private string appName = "FujitsuInstaller";
        private string TempPath = string.Empty;
        private List<AutoUpdate> localAutoUpdateConfig = [];
        private List<AutoUpdate> azureAutoUpdateConfig = [];
        private AutoUpdate azureAutoUpdate = new();
        private AutoUpdate localAutoUpdate = new();
        private UpdateEZCashServiceConfiguration configuration = new();
        private string localServiceVersion = string.Empty;
        private string azureServiceVersion = string.Empty;
        private InstallationStatus status = InstallationStatus.Initialized;

        #endregion Properties

        public CDUAutoUpdate()
        {
            InitializeComponent();
        }

        private void btnSaveConfiguration_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtServiceDownloadURL.Text) || string.IsNullOrEmpty(txtServiceInstallPath.Text))
                {
                    MessageBox.Show("Provide Service download URL and Install path to continue...");
                    return;
                }


                var config = new UpdateEZCashServiceConfiguration()
                {
                    ServiceInstallPath = txtServiceInstallPath.Text,
                    ServiceDownLoadURL = txtServiceDownloadURL.Text

                };

                if (!string.IsNullOrEmpty(txtPassword.Text))
                {
                    var encryptPassword = TokenEncryptDecrypt.Encrypt(txtPassword.Text);
                    config.ServiceDownLoadPassword = encryptPassword;
                    config.ServiceDownLoadUserName = txtUserName.Text;
                }

                var executablePath = Path.GetDirectoryName(Application.ExecutablePath);

                var filePath = Path.Combine(executablePath, "UpdateEZCDUConfig.config");

                XmlSerializer serializer = new XmlSerializer(typeof(UpdateEZCashServiceConfiguration));
                using (TextWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, config);
                }
                LogEvents($"EZCDU auto update configuration saved. Path = '{filePath}'");
                MessageBox.Show("Settings saved. Relaunch it check for service update");
                Application.Exit();
            }
            catch (Exception ex)
            {
                LogExceptions(" btnSaveConfiguration_Click ", ex);
                MessageBox.Show($"Error Occured while saving. {ex.Message}");
            }

        }

        private async Task<bool> CopyEZCDUFiles(string sourcePath, string destinationPath)
        {
            try
            {
                LogEvents($"Moving EZCDU files from {sourcePath} to {destinationPath} in case error occured and for recovery process.");
                if (string.IsNullOrEmpty(sourcePath) && !Directory.Exists(sourcePath))
                    return false;
                DirectoryInfo directoryInfo = new DirectoryInfo(sourcePath);

                Directory.CreateDirectory(destinationPath);

                // Move files only first
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    if (Path.GetExtension(file.FullName).Equals(".msi", StringComparison.OrdinalIgnoreCase) || file.Name.Contains("EZCDUAutoUpdate"))
                        continue; // Skip .msi files

                    var fileName = Path.Combine(destinationPath, file.Name);
                    file.CopyTo(fileName, true);
                    DisplayProgress($"Copying {file.Name}");
                    await Task.Delay(250);
                    LogEvents($"Copying {file.Name} to {destinationPath}");
                }


                ////Move Archievedlogs folder only, exclude everything else.

                //var folderInfo = directoryInfo.GetDirectories();
                //foreach (var folder in folderInfo)
                //{
                //    if (folder.Exists && folder.Name == "Archievedlogs")
                //    {
                //        await CopyEZCDUFiles(folder.FullName, Path.Combine(destinationPath, folder.Name));
                //    }

                //}

                return true;
            }
            catch (Exception ex)
            {
                LogExceptions(" CopyEZCDUFiles ", ex);
                return false;
            }
        }

        private async Task InitiateAutoUpdateProcess()
        {
            status = InstallationStatus.Initialized;
            LogEvents($"State : {status}");
            LogEvents("Initiating Auto Update process.");

            var response = await DownLoadServiceMSI();
            //Observable.FromAsync(() => DownLoadServiceMSI()).Subscribe(async response =>
            //{
            if (response.Status)
            {
                try
                {
                    await Task.Delay(3000);
                    var msiPath = Path.Combine(TempPath, $"FujitsuInstaller_{azureServiceVersion.Replace('.', '_')}.msi"); ;

                    if (!System.IO.File.Exists(msiPath))
                        throw new FileNotFoundException("MSI not found", msiPath);

                    // Extra safety: wait until file is no longer locked

                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            using (var stream = System.IO.File.Open(msiPath, FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                                // If this succeeds, file is ready
                                break; // success

                            }

                        }
                        catch (IOException)
                        {
                            await Task.Delay(1000);
                        }
                    }


                    LogEvents($"State : {status}");
                    var copyResult = await CopyEZCDUFiles(configuration.ServiceInstallPath, TempPath);

                    DisplayProgress("Uninstalling the current CDU...");
                    status = InstallationStatus.CDUUnInstalling;
                    LogEvents($"State : {status}");
                    await Task.Delay(2000);
                    var uninstallResult = await UnistallCDU();

                    await Task.Delay(3000);

                    if (uninstallResult)
                    {
                        status = InstallationStatus.CDUUnInstalled;
                        LogEvents($"State : {status}");
                        DisplayProgress("Successfully Uninstalled the current CDU...");
                        //CenterLabel(lblStatus);
                        await Task.Delay(2000);
                        DisplayProgress("Installing the new CDU");
                        await Task.Delay(2000);
                        status = InstallationStatus.CDUInstalling;
                        LogEvents($"State : {status}");
                        var installResult = await InstallNewEzCDU();
                        await Task.Delay(3000);
                        if (installResult)
                        {
                            status = InstallationStatus.CDUInstalled;
                            LogEvents($"State : {status}");
                            await Task.Delay(2000);
                            var configCopyResult = CopyConfigToServiceFolder();
                            if (configCopyResult)
                            {
                                LogEvents($"State : {status}");
                                LogEvents($"CDU installed succesfully.");
                                DisplayProgress($"CDU installed succesfully.");
                                await Task.Delay(2000);
                                CopyAutoUpdateConfigFile();
                                await CompleteUpdateProcess();
                            }
                            else
                            {
                                DisplayProgress("Error in copying EZCDU config file.\nManual interuption needed.");
                                LogEvents($"\"Error in copying EZCDU config file.Manual interuption needed.");
                                await Task.Delay(1500);
                                DisplayProgress($"Closing the application.");
                                LogEvents($"Closing the application.");
                                await Task.Delay(1500);
                                Application.Exit();
                            }
                        }
                        else
                        {
                            DisplayProgress("Error in installing the new CDU.\nRe-installing the existing CDU");
                            LogEvents($"Error in installing the new CDU.Re-installing the existing CDU");
                            await Task.Delay(3000);
                            status = InstallationStatus.CDUReInstalling;
                            LogEvents($"State : {status}");
                            var reinstall = await ReinstalCDUOnErroredProcess();
                        }


                    }
                    else
                    {
                        DisplayProgress("Error in uninstalling the existing CDU.");
                        await Task.Delay(1500);
                        DisplayProgress("Reinstalling the existing CDU.");
                        LogEvents($"Error in uninstalling the existing CDU.Auto update interupted.Restarting the existing CDU.");
                        await Task.Delay(2000);
                        status = InstallationStatus.CDUReInstalling;
                        LogEvents($"State : {status}");
                        var reinstall = await ReinstalCDUOnErroredProcess();
                    }
                }
                catch (Exception ex)
                {
                    DisplayProgress("Error occured in Installation Process.\nClosing the application");
                    LogEvents("Error occured in Installation Process.Closing the application");
                    LogEvents($"State : {status}");
                    LogExceptions(" InitiateAutoUpdateProcess() ", ex);
                    Application.Exit();
                }

            }
            else
            {
                DisplayProgress($"Closing the application.");
                LogEvents($"Closing the application.");
                await Task.Delay(1500);
                Application.Exit();
            }
            //    },
            //async (error) =>
            //{
            //    DisplayProgress("Error occured in downloading the installer.\nClosing the application");
            //    LogEvents($"Error occured in downloading the installer.Closing the application");
            //    LogEvents($"State : {status}");
            //    LogExceptions(" InitiateAutoUpdateProcess() ", error);
            //    Application.Exit();
            //});

        }

        private async Task CompleteUpdateProcess()
        {
            try
            {
                await Task.Delay(2000);
                DisplayProgress("Cleaning up the resources...");
                LogEvents($"Cleaning up the resources...");
                status = InstallationStatus.CDUUpdateCompleted;
                LogEvents($"State : {status}");
                await Task.Delay(2000);
                await DisposeTempFiles();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<Respone> DownLoadServiceMSI()
        {
            var response = new Respone { Status = true };
            try
            {
                status = InstallationStatus.ServiceCheck;

                LogEvents($"State : {status}");
                var autoUpdateRequired = await CompareAutoUpdateConfiguration();

                if (autoUpdateRequired.Status)
                {
                    status = InstallationStatus.DownLoading;
                    DisplayProgress($"Downloading new CDU version {azureServiceVersion}...");
                    LogEvents($"Downloading new CDU version {azureServiceVersion}...");
                    await Task.Delay(1500);
                    try
                    {
                        var tempPath = Path.Combine(TempPath, $"FujitsuInstaller_{azureServiceVersion.Replace('.', '_')}.msi");


                        if (!Directory.Exists(TempPath))
                        {
                            Directory.CreateDirectory(TempPath);
                        }

                        var serviceDownLoadURL = Path.Combine(configuration.ServiceDownLoadURL, $"FujitsuInstaller_{azureServiceVersion.Replace('.', '_')}.msi");


                        using (var client = new HttpClient())
                        {
                            if (!string.IsNullOrEmpty(configuration.ServiceDownLoadUserName) && !string.IsNullOrEmpty(configuration.ServiceDownLoadPassword))
                            {
                                var decryptedPassword = TokenEncryptDecrypt.Decrypt(configuration.ServiceDownLoadPassword);
                                var authenticationDetails = $"{configuration.ServiceDownLoadUserName}:{decryptedPassword}";
                                var base64Authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationDetails));

                                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Authentication);
                            }

                            byte[] fileBytes = await client.GetByteArrayAsync(serviceDownLoadURL);

                            await System.IO.File.WriteAllBytesAsync(tempPath, fileBytes);

                            status = InstallationStatus.DownLoadCompleted;
                            LogEvents($"Status : {status}");
                            LogEvents($"Downloaded new CDU version. Path='{tempPath}'");
                        }

                        var checkSumResponse = await CheckSizeAndSHA256();
                        if (checkSumResponse.Status)
                            return response;
                        else
                        {
                            response.Status = false;
                            response.Error = checkSumResponse.Error;
                            return response;
                        }

                    }
                    catch (Exception ex)
                    {
                        status = InstallationStatus.DownLoadFailed;
                        LogEvents($"State : {status}");
                        LogExceptions(" DownLoadServiceMSI inner. ", ex);
                        response.Status = false;
                        response.Error = ex.Message;
                        return response;
                    }

                }
                else
                {
                    DisplayProgress(autoUpdateRequired.Error);
                    await Task.Delay(2000);
                    DisplayProgress("Cleaning up the resources...");
                    LogEvents("Cleaning up the resources...");
                    await Task.Delay(2000);
                    await DisposeTempFiles();
                    response.Status = false;
                    response.Error = autoUpdateRequired.Error;
                    return response;
                }
            }
            catch (Exception ex)
            {
                LogExceptions(" DownLoadServiceMSI ", ex);
                DisplayProgress("Error in downloading service.");
                response.Status = false;
                response.Error = ex.Message;
                return response;
            }
        }

        private async Task<Respone> CheckSizeAndSHA256()
        {
            var response = new Respone() { Status = true };
            try
            {
                DisplayProgress("Validating Installer Checksum");
                LogEvents($"Validating Installer Checksum");

                await Task.Delay(1000);
                status = InstallationStatus.CheckDownLoadSizeSH256;
                LogEvents($"State : {status}");

                if (System.IO.File.Exists(Path.Combine(TempPath, $"FujitsuInstaller_{azureServiceVersion.Replace('.', '_')}.msi")))
                {
                    FileInfo fi = new(Path.Combine(TempPath, $"FujitsuInstaller_{azureServiceVersion.Replace('.', '_')}.msi"));

                    if (fi.Length == azureAutoUpdate.Size)
                    {
                        LogEvents($"Installer Size ='{fi.Length}'");
                        var SH256 = GetSHA256(Path.Combine(TempPath, $"FujitsuInstaller_{azureServiceVersion.Replace('.', '_')}.msi")).ToUpper();
                        if (SH256 == azureAutoUpdate.SH256)
                        {
                            LogEvents($"Installer SH256 ='{SH256}'");
                            response.Status = true;
                            return response;

                        }
                        else
                        {
                            DisplayProgress($"Installer SH256 validation failed.\nSH256 not matching.");
                            LogEvents($"Installer SH256 validation failed.Expected = '{azureAutoUpdate.SH256}', Actual='{SH256}'");
                            await Task.Delay(4000);
                            status = InstallationStatus.CheckDownLoadSizeSH256Failed;
                            LogEvents($"State : {status}");
                            response.Status = false;
                            response.Error = $"Installer SH256 validation failed.\nSH256 not matching.";
                            return response;
                        }
                    }
                    else
                    {
                        DisplayProgress($"Installer Checksum validation failed.\nExpected size={azureAutoUpdate.Size}\nActual={fi.Length}");
                        LogEvents($"Installer size validation failed.Expected size={azureAutoUpdate.Size},Actual={fi.Length}");
                        status = InstallationStatus.CheckDownLoadSizeSH256Failed;
                        LogEvents($"State : {status}");
                        await Task.Delay(4000);
                        response.Status = false;
                        response.Error = $"Installer Checksum validation failed.\nExpected size={azureAutoUpdate.Size}\nActual={fi.Length}";
                        return response;
                    }
                }
                else
                {
                    DisplayProgress($"File Not found in '{Path.Combine(TempPath, $"EZCoinDispenserInstaller_{azureServiceVersion.Replace('.', '_')}.msi")}' ");
                    LogEvents($"File Not found in ' {Path.Combine(TempPath, $"EZCoinDispenserInstaller_{azureServiceVersion.Replace('.', '_')}.msi")} ' ");
                    status = InstallationStatus.CheckDownLoadSizeSH256Failed;
                    LogEvents($"State : {status}");
                    await Task.Delay(4000);
                    response.Status = false;
                    response.Error = $"File Not found in '{Path.Combine(TempPath, $"EZCoinDispenserInstaller_{azureServiceVersion.Replace('.', '_')}.msi")}' ";
                    return response;
                }


            }
            catch (Exception ex)
            {
                LogExceptions(" CheckSizeAndSHA256 ", ex);
                DisplayProgress("Error in validating installer checksum.");
                LogEvents($"State : {status}");
                await Task.Delay(4000);
                response.Status = false;
                response.Error = ex.Message;
                return response;
            }
        }

        private string GetSHA256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);

                // Convert to hex string
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2")); // lowercase hex

                return sb.ToString();
            }
        }

        private async Task<Respone> CompareAutoUpdateConfiguration()
        {
            var updateRresponse = new Respone() { Status = true };
            try
            {
                string localautoUpdatefilePath = Path.Combine(configuration.ServiceInstallPath, "AutoUpdateConfig.txt");

                azureAutoUpdate = azureAutoUpdateConfig?.Where(x => x.Name == "EZCDU").FirstOrDefault();
                if (azureAutoUpdate != null)
                    azureServiceVersion = azureAutoUpdate?.Version;

                LogEvents($"Verifying local version with azure version...");

                if (System.IO.File.Exists(localautoUpdatefilePath))
                {
                    string fileContent = System.IO.File.ReadAllText(localautoUpdatefilePath);

                    localAutoUpdateConfig = System.Text.Json.JsonSerializer.Deserialize<List<AutoUpdate>>(fileContent);

                    if (localAutoUpdateConfig != null && localAutoUpdateConfig.Any())
                    {
                        try
                        {
                            localAutoUpdate = localAutoUpdateConfig.Where(x => x.Name == "EZCDU").FirstOrDefault();

                            if (localAutoUpdate != null && azureAutoUpdate != null && !string.IsNullOrEmpty(localAutoUpdate.Version) && !string.IsNullOrEmpty(azureAutoUpdate.Version))
                            {

                                localServiceVersion = localAutoUpdate.Version;
                                var azureversion = azureAutoUpdate.Version.Replace('.', ' ').Replace(" ", "");
                                var localVersion = localAutoUpdate.Version.Replace('.', ' ').Replace(" ", "");

                                var azureIntVersion = Int32.TryParse(azureversion, out Int32 azureResult);
                                var localIntVersion = Int32.TryParse(localVersion, out Int32 localResult);
                                LogEvents($"Local version : {localAutoUpdate.Version} , Azure Version : {azureAutoUpdate.Version}");

                                if (localIntVersion && azureIntVersion && azureResult > localResult)
                                {
                                    LogEvents($"Prompting confirmation to continue update.");
                                    DialogResult result = MessageBox.Show(
                                                              $"New Version {azureServiceVersion} available for download.\nDo you want to continue?",   // Message text
                                                              "Confirmation",              // Title of the MessageBox
                                                              MessageBoxButtons.OKCancel,  // Buttons to display
                                                              MessageBoxIcon.Question      // Icon (optional)
                                                          );

                                    if (result == DialogResult.OK)
                                    {
                                        LogEvents($"Ok selected to continue the update process.");
                                        return updateRresponse;
                                    }
                                    else
                                    {
                                        LogEvents($"Cancel selected.Cancelling the update process.");
                                        updateRresponse.Status = false;
                                        updateRresponse.Error = $"Cancelling the update process.";
                                        return updateRresponse;
                                    }
                                }
                                else if (localIntVersion && azureIntVersion && azureResult == localResult)
                                {
                                    LogEvents($"CDU version {localServiceVersion} is already up to date.No updates needed.");
                                    DisplayProgress($"Your CDU version {localServiceVersion} is already up to date.\nNo updates needed.");
                                    updateRresponse.Status = false;
                                    updateRresponse.Error = $"CDU version {localServiceVersion} is already up to date.\nNo updates needed.";
                                    return updateRresponse;
                                }
                                else if (localIntVersion && azureIntVersion && azureResult < localResult)
                                {
                                    LogEvents($"Prompting confirmation to continue update.");
                                    DialogResult result = MessageBox.Show(
                                                              $"Lower Version {azureServiceVersion} available for download.\nDo you want to downgrade the service?",   // Message text
                                                              "Confirmation",              // Title of the MessageBox
                                                              MessageBoxButtons.OKCancel,  // Buttons to display
                                                              MessageBoxIcon.Question      // Icon (optional)
                                                          );

                                    if (result == DialogResult.OK)
                                    {
                                        LogEvents($"Ok selected to continue the downgrade process.");

                                        return updateRresponse;
                                    }
                                    else
                                    {
                                        LogEvents($"Cancel selected.Cancelling the update process.");
                                        updateRresponse.Status = false;
                                        updateRresponse.Error = $"Cancelling the update process.";
                                        return updateRresponse;
                                    }
                                }
                                return updateRresponse;
                            }
                            else
                            {
                                updateRresponse.Status = false;
                                updateRresponse.Error = "Invalid data found in Local/Azure Auto update configuration file.";
                                return updateRresponse;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogExceptions("CompareAutoUpdateConfiguration in reading local/azure configuration", ex);
                            updateRresponse.Status = false;
                            updateRresponse.Error = ex.Message;
                            return updateRresponse;
                        }
                    }
                    else
                    {
                        return updateRresponse;
                    }
                }
                else
                {
                    LogEvents($"Local version not found. Initiating service installer process....");
                    return updateRresponse;
                }
            }
            catch (Exception ex)
            {
                LogExceptions("CompareAutoUpdateConfiguration in whole.", ex);
                updateRresponse.Status = false;
                updateRresponse.Error = ex.Message;
                return updateRresponse;

            }
        }

        private bool IsServiceInstalled(string serviceName)
        {
            // Get all installed services
            ServiceController[] services = ServiceController.GetServices();

            // Check if a service with the given name exists
            return services.Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        private bool CopyConfigToServiceFolder()
        {
            try
            {
                if (Directory.Exists(TempPath))
                {
                    string sourceFilePath = Path.Combine(TempPath, "FujitsuCDU.exe.config");

                    if (System.IO.File.Exists(sourceFilePath))
                    {
                        string destinationFilePath = Path.Combine(configuration.ServiceInstallPath, Path.GetFileName(sourceFilePath));
                        System.IO.File.Copy(sourceFilePath, destinationFilePath, true);
                        LogEvents($"Original EZCDU config file copied to '{destinationFilePath}' for service startup.");
                    }
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                LogExceptions(" CopyConfigToServiceFolder() ", ex);
                return false;
            }
        }

        private void CopyAutoUpdateConfigFile()
        {
            try
            {
                if (Directory.Exists(TempPath))
                {
                    string sourceAutoUpdateConfig = System.Text.Json.JsonSerializer.Serialize(azureAutoUpdateConfig);
                    string destinationAutoUpdateFilePath = Path.Combine(configuration.ServiceInstallPath, "AutoUpdateConfig.txt");
                    System.IO.File.WriteAllText(destinationAutoUpdateFilePath, sourceAutoUpdateConfig);
                    LogEvents($"Auto update config file copied to '{destinationAutoUpdateFilePath}' for future update.");
                }

            }
            catch (Exception ex)
            {
                LogExceptions(" CopyAutoUpdateConfigFile() ", ex);
                throw;
            }
        }

        private async Task<bool> UnistallCDU()
        {
            try
            {

                string uninstallString = FindUninstallString(appName);

                if (!string.IsNullOrEmpty(uninstallString))
                {
                    LogEvents($"UninstallString for {appName} : '{uninstallString}'");
                    LogEvents($"Extracting product code for '{uninstallString}'");

                    // Extract product code (GUID) from uninstall string
                    string productCode = ExtractProductCode(uninstallString);

                    if (!string.IsNullOrEmpty(productCode))
                    {
                        LogEvents($"Product code for '{uninstallString}' is '{productCode}'");
                        // Build silent uninstall command
                        string silentUninstall = $"msiexec.exe /x {productCode} /qn";

                        LogEvents($"Starting silent service uninstall process...");

                        ProcessStartInfo processStartInfo = new()
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {silentUninstall}",
                            Verb = "runas",  // Run as admin
                            UseShellExecute = true,
                            CreateNoWindow = true
                        };

                        using Process process = Process.Start(processStartInfo);
                        process?.WaitForExit();
                        if (process?.ExitCode == 0)
                        {
                            LogEvents($"Installer un-installed successfully.");
                        }
                        else
                        {
                            LogEvents($"Installer un-install failed. Process exit with code '{process.ExitCode}'");
                        }
                    }
                    else
                    {
                        DisplayProgress("Could not extract product code from uninstall string.");
                        LogEvents($"Could not extract product code from uninstall string.");
                    }
                }
                else
                {
                    LogEvents($"Application not found in uninstall registry for {appName}");
                    DisplayProgress($"Application not found in uninstall registry.");

                    DeleteDirectoryContentOnFailure(configuration.ServiceInstallPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogExceptions(" UnistallCDU ", ex);
                return false;
            }
        }

        private void DeleteExistingCDUFiles()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogExceptions(" UnistallCDU ", ex);
            }

        }

        private string FindUninstallString(string displayName)
        {
            string[] registryPaths =
            {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

            foreach (var path in registryPaths)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key == null) continue;

                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                        {
                            var name = subkey?.GetValue("DisplayName") as string;
                            if (!string.IsNullOrEmpty(name) && name.Contains(displayName))
                            {
                                return subkey.GetValue("UninstallString") as string;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private string ExtractProductCode(string uninstallString)
        {
            // Usually looks like: MsiExec.exe /I{GUID} or /X{GUID}
            int start = uninstallString.IndexOf('{');
            int end = uninstallString.IndexOf('}');
            if (start >= 0 && end > start)
            {
                return uninstallString.Substring(start, end - start + 1);
            }
            return null;
        }

        private async Task<bool> InstallNewEzCDU()
        {
            try
            {
                var tempPath = Path.Combine(TempPath, $"FujitsuInstaller_{azureServiceVersion.Replace('.', '_')}.msi");

                LogEvents($"Silent installation process started for FujitsuInstaller version '{azureServiceVersion}'");
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = "msiexec.exe",
                    UseShellExecute = false,
                    Verb = "runas",  // Run as admin
                    CreateNoWindow = true
                };

                processStartInfo.ArgumentList.Add("/i");
                processStartInfo.ArgumentList.Add(tempPath);
                processStartInfo.ArgumentList.Add("/qb");
                processStartInfo.ArgumentList.Add("/norestart");

                using Process process = Process.Start(processStartInfo);

                process?.WaitForExit();

                if (process?.ExitCode == 0 || process?.ExitCode == 3010)
                {
                    LogEvents($"Installer installed successfully.Process exit with code '0'");
                    return true;
                }
                else
                {
                    LogEvents($"Installer failed. Process exit with code '{process.ExitCode}'");
                    return false;
                }

            }
            catch (Exception ex)
            {
                LogExceptions(" InstallNewEzCDU ", ex);
                return false;
            }

        }

        private bool DeleteDirectoryContentOnFailure(string destinationDirectory)
        {
            var result = true;
            try
            {
                LogEvents("CDU existing files deleted on Uninstall process.");
                Directory.Delete(destinationDirectory, true);

                // Path to the user's desktop
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Shortcut name (without extension)
                string shortcutName = "FujitsuCDU";

                // Full path to the shortcut
                string shortcutPath = Path.Combine(desktopPath, shortcutName + ".lnk");

                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                    LogEvents("Shortcut removed on Uninstall process.");
                }

            }
            catch (Exception ex)
            {
                LogExceptions(" DeleteAndCopyDirectoryContentOnFailure ", ex);
                result = false;
            }

            return result;
        }

        private async Task<bool> ReinstalCDUOnErroredProcess()
        {
            try
            {
                DisplayProgress("Checking CDU status...");
                LogEvents($"Checking CDU status...");
                await Task.Delay(2000);

                status = InstallationStatus.ServiceFilesCopying;

                var result = DeleteDirectoryContentOnFailure(configuration.ServiceInstallPath);

                var copyResult = await CopyEZCDUFiles(TempPath, configuration.ServiceInstallPath);
                if (copyResult)
                {
                    status = InstallationStatus.ServiceFilesCopyingCompleted;
                    DisplayProgress("Backup files copied succesfully. ");
                    LogEvents($"Backup files copied succesfully to {configuration.ServiceInstallPath}");

                    await Task.Delay(1500);

                    DisplayProgress("Creating FujitsuCDU shortcut to desktop");
                    LogEvents($"Creating FujitsuCDU shortcut to desktop");
                    await Task.Delay(1000);

                    string shortcutName = "FujitsuCDU";
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string shortcutPath = Path.Combine(desktopPath, shortcutName + ".lnk");

                    CreateShortcut(shortcutPath, configuration.ServiceInstallPath, "");

                    DisplayProgress("Created FujitsuCDU shortcut");
                    LogEvents($"Created FujitsuCDU shortcut");

                    LogEvents($"CDU installed succesfully.");
                    status = InstallationStatus.CDUReInstalled;
                    LogEvents($"State : {status}");
                    await CompleteUpdateProcess();

                    return true;
                }
                else
                {
                    DisplayProgress("Failed to copy files to EZCDU folder");
                    LogEvents("Failed to copy files from Temp folder to EZCDU folder");
                    await Task.Delay(2000);
                    DisplayProgress("Cleaning up the resources...");
                    LogEvents("Cleaning up the resources...");
                    await Task.Delay(2000);
                    await DisposeTempFiles();
                    return false;
                }

            }
            catch (Exception ex)
            {
                LogExceptions(" ReinstalCDUOnErroredProcess ", ex);
                return false;
            }
        }

        private async Task DisposeTempFiles()
        {
            try
            {
                if (Directory.Exists(TempPath))
                    Directory.Delete(TempPath, true);

                LogEvents("Update Process completed.");
                DisplayProgress("Update Process completed.");
                await Task.Delay(3000);
                LogEvents("Closing Auto Update application.");
                Application.Exit();
            }
            catch (Exception)
            {
                LogEvents("Update Process completed.");
                DisplayProgress("Update Process completed.");
                await Task.Delay(3000);
                LogEvents("Closing Auto Update application.");
                Application.Exit();
            }
        }

        private void DisplayProgress(string message)
        {
            if (!lblStatus.IsDisposed)
            {
                lblStatus.BeginInvoke((Action)(() =>
                        {
                            lblStatus.Text = message;

                            lblStatus.Left = (lblStatus.Parent.ClientSize.Width - lblStatus.Width) / 2;
                            lblStatus.Top = (lblStatus.Parent.ClientSize.Height - lblStatus.Height) / 2;
                        }));
            }
        }

        private void LogEvents(string input)
        {
            Logger.LogWithNoLock($" {input}");
        }

        private void LogExceptions(string message, Exception ex)
        {
            Logger.LogExceptionWithNoLock($" Exception at {message}", ex);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            LogEvents("Clsoing Auto Update application.");
            Application.Exit();
        }

        private void btnFolderSelect_Click(object sender, EventArgs e)
        {

            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                // Optional: Set initial properties
                folderBrowserDialog.Description = "Select a folder for your files:";
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderBrowserDialog.SelectedPath;
                    txtServiceInstallPath.Text = selectedPath;
                    LogEvents($"Service Install Folder Path = '{selectedPath}'");
                }
            }
        }

        private async void CDUAutoUpdate_Load(object sender, EventArgs e)
        {
            try
            {
                LogEvents($"Version 1.0.0");
                LogEvents($"EZCDU auto update process started.");
                LogEvents($"Checking CDU status.");

                TempPath = Path.Combine(Path.GetTempPath(), "EZCDU");

                var executablePath = Path.GetDirectoryName(Application.ExecutablePath);

                var configPath = Path.Combine(executablePath, "UpdateEZCDUConfig.config");
                if (System.IO.File.Exists(configPath))
                {
                    LogEvents($"UpdateEZCDUConfig.config found.Reading configuration information.");
                    DisplayProgress("Checking CDU update...");

                    await Task.Delay(2000);

                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(UpdateEZCashServiceConfiguration));

                    using (var reader = new StreamReader(configPath))
                    {
                        configuration = (UpdateEZCashServiceConfiguration)xmlSerializer.Deserialize(reader);

                        if (configuration != null && !string.IsNullOrEmpty(configuration.ServiceDownLoadURL) && !string.IsNullOrEmpty(configuration.ServiceInstallPath))
                        {
                            var validCredentials = await ValidateDownLoadCredentials();
                            if (validCredentials)
                                await InitiateAutoUpdateProcess();
                            else
                                EnableControls();
                        }
                        else
                        {
                            LogEvents("Either config is null Or Version/Service download URL is empty.");
                            MessageBox.Show($"Either config is null Or Version/Service download URL is empty.");
                            Application.Exit();

                        }
                    }
                }
                else
                {
                    LogEvents($"UpdateEZCashServiceConfig.config not found.Loading controls to get config information.");
                    EnableControls();
                }
            }
            catch (Exception ex)
            {
                LogExceptions(" frmAutoUpdater_Load ", ex);
                MessageBox.Show($"Error Occured: {ex.Message}");
            }
        }


        private async Task<bool> ValidateDownLoadCredentials()
        {
            var result = true;
            try
            {
                var tempPath = Path.Combine(TempPath, "AutoUpdateConfig.txt");

                if (!Directory.Exists(TempPath))
                {
                    Directory.CreateDirectory(TempPath);
                }

                var serviceDownLoadURL = Path.Combine(configuration.ServiceDownLoadURL, "AutoUpdateConfig.txt");
                LogEvents($"Downloading Auto update configuration file from {configuration.ServiceDownLoadURL}");

                using (var client = new HttpClient())
                {
                    if (!string.IsNullOrEmpty(configuration.ServiceDownLoadUserName) && !string.IsNullOrEmpty(configuration.ServiceDownLoadPassword))
                    {
                        var decryptedPassword = TokenEncryptDecrypt.Decrypt(configuration.ServiceDownLoadPassword);
                        var authenticationDetails = $"{configuration.ServiceDownLoadUserName}:{decryptedPassword}";
                        var base64Authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationDetails));

                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Authentication);
                    }
                    var response = await client.GetAsync(serviceDownLoadURL, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        azureAutoUpdateConfig = System.Text.Json.JsonSerializer.Deserialize<List<AutoUpdate>>(contentStream);

                        using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(fileStream);
                            LogEvents($"Azure Auto update configuration file saved to {tempPath}");
                        }
                    }

                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("401"))
                    MessageBox.Show("Invalid Credentials.Re-enter username and password.", "Failure");
                else if (ex.Message.Contains("404"))
                    MessageBox.Show("Invalid download url. Re-enter valid download url.", "Failure");
                else
                    MessageBox.Show(ex.Message, "Failure");
                result = false;
                LogExceptions(" ValidateDownLoadCredentials() ", ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Failure");
                result = false;
                LogExceptions(" ValidateDownLoadCredentials() ", ex);
            }

            return result;
        }

        private void EnableControls()
        {
            try
            {
                lblStatus.Visible = false;
                lblService.Visible = true;
                lblVersion.Visible = true;
                lblUsername.Visible = true;
                lblPassword.Visible = true;
                txtServiceInstallPath.Visible = true;
                txtServiceDownloadURL.Visible = true;
                txtUserName.Visible = true;
                txtPassword.Visible = true;
                btnSaveConfiguration.Visible = true;
                btnCancel.Visible = true;
                btnFolderSelect.Visible = true;
                btnShowPassword.Visible = true;

                if (configuration != null)
                {
                    txtServiceDownloadURL.Text = configuration.ServiceDownLoadURL;
                    txtUserName.Text = configuration.ServiceDownLoadUserName;
                    txtPassword.Text = configuration.ServiceDownLoadPassword;
                    txtServiceInstallPath.Text = configuration.ServiceInstallPath;
                    if (!string.IsNullOrEmpty(configuration.ServiceInstallPath))
                    {
                        txtServiceInstallPath.Enabled = false;
                        btnFolderSelect.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {

            }

        }

        private bool CreateShortcut(string shortcutPath, string targetFileLocation, string description)
        {
            try
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.Description = description;
                shortcut.TargetPath = Path.Combine(configuration.ServiceInstallPath, "FujitsuCDU.exe");
                shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(Path.Combine(configuration.ServiceInstallPath, "FujitsuCDU.exe"));
                shortcut.IconLocation = Path.Combine(targetFileLocation, "lightning.ico"); // uses the app's own icon
                shortcut.Save();

                SetRunAsAdmin(shortcutPath);

                return true;
            }
            catch (Exception ex)
            {
                LogExceptions(" CreateShortcut ", ex);
                return false;
            }

        }

        static void SetRunAsAdmin(string shortcutPath)
        {
            // Open the shortcut file and modify its bytes to set RunAs flag
            var fileBytes = System.IO.File.ReadAllBytes(shortcutPath);

            // At offset 0x15, setting bit 0x20 enables "Run as Administrator"
            fileBytes[0x15] |= 0x20;

            System.IO.File.WriteAllBytes(shortcutPath, fileBytes);
        }

        private void btnShowPassword_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
        }
    }
    class ServiceHelper
    {
        public static string GetServicePath(string serviceName)
        {
            try
            {
                string key = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
                using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(key))
                {
                    if (rk != null)
                    {
                        string imagePath = rk.GetValue("ImagePath").ToString();
                        // Expand %SystemRoot% and quotes if present
                        return Environment.ExpandEnvironmentVariables(imagePath).Trim('"');
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }

        }
    }

    [Serializable]
    public class UpdateEZCashServiceConfiguration
    {
        public string ServiceInstallPath { get; set; }
        public string ServiceDownLoadURL { get; set; }
        public string ServiceDownLoadUserName { get; set; }
        public string ServiceDownLoadPassword { get; set; }
    }


    public class AutoUpdate
    {
        public string Version { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string SH256 { get; set; }
    }

    public class Respone
    {
        public bool Status { get; set; }
        public string Error { get; set; }
    }
}
