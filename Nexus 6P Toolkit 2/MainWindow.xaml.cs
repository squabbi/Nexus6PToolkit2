using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Net;
using aUpdater;
using INI;
using AndroidCtrl;
using AndroidCtrl.ADB;
using AndroidCtrl.Tools;
using AndroidCtrl.Fastboot;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Media;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Nexus_6P_Toolkit_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show(
                    "There seems to be another instance of the toolkit running. Please make sure it is not running in the background.",
                    "Another Instance is running", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();             
            }

            InitializeComponent();
        }
        //Device names
        private string fullDeviceName = "Nexus 6P (Huawei Nexus 6P)";
        private string codeDeviceName = "angler";
        //Download int
        private int retryLvl = 0;
        //Factory image options
        private string stockVersion;
        private string stockEdition;
        private string stockUniqueID;
        private string stockExtension;
        //private string pStockMD5;
        private string pStockFileName;
        private string isStockDev;
        //private string stockExtension;
        //Factory image paths
        private string bootloaderPath;
        private string radioPath;
        private string imagePath;
        private string bootPath;
        private string cachePath;
        private string recoveryPath;
        private string systemPath;
        private string vendorPath;
        //private string userdataPath;
        //Factory image abilities
        private bool factoryEzMode;
        private bool flashBootloader;
        private bool flashRadio;
        private bool flashBoot;
        private bool flashCache;
        private bool formatUserdata;
        private bool flashRecovery;
        private bool flashSystem;
        private bool flashVendor;
        //TWRP options
        private string twrpVersion;
        private string pTWRPMD5;
        private string pTWRPFileName;
        //SuperSU options
        private string suVersion;
        private string suType;
        private bool suManInstall;
        private string pSuFileName;
        //SHA string
        public string supSHA;
        //OTA options
        private string otaMD5;
        private string otaID;
        private string otaFromVer;
        private string otaToVer;
        private string pOTAFileName;
        private bool checkOTAHash;
        //Full OTA options
        private bool isFOTA;
        private string fOtaSHA;
        private string fOtaID;
        private string fOtaVersion;
        private string pfOTAFileName;
        //Modified boot image options
        private string modBootVersion;
        private string modBootExportCode;
        private string pModBootFileName;
        //Webclients for downloads
        private WebClient _stockClient;
        private WebClient _twrpClient;
        private WebClient _suClient;
        private WebClient _otaClient;
        private WebClient _modBootClient;
        private WebClient _driverClient;
        //Lists from ini files
        List<String> twrpListStrLineElements;
        List<String> stockListStrLineElements;
        List<String> suListStrLineElements;
        List<String> otaListStrLineElements;
        List<String> modBootListStrLineElements;
        //App driectory
        public static string appPath = System.AppDomain.CurrentDomain.BaseDirectory;
        //private BackgroundWorker mWorker;

        //Driver installation API        
        [DllImport("DIFXApi.dll", CharSet = CharSet.Unicode)]
        public static extern Int32 DriverPackagePreinstall(string DriverPackageInfPath, Int32 Flags);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            //Set Version Label
            menuVersion.Header = "Version: " + version;
            this.Title = string.Format("Squabbi's Nexus 6P Toolkit - v{0}", version);
            cAppend("Set version.");

            startupProcesses();            

            ////Check for updates
            if (!File.Exists("./debug"))
            {
                cAppend("Checking for updates...\n");
                Updater upd = new Updater();
                upd.UpdateUrl = string.Format("https://s.basketbuild.com/dl/devs?dl=squabbi/{0}/toolkit/UpdateInfo.dat", codeDeviceName);
                upd.CheckForUpdates();
            }
            else
                cAppend("debug file detected. Skipping update.\n");
        }

        public async void startupProcesses()
        {
            try
            {
                await Task.Run(() =>
                {
                    //Checks and Creates folders
                    cAppend("Checking folder structure.");
                    CheckFileSystem();
                    //Download Stock list and add entries to combobox
                    if (!File.Exists("./debug"))
                    {
                        cAppend("Downloading file lists.");
                        downloadCachedFiles();
                    }
                    else
                        cAppend("debug file detected. Skipping file list download...");
                    cAppend("Populating lists...");
                    getBuildLists();
                    cAppend("Deploying ADB & Fastboot.");
                    CheckandDeploy();
                    cAppend("Starting detection service.");
                    DeviceDetectionService();
                    cAppend("INFO: Finished running initial startup. READY.");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            

        }

        public void CheckandDeploy()
        {
            if (ADB.IntegrityCheck() == false)
            {
                Deploy.ADB();
            }
            if (Fastboot.IntegrityCheck() == false)
            {
                Deploy.Fastboot();
            }
            // Check if ADB is running
            if (ADB.IsStarted)
            {
                // Stop ADB
                ADB.Stop();

                // Force Stop ADB
                ADB.Stop(true);
            }
            else
            {
                // Start ADB
                ADB.Start();
            }
        }

        public void downloadCachedFiles()
        {
            try
            {
                //string update_url = "https://s.basketbuild.com/dl/devs?dl=squabbi/toolkits/listVersion.ctl";

                //using (var listVersionWEBCLIENT = new WebClient())
                //{
                //    listVersionWEBCLIENT.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                //    string onlineVersionString = listVersionWEBCLIENT.DownloadString(update_url);

                //    string[] onlineVersionStringARRAY = onlineVersionString.Split(',');

                //    //MessageBox.Show(onlineVersionString);
                //    //MessageBox.Show("security: " + onlineVersionStringARRAY[0]);
                //    //MessageBox.Show("version: " + onlineVersionStringARRAY[1]);
                //    //string currentVersion 

                //    //Check security string
                //    if (onlineVersionStringARRAY[0] == "if0L4U9vTS")
                //    {
                //int onlineVersion = 0;
                //int savedVersion = 0;

                //if (Int32.TryParse(Squabbi.Toolkit.Nexus6P.Properties.Settings.Default["listVersion"].ToString(), out savedVersion))
                //{
                //    if (Int32.TryParse(onlineVersionStringARRAY[1], out onlineVersion))
                //    {
                //        if (savedVersion < onlineVersion)
                //        {
                //            //Start downloading lists
                using (WebClient client = new WebClient())
                {
                    //Proxy for WebClient
                    IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                    if (defaultProxy != null)
                    {
                        defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                        client.Proxy = defaultProxy;
                    }

                    cAppend("Downloading list from BasketBuild");

                    client.DownloadFile("https://s.basketbuild.com/dl/devs?dl=squabbi/toolkits/StockBuildList.ini"
                    , "./Data/.cached/StockBuildList.ini");
                    client.DownloadFile("https://s.basketbuild.com/dl/devs?dl=squabbi/toolkits/TWRPBuildList.ini"
                        , "./Data/.cached/TWRPBuildList.ini");
                    client.DownloadFile("https://s.basketbuild.com/dl/devs?dl=squabbi/superSU/SuBuildList.ini"
                        , "./Data/.cached/SuBuildList.ini");
                    client.DownloadFile("https://s.basketbuild.com/dl/devs?dl=squabbi/toolkits/OTABuildList.ini"
                        , "./Data/.cached/OTABuildList.ini");
                    client.DownloadFile("https://s.basketbuild.com/dl/devs?dl=squabbi/toolkits/ModBootBuildList.ini"
                        , "./Data/.cached/ModBootBuildList.ini");
                    client.Dispose();

                    //cAppend(string.Format("INFO: Setting online version as current version. {0}", onlineVersion));
                    //Squabbi.Toolkit.Nexus6P.Properties.Settings.Default["listVersion"] = onlineVersion;
                    //Squabbi.Toolkit.Nexus6P.Properties.Settings.Default.Save();
                    //                }
                    //            }
                    //            else
                    //            {
                    //                cAppend("Lists already up to date.");
                    //            }
                    //        }
                    //        else
                    //        {
                    //            MessageBox.Show("Failed to convert online version to int32.");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        MessageBox.Show("Failed to convert current version to int32.");
                    //    }
                    //}
                    //    else
                    //    {
                    //        MessageBox.Show("WARN: Secuirty string mismatch. Please report this on XDA. Downloading files and newer lists will not work.");
                    //    }
                    //}


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unexpected error.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void getBuildLists()
        {
            //Check for required string in the first line
            string secStringCheckSBL = File.ReadLines("./Data/.cached/StockBuildList.ini").First(); // gets the first line from file.
            if (secStringCheckSBL == "if0L4U9vTS") // check if first line matches
            {
                // success
                IniFileName iniItems = new IniFileName("./Data/.cached/StockBuildList.ini");
                string[] iniValues = iniItems.GetEntryNames(fullDeviceName);

                //Foreach entry make a combobox item
                foreach (string iniValue in iniValues)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        stockBuildList.Items.Add(iniValue);
                    });
                }

                cAppend("INFO: Loaded StockBuildList");
            }
            else
            {
                // fail
                cAppend("WARN: Security string not found for StockBuildList.");
            }

            string secStringCheckTBL = File.ReadLines("./Data/.cached/TWRPBuildList.ini").First(); // gets the first line from file.
            if (secStringCheckTBL == "if0L4U9vTS") // check if first line matches
            {
                // success
                IniFileName iniItems = new IniFileName("./Data/.cached/TWRPBuildList.ini");
                string[] iniValues = iniItems.GetEntryNames(fullDeviceName);
                //Foreach entry make a combobox item
                foreach (string iniValue in iniValues)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        twrpBuildList.Items.Add(iniValue);
                    });
                }
                cAppend("INFO: Loaded TWRPBuildList");
            }
            else
            {
                // fail
                cAppend("WARN: Security string not found for TWRPBuildList.");
            }

            string secStringCheckSuBL = File.ReadLines("./Data/.cached/SuBuildList.ini").First(); // gets the first line from file.
            if (secStringCheckSuBL == "if0L4U9vTS") // check if first line matches
            {
                // success
                IniFileName iniItems = new IniFileName("./Data/.cached/SuBuildList.ini");
                string[] iniSecValue = iniItems.GetSectionNames();
                string[] iniValues = iniItems.GetEntryNames(iniSecValue[0]);

                //Foreach entry make a combobox item
                foreach (string iniValue in iniValues)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        supersuBuildList.Items.Add(iniValue);
                    });
                }
                cAppend("INFO: Loaded SuBuildList");
            }
            else
            {
                // fail
                cAppend("WARN: Security string not found for SuBuildList.");
            }

            string secStringCheckOBL = File.ReadLines("./Data/.cached/OTABuildList.ini").First(); // gets the first line from file.
            if (secStringCheckOBL == "if0L4U9vTS") // check if first line matches
            {
                // success
                IniFileName iniItems = new IniFileName("./Data/.cached/OTABuildList.ini");
                string[] iniValues = iniItems.GetEntryNames(fullDeviceName);
                //Foreach entry make a combobox item
                foreach (string iniValue in iniValues)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        otaBuildList.Items.Add(iniValue);
                    });
                }
                cAppend("INFO: Loaded OTABuildList");
            }
            else
            {
                // fail
                cAppend("WARN: Security string not found for OTABuildList.");
            }

            string secStringCheckMBL = File.ReadLines("./Data/.cached/ModBootBuildList.ini").First(); // gets the first line from file.
            if (secStringCheckMBL == "if0L4U9vTS") // check if first line matches specified languages
            {
                // success
                IniFileName iniItems = new IniFileName("./Data/.cached/ModBootBuildList.ini");
                string[] iniValues = iniItems.GetEntryNames(fullDeviceName);
                //Foreach entry make a combobox item
                foreach (string iniValue in iniValues)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        modBootBuildList.Items.Add(iniValue);
                    });
                }
                cAppend("INFO: Loaded ModBootBuildList");
            }
            else
            {
                // fail
                cAppend("WARN: Security string not found for ModBootBuildList.");
            }
        }

        public void Add(List<string> msg)
        {
            foreach (string tmp in msg)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    console.Document.Blocks.Add(new Paragraph(new Run(tmp.Replace("(bootloader) ", ""))));
                });
            }
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                console.ScrollToEnd();
            });
        }

        public void cAppend(string message)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                console.AppendText(string.Format("\n{0} :: {1}", DateTime.Now, message));
                console.ScrollToEnd();
            });
        }

        private void CheckFileSystem()
        {
            try
            {
                string[] neededDirectories = new string[] { "Data/", "Data/Logs", "Data/Downloads", "Data/Downloads/Stock", "Data/.cached",
                "Data/Backups", "Data/Downloads/TWRP", "Data/Downloads/SU", "Data/Downloads/OTA", "Data/Downloads/ModBoot/"};
                foreach (string dir in neededDirectories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
            }
            catch (Exception ex)
            {
                //Declare variable for AppData folder
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                MessageBox.Show(
                    "An error has occured. A this point the program should be able to read and write in this directory. Check the the output log in the toolkit's 'appdata'" +
                    " folder by pressing OK. \n\n You can try re-running the toolkit as an Administrator.",
                    "Folder Creation Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                string fileDateTime = DateTime.Now.ToString("MMddyyyy") + "_" + DateTime.Now.ToString("HHmmss");
                var file = new StreamWriter(appData + "/SquabbiXDA/StartUp Error " + fileDateTime + ".txt");
                file.WriteLine(ex);
                file.Close();
                Process.Start(appData + "/SquabbiXDA/");
            }
        }

        //public void CheckDeviceState(List<DataModelDevicesItem> devices)
        //{
        //    App.Current.Dispatcher.Invoke((Action)delegate
        //    {
        //        // Here we refresh our combobox
        //        SetDeviceList();
        //    });
        //}

        private void SetDeviceList()
        {
            string active = String.Empty;
            if (deviceselector.Items.Count != 0)
            {
                active = ((DataModelDevicesItem)deviceselector.SelectedItem).Serial;
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                // Here we refresh our combobox
                deviceselector.Items.Clear();
            });

            // This will get the currently connected ADB devices
            List<DataModelDevicesItem> adbDevices = ADB.Devices();

            // This will get the currently connected Fastboot devices
            List<DataModelDevicesItem> fastbootDevices = Fastboot.Devices();

            foreach (DataModelDevicesItem device in adbDevices)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    // here goes the add command ;)
                    deviceselector.Items.Add(device);
                });
            }
            foreach (DataModelDevicesItem device in fastbootDevices)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    deviceselector.Items.Add(device);
                });
            }
            if (deviceselector.Items.Count != 0)
            {
                int i = 0;
                bool empty = true;
                foreach (DataModelDevicesItem device in deviceselector.Items)
                {
                    if (device.Serial == active)
                    {
                        empty = false;
                        deviceselector.SelectedIndex = i;
                        break;
                    }
                    i++;
                }
                if (empty)
                {

                    // This calls will select the BASE class if we have no connected devices
                    ADB.SelectDevice();
                    Fastboot.SelectDevice();

                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        deviceselector.SelectedIndex = 0;
                    });
                }
            }
        }

        private void DeviceDetectionService()
        {
            ADB.Start();

            // Here we initiate the BASE Fastboot instance
            Fastboot.Instance();

            //This will starte a thread which checks every 10 sec for connected devices and call the given callback
            if (Fastboot.ConnectionMonitor.Start())
            {
                //Here we define our callback function which will be raised if a device connects or disconnects
                Fastboot.ConnectionMonitor.Callback += ConnectionMonitorCallback;

                // Here we check if ADB is running and initiate the BASE ADB instance (IsStarted() will everytime check if the BASE ADB class exists, if not it will create it)
                if (ADB.IsStarted)
                {
                    //Here we check for connected devices
                    SetDeviceList();

                    //This will starte a thread which checks every 10 sec for connected devices and call the given callback
                    if (ADB.ConnectionMonitor.Start())
                    {
                        //Here we define our callback function which will be raised if a device connects or disconnects
                        ADB.ConnectionMonitor.Callback += ConnectionMonitorCallback;
                    }
                }
            }
        }

        public void ConnectionMonitorCallback(object sender, ConnectionMonitorArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                // Do what u want with the "List<DataModelDevicesItem> e.Devices"
                // The "sender" is a "string" and returns "adb" or "fastboot"
                SetDeviceList();

                

            });
        }

        private void SelectDeviceInstance(object sender, SelectionChangedEventArgs e)
        {
            if (deviceselector.Items.Count != 0)
            {
                DataModelDevicesItem device = (DataModelDevicesItem)deviceselector.SelectedItem;

                // This will select the given device in the Fastboot and ADB class
                Fastboot.SelectDevice(device.Serial);
                ADB.SelectDevice(device.Serial);
            }
        }

        private async void flashModBootImage()
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var controllerTWRPflash = await this.ShowProgressAsync("Flashing Modified Boot Image...", "");
            controllerTWRPflash.SetIndeterminate();

            cAppend("Flashing modified boot image...");
            await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.BOOT, "./Data/Downloads/ModBoot/" + pModBootFileName));
            cAppend("Done flashing modified boot image.\n");
            await controllerTWRPflash.CloseAsync();

            var result = await this.ShowMessageAsync("Flash Successful!", "Would you like to reboot now?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
            if (result == MessageDialogResult.Affirmative)
            {
                cAppend("Rebooting device...");
                await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.REBOOT));
            }
        }

        private async void flashOTAs(string func)
        {
            IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
            if (state == IDDeviceState.DEVICE)
            {
                var controllerOTAflash = await this.ShowProgressAsync("Rebooting to Recovery...", "Flashing OTA via Sideload");
                controllerOTAflash.SetIndeterminate();

                cAppend("Rebooting to recovery now...");
                await Task.Run(() => ADB.Instance().Reboot(IDBoot.RECOVERY));
                controllerOTAflash.SetTitle("Waiting for device...");
                cAppend("Waiting for device.");
                await Task.Run(() => ADB.WaitForDevice());
                controllerOTAflash.SetTitle("Sending ZIP to sideload...");
                cAppend("Flashing OTA...");
                await Task.Run(() => ADB.Instance().Sideload(Path.Combine("./Data/Downloads/OTA", pOTAFileName)));
                await controllerOTAflash.CloseAsync();
                cAppend("Finished sideloading.\n");
            }
            else if (state == IDDeviceState.FASTBOOT)
            {
                cAppend("You'll need to be in the recovery...");
                cAppend("Please reboot your phone into recovery and try again.\n");
            }
            else if (state == IDDeviceState.RECOVERY)
            {
                var controllerOTAflash = await this.ShowProgressAsync("Rebooting to Recovery...", "Flashing OTA via Sideload");
                controllerOTAflash.SetIndeterminate();

                cAppend("Waiting for device.");
                await Task.Run(() => ADB.WaitForDevice());
                controllerOTAflash.SetTitle("Sending ZIP to sideload...");
                cAppend("Flashing OTA...");
                await Task.Run(() => ADB.Instance().Sideload(Path.Combine("./Data/Downloads/OTA", pOTAFileName)));
                await controllerOTAflash.CloseAsync();
                cAppend("Finished sideloading.\n");
            }
            else
            {
                cAppend("Your device was not detected by the Connection Monitor, you may have not installed the drivers correctly.");

                var dictionary = new ResourceDictionary();
                dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

                var mySettings = new MetroDialogSettings
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No",
                    SuppressDefaultResources = true,
                    CustomResourceDictionary = dictionary
                };

                var result = await this.ShowMessageAsync("No device detected",
                    "You can press Yes if you are sure your device is in the correct state and connected. Otherwise, press No to try again later.",
                            MessageDialogStyle.AffirmativeAndNegative, mySettings);
                if (result == MessageDialogResult.Affirmative)
                {
                    //Continue with blind install
                    await Task.Run(() => ADB.WaitForDevice());
                    cAppend("Flashing OTA...");
                    await Task.Run(() => ADB.Instance().Sideload(Path.Combine("./Data/Downloads/OTA", pOTAFileName)));
                    cAppend("Finished sideloading.\n");
                }
            }
        }

        private async void flashTWRP()
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var controllerTWRPflash = await this.ShowProgressAsync("Flashing Recovery...", "");
            controllerTWRPflash.SetIndeterminate();

            cAppend("Flashing TWRP...");
            await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.RECOVERY, "./Data/Downloads/TWRP/" + pTWRPFileName));
            cAppend("Done flashing TWRP.\n");
            await controllerTWRPflash.CloseAsync();

            var result = await this.ShowMessageAsync("Flash Successful!", "Would you like to reboot now?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
            if (result == MessageDialogResult.Affirmative)
            {
                cAppend("Rebooting device...");
                await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.REBOOT));
            }
        }

        private async void flashSuperSU()
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var manSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
            if (state == IDDeviceState.DEVICE)
            {
                var controllerSUflash = await this.ShowProgressAsync(string.Format("Pushing {0}...", pSuFileName), "Flashing OTA via Sideload");
                controllerSUflash.SetIndeterminate();

                cAppend(string.Format("Pushing {0} to your device...", pSuFileName));
                await Task.Run(() => ADB.WaitForDevice());
                await Task.Run(() => ADB.Instance().Push(Path.Combine("./Data/Downloads/SU", pSuFileName), "/sdcard/"));
                cAppend("Rebooting to recovery...");
                controllerSUflash.SetTitle("Rebooting to recovery...");
                await Task.Run(() => ADB.Instance().Reboot(IDBoot.RECOVERY));
                if (suManInstall == false)
                {
                    cAppend("Waiting for device...");
                    await Task.Run(() => ADB.WaitForDevice());
                    controllerSUflash.SetTitle(string.Format("Flashing {0}...", pSuFileName));
                    cAppend(string.Format("Flashing {0}...", pSuFileName));
                    await Task.Run(() => ADB.Instance().ShellCmd(string.Format("twrp install /sdcard/{0}", pSuFileName)));
                    cAppend("Done!");
                    await controllerSUflash.CloseAsync();
                    var result = await this.ShowMessageAsync("Flash Completed!",
                    string.Format("Did you see the flashing screen on your phone? If not, you may need to manually install {0} from /sdcard. \n\nPressing Yes will reboot your phone.")
                    , MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        cAppend("Rebooting...");
                        //await Task.Run(() => ADB)
                    }
                }
                else
                {
                    await this.ShowMessageAsync("Rebooting into recovery", "Locate the SuperSU.zip in /sdcard/ and flash it!", MessageDialogStyle.Affirmative, manSettings);
                }
            }
            else if (state == IDDeviceState.FASTBOOT)
            {
                cAppend("You'll need to be in the recovery...");
                cAppend("Please reboot your phone into recovery and try again.\n");
            }
            else if (state == IDDeviceState.RECOVERY)
            {
                var controllerSUflash = await this.ShowProgressAsync(string.Format("Pushing {0}...", pSuFileName), "Flashing OTA via Sideload");
                controllerSUflash.SetIndeterminate();

                cAppend(string.Format("Pushing {0} to your device...", pSuFileName));
                //await Task.Run(() => ADB.WaitForDevice());
                await Task.Run(() => ADB.Instance().PushPullUTF8.Push(Path.Combine("./Data/Downloads/SU", pSuFileName), "/sdcard/"));
                if (suManInstall == false)
                {
                    cAppend("Waiting for device...");
                    await Task.Run(() => ADB.WaitForDevice());
                    controllerSUflash.SetTitle(string.Format("Flashing {0}...", pSuFileName));
                    cAppend(string.Format("Flashing {0}...", pSuFileName));
                    await Task.Run(() => ADB.Instance().ShellCmd(string.Format("twrp install /sdcard/{0}", pSuFileName)));
                    cAppend("Done!");
                    await controllerSUflash.CloseAsync();
                    var result = await this.ShowMessageAsync("Flash Completed!",
                    string.Format("Did you see the flashing screen on your phone? If not, you may need to manually install {0} from /sdcard. \n\nPressing Yes will reboot your phone.")
                    , MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        cAppend("Rebooting...");
                        //await Task.Run(() => ADB)
                    }
                }
                else
                {
                    await this.ShowMessageAsync("Rebooting into recovery", "Locate the SuperSU.zip in /sdcard/ and flash it!", MessageDialogStyle.Affirmative, manSettings);
                }
                await controllerSUflash.CloseAsync();
            }
            else
            {
                cAppend("Your device was not detected by the Connection Monitor, you may have not installed the drivers correctly.");

                var result = await this.ShowMessageAsync("No device detected",
                    "You can press Yes if you are sure your device is in the correct state and connected. Otherwise, press No to try again later.",
                            MessageDialogStyle.AffirmativeAndNegative, mySettings);
                if (result == MessageDialogResult.Affirmative)
                {
                    if (suManInstall == false)
                    {
                        var controllerSUflash = await this.ShowProgressAsync("Waiting for device...", "");
                        cAppend("Waiting for device...");
                        await Task.Run(() => ADB.WaitForDevice());
                        controllerSUflash.SetTitle(string.Format("Flashing {0}...", pSuFileName));
                        cAppend(string.Format("Flashing {0}...", pSuFileName));
                        await Task.Run(() => ADB.Instance().ShellCmd(string.Format("twrp install /sdcard/{0}", pSuFileName)));
                        cAppend("Done!");
                        await controllerSUflash.CloseAsync();
                        var result2 = await this.ShowMessageAsync("Flash Completed!",
                        string.Format("Did you see the flashing screen on your phone? If not, you may need to manually install {0} from /sdcard. \n\nPressing Yes will reboot your phone.")
                        , MessageDialogStyle.AffirmativeAndNegative, mySettings);
                        if (result2 == MessageDialogResult.Affirmative)
                        {
                            cAppend("Rebooting...");
                            //await Task.Run(() => ADB)
                        }
                    }
                    else
                    {
                        await this.ShowMessageAsync("Rebooting into recovery", "Locate the SuperSU.zip in /sdcard/ and flash it!", MessageDialogStyle.Affirmative, manSettings);
                    }
                }
            }
        }

        private async void flashFactoryImage(bool user)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var existingSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Extract again",
                NegativeButtonText = "Use existing files",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var controllerFactoryflash = await this.ShowProgressAsync("Extracting factory image...", pStockFileName);
            controllerFactoryflash.SetIndeterminate();
            string stockImage;

            //Insert Background worker here


            if (user == false)
            {
                 stockImage = string.Format("{0}/Data/Downloads/Stock/{1}", appPath, pStockFileName);
            }
            else
            {
                stockImage = pStockFileName; //This one will include the full path
            }

            if (!Directory.Exists("./Data/Downloads/Stock/.extracted"))
                Directory.CreateDirectory("./Data/Downloads/Stock/.extracted");

            //Check extension of custom image
            if (!Directory.Exists(stockImage))
            {
                cAppend("Extracting factory image...\n");
                if (!string.IsNullOrEmpty(stockExtension))
                {
                    if (stockExtension == "tgz")
                    {
                        //Start extraction
                        await Task.Run(() => extractTGZ(stockImage, "./Data/Downloads/Stock/.extracted/"));
                    }
                    else if (stockExtension == "zip")
                    {
                        //Start extraction
                        await Task.Run(() => FastZipUnpack(stockImage, "./Data/Downloads/Stock/.extracted/"));
                    }
                }
                else
                {
                    MessageBox.Show("This is unhandled! Tell me on XDA: 'stock extention not set'.\n\n" +
                        "The toolkit will now close.");
                    Application.Current.Shutdown();
                }

                //Start extraction

                //await Task.Run(() => FastZipUnpack(stockImage, "./Data/Downloads/Stock/.extracted/"));
            }
            else
            {
                cAppend("Existing factory image folder found.");
                if (Directory.GetFiles(stockImage).Length > 0)
                {
                    var resultExtract = await this.ShowMessageAsync("Extract factory images again?", string.Format(@"The folder already contains files from the {0} image.
                          If you have previously flased the image before, the files should be OK. However there is no way that the toolkit can tell if the files
                            are corrupt. Please check manually in the Data/Downloads/Stock/.extracted/{1} folder.", stockImage, pStockFileName), MessageDialogStyle.AffirmativeAndNegative,
                            existingSettings);
                    if (resultExtract == MessageDialogResult.Affirmative)
                    {
                        cAppend("Extracting factory image...\n");
                        //if (!string.IsNullOrEmpty(stockExtension))
                        //{
                        //    if (stockExtension == "tgz")
                        //    {
                        //        await Task.Run(() => extractTGZ(stockImage, "./Data/Downloads/Stock/.extracted/"));
                        //    }
                        //    else if (stockExtension == "zip")
                        //    {
                        //        await Task.Run(() => FastZipUnpack(stockImage, "./Data/Downloads/Stock/.extracted/"));
                        //    }
                        //}
                        //else
                        //{
                        //    MessageBox.Show("This is unhandled! Tell me on XDA: 'stock extention not set'.\n\n" +
                        //        "The toolkit will now close.");
                        //    Application.Current.Shutdown();
                        //}

                        //Start extraction
                        await Task.Run(() => FastZipUnpack(stockImage, "./Data/Downloads/Stock/.extracted/"));

                    }
                    else
                    {
                        cAppend("Continuing with existing files...");
                    }
                }
            }

            string rStockFolder = Path.Combine("./Data/Downloads/Stock/.extracted/", string.Format("{0}-{1}", codeDeviceName, stockVersion));

            if (factoryEzMode == true)
            {
                controllerFactoryflash.SetTitle("Flashing bootloader...");
                controllerFactoryflash.SetMessage("Progress 1/3");
            }
            else
            {
                controllerFactoryflash.SetTitle("Flashing bootloader...");
                controllerFactoryflash.SetMessage("Progress 1/8");
            }

            if (flashBootloader == true)
            {
                string[] fBootloader = System.IO.Directory.GetFiles(rStockFolder, "*bootloader*", System.IO.SearchOption.TopDirectoryOnly);
                if (fBootloader.Length > 0)
                {
                    cAppend("Flashing bootloader...");
                    bootloaderPath = fBootloader[0].ToString();
                    await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.BOOTLOADER, bootloaderPath));

                    controllerFactoryflash.SetIndeterminate();
                    controllerFactoryflash.SetTitle("Rebooting to bootloader...");

                    await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.BOOTLOADER));
                }
            }
            else
            {
                cAppend("Skipping bootloader...");
            }         

            if (flashRadio == true)
            {
                string[] fRadio = System.IO.Directory.GetFiles(rStockFolder, "*radio*", System.IO.SearchOption.TopDirectoryOnly);
                if (fRadio.Length > 0)
                {
                    if (factoryEzMode == true)
                    {
                        controllerFactoryflash.SetTitle("Flashing radio...");
                        controllerFactoryflash.SetMessage("Progress 2/3");
                    }
                    else
                    {
                        controllerFactoryflash.SetTitle("Flashing radio...");
                        controllerFactoryflash.SetMessage("Progress 2/8");
                    }
                   
                    radioPath = fRadio[0].ToString();
                    await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.RADIO, radioPath));

                    controllerFactoryflash.SetIndeterminate();
                    controllerFactoryflash.SetTitle("Rebooting to bootloader...");

                    await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.BOOTLOADER));
                }
            }
            else
            {
                cAppend("Skipping radio...");
            }

            if (factoryEzMode == true)
            {
                controllerFactoryflash.SetTitle("Flashing Update Image...");
                controllerFactoryflash.SetMessage("Progress 3/3");

                

                    string[] fImage = System.IO.Directory.GetFiles(rStockFolder, "*image*", System.IO.SearchOption.TopDirectoryOnly);
                if (fImage.Length > 0)
                {
                    cAppend("Flashing image...");
                    imagePath = fImage[0].ToString();

                    if (formatUserdata == true)
                    {
                        controllerFactoryflash.SetTitle("Flashing image, wiping userdata...");
                        await Task.Run(() => Fastboot.Instance().Execute(string.Format("update -w {0}", imagePath)));
                    }
                    else
                    {
                        controllerFactoryflash.SetTitle("Flashing image, keeping userdata...");
                        await Task.Run(() => Fastboot.Instance().Execute(string.Format("update {0}", imagePath)));
                    }
                }
            }
            else
            {
                controllerFactoryflash.SetTitle("Extracting images...");
                controllerFactoryflash.SetMessage("Progress 2.5/8");

                string[] fImage = System.IO.Directory.GetFiles(rStockFolder, "*image*", System.IO.SearchOption.TopDirectoryOnly);
                if (fImage.Length > 0)
                {
                    cAppend("Extracting images...");
                    imagePath = fImage[0].ToString();
                    await Task.Run(() => FastZipUnpack(imagePath, rStockFolder));
                }

                if (flashBoot == true)
                {
                    controllerFactoryflash.SetTitle("Flashing boot...");
                    controllerFactoryflash.SetMessage("Progress 3/8");

                    cAppend("Flashing boot...");
                    string[] fBoot = System.IO.Directory.GetFiles(rStockFolder, "*boot*", System.IO.SearchOption.TopDirectoryOnly);
                    if (fBoot.Length > 0)
                    {
                        bootPath = fBoot[0].ToString();
                        await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.BOOT, bootPath));
                    }
                }
                else
                {
                    controllerFactoryflash.SetTitle("Skipping boot...");
                    controllerFactoryflash.SetMessage("Progress 3/8");
                    cAppend("Skipping boot image...");
                }

                controllerFactoryflash.SetTitle("Flashing cache...");
                controllerFactoryflash.SetMessage("Progress 4/8");

                if (flashCache == true)
                {
                    string[] fCache = Directory.GetFiles(rStockFolder, "*cache*", SearchOption.TopDirectoryOnly);
                    if (fCache.Length > 0)
                    {
                        cAppend("Flashing cache...");
                        cachePath = fCache[0].ToString();
                        await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.CACHE, cachePath));
                    }
                }
                else
                {
                    controllerFactoryflash.SetTitle("Skipping cache image...");
                    cAppend("Skipping cache image...");
                }

                if (flashRecovery == true)
                {
                    controllerFactoryflash.SetTitle("Flashing recovery...");
                    controllerFactoryflash.SetMessage("Progress 5/8");

                    cAppend("Flashing recovery...");
                    string[] fRecovery = System.IO.Directory.GetFiles(rStockFolder, "*recovery*", System.IO.SearchOption.TopDirectoryOnly);
                    if (fRecovery.Length > 0)
                    {
                        recoveryPath = fRecovery[0].ToString();
                        await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.RECOVERY, recoveryPath));
                    }
                }
                else
                {
                    controllerFactoryflash.SetTitle("Skipping recovery...");
                    controllerFactoryflash.SetMessage("Progress 5/8");
                    cAppend("Skipping recovery...");
                }

                if (flashSystem == true)
                {
                    string[] fSystem = System.IO.Directory.GetFiles(rStockFolder, "*system*", System.IO.SearchOption.TopDirectoryOnly);
                    if (fSystem.Length > 0)
                    {
                        controllerFactoryflash.SetTitle("Flashing system...");
                        controllerFactoryflash.SetMessage("Progress 6/8");

                        cAppend("Flashing system...");
                        systemPath = fSystem[0].ToString();
                        await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.SYSTEM, systemPath));
                    }
                }
                else
                {
                    controllerFactoryflash.SetTitle("Skipping system image...");
                    cAppend("Skipping system image...");
                }

                if (formatUserdata == true)
                {
                    controllerFactoryflash.SetTitle("Formatting userdata...");
                    controllerFactoryflash.SetMessage("Progress 7/8");

                    cAppend("Formatting userdata...");
                    await Task.Run(() => Fastboot.Instance().Format(IDDevicePartition.USERDATA));
                }
                else
                {
                    controllerFactoryflash.SetTitle("Not formatting userdata...");
                    controllerFactoryflash.SetMessage("Progress 7/8");
                    cAppend("Skipping erasing userdata");
                }

                if (flashVendor == true)
                {
                    string[] fVendor = System.IO.Directory.GetFiles(rStockFolder, "*vendor*", System.IO.SearchOption.TopDirectoryOnly);
                    if (fVendor.Length > 0)
                    {
                        cAppend("Flashing Vendor...");
                        controllerFactoryflash.SetTitle("Flashing vendor...");
                        controllerFactoryflash.SetMessage("Progress 8/8");

                        vendorPath = fVendor[0].ToString();
                        MessageBox.Show(fVendor[0].ToString());
                        await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.VENDOR, vendorPath));
                    }
                }
                else
                {
                    cAppend("Skipping vendor...");
                }

                cAppend("Flashing complete!\n");
                cAppend("Done! Check the output and make sure the following have been flashed sucessfully.\n\nBootloader, Radio, Cache, System, Vendor.\n");

                cAppend("You can reboot once you're happy with the flashing process.\n");

                var result = await this.ShowMessageAsync("Flash Successful!", "Would you like to reboot now?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                if (result == MessageDialogResult.Affirmative)
                {
                    await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.REBOOT));
                }

                var resultCleanup = await this.ShowMessageAsync("Flash Successful!", "Would you like to clean up (delete) the extracted files?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                if (resultCleanup == MessageDialogResult.Affirmative)
                {
                    try
                    {
                        Directory.Delete("./Data/Downloads/Stock/.extracted/", true);
                    }
                    catch (Exception ex)
                    {
                        await this.ShowMessageAsync("Unable to delete extracted files", ex.ToString(), MessageDialogStyle.Affirmative, mySettings);
                    }
                }
            }
            await controllerFactoryflash.CloseAsync();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Stop Connection Monitor before close
            //Be sure to dispose of ADB and Fastboot for the Application to terminate properly
            ADB.ConnectionMonitor.Stop();
            ADB.ConnectionMonitor.Callback -= ConnectionMonitorCallback;
            ADB.Stop();
            Fastboot.Dispose();
            ADB.Dispose();
        }

        private async void adbVersion_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            await this.ShowMessageAsync("ADB Version", ADB.Version, MessageDialogStyle.Affirmative, mySettings);
        }

        private async void adbSideload_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension and title
            dlg.Title = "Open file for Sideloading";
            dlg.DefaultExt = ".zip";
            dlg.Filter = "ZIP Archives (*.zip)|*.zip";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                //MessageBox.Show(filename);
                var controllerSideload = await this.ShowProgressAsync("Sideloading", filename);
                controllerSideload.SetIndeterminate();

                await Task.Run(() => ADB.Instance().Sideload(filename));

                await controllerSideload.CloseAsync();
            }
        }

        private async void adbRebootReboot_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => ADB.Instance().Reboot(IDBoot.REBOOT));
        }

        private async void adbRebootBootloader_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
        }

        private async void adbRebootRecovery_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => ADB.Instance().Reboot(IDBoot.RECOVERY));
        }

        private async void adbAPK_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension and title
            dlg.Multiselect = true;
            dlg.Title = "Open file for Installing APK";
            dlg.DefaultExt = ".apk";
            dlg.Filter = "APK Package (*.apk)|*.apk";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                statusProgress.IsIndeterminate = true;
                // Open document 
                foreach (string apk in dlg.FileNames)
                {
                    cAppend(string.Format("Installing {0}...", Path.GetFileName(apk)));
                    bool apkInstalled = await Task.Run(() => ADB.Instance().Install(apk));
                    if (apkInstalled == true)
                    {
                        cAppend(string.Format("Sucessfully installed {0}.", Path.GetFileName(apk)));
                    }
                    else if (apkInstalled == false)
                    {
                        cAppend(string.Format("Failed to install {0}.", Path.GetFileName(apk)));
                    }
                    else
                    {
                        cAppend("No return for installing APK...\n");
                    }
                }
                cAppend("Finished installing APK(s).\n");
                statusProgress.IsIndeterminate = false;
            }
        }

        private async void fbtRebootReboot_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.REBOOT));
        }

        private async void fbtRebootBootloader_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.BOOTLOADER));
        }

        private void linkDeviceManager_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("devmgmt.msc");
        }

        private void linkExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void howEnableOEMUnlock_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var controllerEnableOEMUnlock = this.ShowMessageAsync("Enabling OEM Unlock", "Here are the steps to enable OEM Unlocking within Android's Developer Options:" +
                "\n\n 1. Goto Settings > About Phone" +
                "\n\n 2. Enable Developer Options: Scroll down to Build Number and tap on it repeatedly until you see a notification informing you that you have enabled Developer Options." +
                "\n\n 3. Go back to the Settings menu > Developer Options." +
                "\n\n 4. Scroll down and check the Enable OEM Unlock box.", MessageDialogStyle.Affirmative, mySettings);
        }

        private async void fastbootFlash_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension and title
            dlg.Title = "Open file for flashing";
            dlg.DefaultExt = ".img";
            dlg.Filter = "Image File (*.img)|*.img";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                tabControl.IsEnabled = false;
                statusProgress.IsIndeterminate = true;
                // Open document 
                string filename = dlg.FileName;
                IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                if (state == IDDeviceState.FASTBOOT)
                {
                    switch (cbPartition.Text)
                    {
                        case "Boot":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.BOOT, filename)));
                            }
                            break;
                        case "Cache":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.CACHE, filename)));
                            }
                            break;
                        case "Data":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.DATA, filename)));
                            }
                            break;
                        case "Userdata":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.USERDATA, filename)));
                            }
                            break;
                        case "Bootloader":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.BOOTLOADER, filename)));
                            }
                            break;
                        case "Radio":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.RADIO, filename)));
                            }
                            break;
                        case "Recovery":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.RECOVERY, filename)));
                            }
                            break;
                        case "System":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.SYSTEM, filename)));
                            }
                            break;
                        case "Vendor":
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.VENDOR, filename)));
                            }
                            break;
                    }
                }
                else
                {
                    cAppend("Your device will need to be in the bootloader.\n");
                }
                tabControl.IsEnabled = true;
                statusProgress.IsIndeterminate = false;
            }
        }

        private async void fastbootErase_Click(object sender, RoutedEventArgs e)
        {
            tabControl.IsEnabled = false;
            statusProgress.IsIndeterminate = true;
            IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
            if (state == IDDeviceState.FASTBOOT)
            {
                switch (cbPartition.Text)
                {
                    case "Boot":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.BOOT)));
                        }
                        break;
                    case "Cache":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.CACHE)));
                        }
                        break;
                    case "Data":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.DATA)));
                        }
                        break;
                    case "Userdata":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.USERDATA)));
                        }
                        break;
                    case "Bootloader":
                        {

                            var dictionary = new ResourceDictionary();
                            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

                            var mySettings = new MetroDialogSettings
                            {
                                AffirmativeButtonText = "Yes",
                                NegativeButtonText = "No",
                                SuppressDefaultResources = true,
                                CustomResourceDictionary = dictionary
                            };

                            var bootloaderEraseResult = await this.ShowMessageAsync("Erase Bootloader", "Are you sure you want to erase the bootloader?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                            if (bootloaderEraseResult == MessageDialogResult.Affirmative)
                            {
                                Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.BOOTLOADER)));
                            }
                        }
                        break;
                    case "Radio":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.RADIO)));
                        }
                        break;
                    case "Recovery":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.RECOVERY)));
                        }
                        break;
                    case "System":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.SYSTEM)));
                        }
                        break;
                    case "Vendor":
                        {
                            Add(await Task.Run(() => Fastboot.Instance().Erase(IDDevicePartition.VENDOR)));
                        }
                        break;
                }
            }
            else
            {
                cAppend("You need to be in the bootloader to erase partitions.");
            }
            tabControl.IsEnabled = true;
            statusProgress.IsIndeterminate = false;
        }

        private async void unlockBootloader_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "Take me out of here!",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            tabControl.IsEnabled = false;
            statusProgress.IsIndeterminate = true;
            IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
            if (state == IDDeviceState.BOOTLOADER)
            {
                var result = await this.ShowMessageAsync("Unlocking the bootloader", "Selecting yes on your phone will factory reset your device.", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                if (result == MessageDialogResult.Affirmative)
                {
                    cAppend("Please check your device to confirm that you want to unlock the bootloader.\n");
                    Add(await Task.Run(() => Fastboot.Instance().Flashing(IDFlashingMode.UNLOCK)));
                }
            }
            else
            {
                cAppend("You need to be in the bootloader to unlock the bootloader.");
            }
            tabControl.IsEnabled = true;
            statusProgress.IsIndeterminate = false;
        }

        private async void flashTWRP_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var md5mismatchSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "Re-download",
                FirstAuxiliaryButtonText = "No, stop process",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var md5matchSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "Later",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var noImageSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            if (twrpBuildList.SelectedIndex == -1)
            {
                cAppend("No recovery image selected.");
                await this.ShowMessageAsync("No TWRP image selected", "Please select an image and try again.", MessageDialogStyle.Affirmative, noImageSettings);
                twrpBuildList.IsDropDownOpen = true;
            }
            if (twrpBuildList.SelectedIndex == 0)
            {
                // Create OpenFileDialog 
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                // Set filter for file extension and default file extension and title
                dlg.Title = "Open file for Custom Recovery";
                dlg.DefaultExt = ".img";
                dlg.Filter = "Custom Recovery (*.img)|*.img";

                // Display OpenFileDialog by calling ShowDialog method 
                Nullable<bool> result = dlg.ShowDialog();

                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;
                    cAppend(string.Format("Flashing Recovery Image{0}\n", Path.GetFileName(filename)));
                    statusProgress.IsIndeterminate = true;
                    tabControl.IsEnabled = false;

                    var controllerFlashRecovery = await this.ShowProgressAsync("Flashing Recovery...", filename);
                    controllerFlashRecovery.SetIndeterminate();

                    await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.RECOVERY, filename));

                    await controllerFlashRecovery.CloseAsync();
                    tabControl.IsEnabled = false;
                    statusProgress.IsIndeterminate = false;
                }
            }
            else if (twrpBuildList.SelectedIndex != -1)
            {
                IniFileName iniStock = new IniFileName("./Data/.cached/TWRPBuildList.ini");
                string[] sectionValues = iniStock.GetEntryNames(fullDeviceName);
                string entryValue = iniStock.GetEntryValue(fullDeviceName, twrpBuildList.SelectedValue.ToString()).ToString();

                twrpListStrLineElements = entryValue.Split(';').ToList();

                //Set required variables from ini
                twrpVersion = twrpListStrLineElements[0];
                pTWRPMD5 = twrpListStrLineElements[1];
                pTWRPFileName = string.Format("twrp-{0}-{1}.img", twrpVersion, codeDeviceName);

                if (_suClient != null && _suClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_twrpClient != null && _twrpClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_stockClient != null && _stockClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_driverClient != null && _driverClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_otaClient != null && _otaClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_modBootClient != null && _modBootClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else
                {
                    //Start checking for existing tgz
                    if (!File.Exists(Path.Combine("./Data/Downloads/TWRP", pTWRPFileName)))
                    {
                        //Start downloading selected tgz
                        cAppend(string.Format("Downloading {0}. Please wait... You can continue to use the program while I download it in the background.", pTWRPFileName));
                        cAppend("You can cancel the download by double clicking the progress bar at any time!\n");
                        //Declare new webclient
                        _twrpClient = new WebClient();

                        //Proxy for WebClient
                        IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                        if (defaultProxy != null)
                        {
                            defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                            _twrpClient.Proxy = defaultProxy;
                        }

                        //Subscribe to download and completed event handlers
                        _twrpClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        _twrpClient.DownloadFileCompleted += new AsyncCompletedEventHandler(twrpClient_DownloadFileCompleted);
                        // Starts the download
                        _twrpClient.DownloadFileAsync(new Uri(string.Format("https://s.basketbuild.com/dl/devs?dl=squabbi/{0}/toolkit/{1}", codeDeviceName, pTWRPFileName)), Path.Combine("./Data/Downloads/TWRP", pTWRPFileName));
                    }
                    else
                    {
                        statusProgress.IsIndeterminate = true;
                        bool lMd5Result = await Task.Run(() => checkTWRPMd5());
                        if (lMd5Result == true)
                        {
                            var matchResult = await this.ShowMessageAsync("MD5 Match", string.Format("The MD5s have returned as a match. Are you ready to flash {0}?", pTWRPFileName), MessageDialogStyle.AffirmativeAndNegative, md5matchSettings);
                            if (matchResult == MessageDialogResult.Affirmative)
                            {
                                IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                                if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                                {
                                    var controllerRebootingRecoveryFlash = await this.ShowProgressAsync("Waiting for device...", "");
                                    controllerRebootingRecoveryFlash.SetIndeterminate();
                                    cAppend("Waiting for device...");
                                    await Task.Run(() => ADB.WaitForDevice());
                                    cAppend("Rebooting to the bootloader.");
                                    await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                                    await controllerRebootingRecoveryFlash.CloseAsync();
                                    flashTWRP();
                                }
                                else if (state == IDDeviceState.FASTBOOT)
                                {
                                    flashTWRP();
                                }
                                else
                                {
                                    cAppend("Your device is in the wrong state. Please put your device in the bootloader.\n");
                                }
                            }
                            else
                            {
                                cAppend("No devices were detected...\n");
                            }
                        }
                        else
                        {
                            var result = await this.ShowMessageAsync("MD5 Check Failed", "The MD5s are not the same! This means the file is corrupt and probably wasn't downloaded properly. Would you like to re-download (recommended)?",
                                MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, md5mismatchSettings);
                            if (result == MessageDialogResult.Negative)
                            {
                                File.Delete(Path.Combine("./Data/Downloads/TWRP", pTWRPFileName));
                                flashTWRP_Click(new object(), new RoutedEventArgs());
                            }
                            else if (result == MessageDialogResult.Affirmative)
                            {
                                IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                                if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                                {
                                    var controllerRebootingRecoveryFlash = await this.ShowProgressAsync("Waiting for device...", "");
                                    controllerRebootingRecoveryFlash.SetIndeterminate();
                                    cAppend("Waiting for device...");
                                    await Task.Run(() => ADB.WaitForDevice());
                                    cAppend("Rebooting to the bootloader.");
                                    await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                                    await controllerRebootingRecoveryFlash.CloseAsync();
                                    flashTWRP();
                                }
                                else if (state == IDDeviceState.FASTBOOT)
                                {
                                    flashTWRP();
                                }
                                else
                                {
                                    cAppend("Your device is in the wrong state. Please put your device in the bootloader.\n");
                                }
                            }
                            else
                            {
                                cAppend("Flashing TWRP cancelled. MD5 Failed.");
                            }
                        }
                        statusProgress.IsIndeterminate = false;
                    }
                }
            }
        }

        private async void twrpClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var md5mismatchSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "Re-download",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var md5matchSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "Later",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            //Check if it was cancled
            if (e.Cancelled)
            {
                //Reset retrys
                retryLvl = 0;
                _twrpClient.Dispose();
                //Set progress to 0
                statusProgress.Value = 0;
                cAppend("Download cancelled!");
                cAppend("Cleaning up...\n");
                //Delete attemped file download
                if (File.Exists(Path.Combine("./Data/Downloads/TWRP", pTWRPFileName)))
                {
                    cAppend(string.Format("Deleting {0}...", pTWRPFileName));
                    cAppend("Ready for next download.");
                    File.Delete(Path.Combine("./Data/Downloads/TWRP", pTWRPFileName));
                }
            }
            //In case of error
            else if (e.Error != null)
            {
                // We have an error! Retry a few times, then abort.
                retryLvl++;
                if (retryLvl < 3)
                {
                    cAppend(string.Format("Failed.. {0} retry...", retryLvl.ToString()));
                    cAppend(string.Format("Error: {0}\n", e.Error.Message));
                    flashTWRP_Click(new object(), new RoutedEventArgs());
                }
                else
                {
                    retryLvl = 0;
                    cAppend(string.Format("Failed after 3 retries... Error: {0}", e.Error.Message));
                }
            }
            //No error
            else if (!e.Cancelled)
            {
                //Reset retrys
                retryLvl = 0;
                //Clean up webclient
                _twrpClient.Dispose();
                statusProgress.Value = 0;
                //start flashing
                statusProgress.IsIndeterminate = true;
                bool lResult = await Task.Run(() => checkTWRPMd5());
                if (lResult == true)
                {
                    var matchResult = await this.ShowMessageAsync("MD5 Match", string.Format("The MD5s have returned as a match. Are you ready to flash {0}?", pTWRPFileName), MessageDialogStyle.AffirmativeAndNegative, md5matchSettings);
                    if (matchResult == MessageDialogResult.Affirmative)
                    {
                        IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                        if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                        {
                            var controllerRebootingRecoveryFlash = await this.ShowProgressAsync("Waiting for device...", "");
                            controllerRebootingRecoveryFlash.SetIndeterminate();
                            cAppend("Waiting for device...");
                            await Task.Run(() => ADB.WaitForDevice());
                            cAppend("Rebooting to the bootloader.");
                            await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                            await controllerRebootingRecoveryFlash.CloseAsync();
                            flashTWRP();
                        }
                        else if (state == IDDeviceState.FASTBOOT)
                        {
                            flashTWRP();
                        }
                        else
                        {
                            cAppend("Your device is in the wrong state. Please put your device in the bootloader.\n");
                        }
                    }
                }
                else
                {
                    var result = await this.ShowMessageAsync("MD5 Check Failed", "The MD5s are not the same! This means the file is corrupt and probably wasn't downloaded properly. Would you like to re-download (recommended)?",
                                MessageDialogStyle.AffirmativeAndNegative, md5mismatchSettings);
                    if (result == MessageDialogResult.Negative)
                    {
                        File.Delete(Path.Combine("./Data/Downloads/TWRP", pTWRPFileName));
                        flashTWRP_Click(new object(), new RoutedEventArgs());
                    }
                    else
                    {
                        IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                        if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                        {
                            var controllerRebootingRecoveryFlash = await this.ShowProgressAsync("Waiting for device...", "");
                            controllerRebootingRecoveryFlash.SetIndeterminate();
                            cAppend("Waiting for device...");
                            await Task.Run(() => ADB.WaitForDevice());
                            cAppend("Rebooting to the bootloader.");
                            await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                            await controllerRebootingRecoveryFlash.CloseAsync();
                            flashTWRP();
                        }
                        else if (state == IDDeviceState.FASTBOOT)
                        {
                            flashTWRP();
                        }
                        else
                        {
                            cAppend("Your device is in the wrong state. Please put your device in the bootloader.\n");
                        }
                    }
                }
                statusProgress.IsIndeterminate = false;
            }
        }

        private bool checkTWRPMd5()
        {
            //Check MD5
            cAppend("Starting MD5 check...\n");
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(Path.Combine("./Data/Downloads/TWRP", pTWRPFileName)))
                {
                    string compHashDownloaded = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    cAppend(string.Format("Downloaded MD5: {0}", compHashDownloaded));
                    cAppend(string.Format("TWRP's MD5: {0}\n", pTWRPMD5));
                    if (compHashDownloaded == pTWRPMD5)
                    {
                        //MD5 are the same
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        private bool checkOTAMD5()
        {
            //Check MD5
            cAppend("Starting MD5 check for OTA...\n");
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(Path.Combine("./Data/Downloads/OTA", pOTAFileName)))
                {
                    string compHashDownloaded = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    cAppend(string.Format("Downloaded MD5: {0}", compHashDownloaded));
                    cAppend(string.Format("OTA's MD5: {0}\n", otaMD5));
                    if (compHashDownloaded == otaMD5)
                    {
                        //MD5 are the same
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        private bool checkSHA256(string type, string filename)
        {
            //Check SHA256 Checksum
            cAppend("Starting SHA256 check...\n");
            using (var sha = SHA256.Create())
            {
                using (var stream = File.OpenRead(Path.Combine(string.Format("./Data/Downloads/{0}/", type), filename)))
                {
                    string compHashDownloaded = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLower();
                    cAppend(string.Format("Downloaded SHA256: {0}", compHashDownloaded));
                    cAppend(string.Format("Supposed SHA256: {0}\n", supSHA));
                    if (compHashDownloaded == supSHA)
                    {
                        //SHA Match
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        private async void pushSU_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var noImageSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            if (supersuBuildList.SelectedIndex == -1)
            {
                cAppend("No SuperSU selected...");
                await this.ShowMessageAsync("No SuperSU version selected", "Please select a version of SuperSU and try again.", MessageDialogStyle.Affirmative, noImageSettings);
                supersuBuildList.IsDropDownOpen = true;
            }
            if (supersuBuildList.SelectedIndex == 0)
            {
                // Create OpenFileDialog 
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                // Set filter for file extension and default file extension and title
                dlg.Title = "Open file for SuperSU";
                dlg.DefaultExt = ".zip";
                dlg.Filter = "SuperSU ZIP (*.zip)|*.zip";

                // Display OpenFileDialog by calling ShowDialog method 
                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;
                    var controllerPushSU = await this.ShowProgressAsync(string.Format("Pushing {0}", Path.GetFileName(filename)), "");
                    controllerPushSU.SetIndeterminate();
                    statusProgress.IsIndeterminate = true;
                    await Task.Run(() => ADB.Instance().PushPullUTF8.Push(filename, "/sdcard/"));
                    await controllerPushSU.CloseAsync();
                    statusProgress.IsIndeterminate = false;
                }
            }
            else if (supersuBuildList.SelectedIndex != -1)
            {
                IniFileName iniSu = new IniFileName("./Data/.cached/SuBuildList.ini");
                string[] suSecValue = iniSu.GetSectionNames();
                string entryValue = iniSu.GetEntryValue(suSecValue[0], supersuBuildList.SelectedValue.ToString()).ToString();

                suListStrLineElements = entryValue.Split(';').ToList();

                //Set required variables from ini
                suVersion = suListStrLineElements[0];
                suType = suListStrLineElements[1];

                switch (justPushSU.IsChecked)
                {
                    case true:
                        {
                            suManInstall = true;
                        }
                        break;
                    case false:
                        {
                            suManInstall = false;
                        }
                        break;
                }

                pSuFileName = string.Format("{1}-SuperSU-v{0}.zip", suVersion, suType);

                if (_suClient != null && _suClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_twrpClient != null && _twrpClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_stockClient != null && _stockClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_driverClient != null && _driverClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_otaClient != null && _otaClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_modBootClient != null && _modBootClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else
                {
                    //Start checking for existing tgz
                    if (!File.Exists(Path.Combine("./Data/Downloads/SU", pSuFileName)))
                    {
                        //Start downloading selected tgz
                        cAppend(string.Format("Downloading {0}. Please wait... You can continue to use the program while I download it in the background.", pSuFileName));
                        cAppend("You can cancel the download by double clicking the progress bar at any time!\n");
                        //Declare new webclient
                        _suClient = new WebClient();

                        //Proxy for WebClient
                        IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                        if (defaultProxy != null)
                        {
                            defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                            _suClient.Proxy = defaultProxy;
                        }

                        //Subscribe to download and completed event handlers
                        _suClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        _suClient.DownloadFileCompleted += new AsyncCompletedEventHandler(suClient_DownloadFileCompleted);
                        // Starts the download
                        _suClient.DownloadFileAsync(new Uri(string.Format("https://s.basketbuild.com/dl/devs?dl=squabbi/superSU/{0}", pSuFileName)), Path.Combine("./Data/Downloads/SU", pSuFileName));
                    }
                    else
                    {
                        flashSuperSU();
                    }
                }
            }
        }

        private void suClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //Check if it was cancled
            if (e.Cancelled)
            {
                //Clean up
                _suClient.Dispose();
                //Set retries back to 0
                retryLvl = 0;
                //Set progress to 0
                statusProgress.Value = 0;
                cAppend("Download cancelled!");
                cAppend("Cleaning up...\n");
                //Delete attemped file download
                if (File.Exists(Path.Combine("./Data/Downloads/SU", pSuFileName)))
                {
                    cAppend(string.Format("Deleting {0}...", pSuFileName));
                    cAppend("Ready for next download...");
                    File.Delete(Path.Combine("./Data/Downloads/SU", pSuFileName));
                }
            }
            //In case of error
            else if (e.Error != null)
            {
                // We have an error! Retry a few times, then abort.
                retryLvl++;
                if (retryLvl < 3)
                {
                    //Delete attemped file download
                    if (File.Exists(Path.Combine("./Data/Downloads/SU", pSuFileName)))
                    {
                        cAppend(string.Format("Deleting {0}...", pSuFileName));
                        cAppend("Ready for next download...");
                        File.Delete(Path.Combine("./Data/Downloads/SU", pSuFileName));
                    }
                    cAppend(string.Format("Failed.. {0} retry...", retryLvl.ToString()));
                    cAppend(string.Format("Error: {0}\n", e.Error.Message));
                    pushSU_Click(new object(), new RoutedEventArgs());
                }
                else
                {
                    retryLvl = 0;
                    cAppend(string.Format("Failed after 3 retries... Error: {0}", e.Error.Message));
                }
            }
            //No error
            else if (!e.Cancelled)
            {
                //Clean Up
                _suClient.Dispose();
                //Reset retrys
                retryLvl = 0;
                //Start flashing
                flashSuperSU();
            }
        }

        private void menuCheckforUpdates_Click(object sender, RoutedEventArgs e)
        {
            Updater upd = new Updater();
            upd.UpdateUrl = string.Format("https://s.basketbuild.com/dl/devs?dl=squabbi/{0}/toolkit/UpdateInfo.dat", codeDeviceName);
            upd.CheckForUpdates();
        }

        private async void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            await this.ShowMessageAsync("About", "- Nexus 6P Toolkit\n\n" +
                "I made this as a little handy tool for your lovely, shiny 6P. This offers " +
                "several new and exciting features such as: flashing stock with the option to keep " +
                "you existing recovery and/or user data, flashing images to various paritions and also " +
                "quick and easy vendor image flashing. An ADB based interface for browsing and managing " +
                "your device's contents is pretty neat.\n\n" +
                "This is only possible with XDA Seinor Memeber @k1ll3r8e with his AndroidCtrl and AndroidCtrlUI\n" +
                "framework. Special thanks to him.\n\n" +
                "Thanks to http://icons8.com/ for the icons in the application.\n\n" +
                "The rest was me and of course the community for their feedback and inspriation.\n\n" +
                "Thank you for using my toolkit! :)\n" +
                "~Squabbi @ XDA", MessageDialogStyle.Affirmative, mySettings);
        }

        private async void relockBootloader_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            tabControl.IsEnabled = false;
            statusProgress.IsIndeterminate = true;
            cAppend("Confirming to lock the bootloader.");
            var resultLockBL = await this.ShowMessageAsync("Lock Bootloader", "Are you sure you want to relock the bootloader? You must be 100% stock! NO MODIFICATIONS!");
            if (resultLockBL == MessageDialogResult.Affirmative)
            {
                cAppend("Confirmed...\n\nChecking device state...");
                IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                if (state == IDDeviceState.FASTBOOT)
                {
                    cAppend("Locking bootloader.");
                    await Task.Run(() => Fastboot.Instance().Flashing(IDFlashingMode.LOCK));
                    cAppend("Done");
                }
                cAppend("No device found in fastboot.\n");
            }
            cAppend("Canceled.\n");
            statusProgress.IsIndeterminate = false;
            tabControl.IsEnabled = true;
        }

        private async void dlStock_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var noImageSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            if (stockBuildList.SelectedIndex == -1)
            {
                cAppend("No factory image was selected.");
                await this.ShowMessageAsync("No factory image selected", "Please select a factory image and try again.", MessageDialogStyle.Affirmative, noImageSettings);
                stockBuildList.IsDropDownOpen = true;
            }
            else if (stockBuildList.SelectedIndex == 0)
            {
                // Create OpenFileDialog 
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                // Set filter for file extension and default file extension and title
                dlg.Title = "Select factory image";
                dlg.DefaultExt = ".zip";
                dlg.Filter = "Factory Image|*.zip;*.tgz";

                // Display OpenFileDialog by calling ShowDialog method 
                Nullable<bool> result = dlg.ShowDialog();

                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    pStockFileName = dlg.FileName;
                    //split full stock file name
                    string[] pSFNArray = pStockFileName.Split('-');
                    string[] pSFNExtArray = pStockFileName.Split('.');

                    stockVersion = pSFNArray[1];
                    stockExtension = pSFNExtArray[1];

                    MessageBox.Show(stockVersion + stockExtension);

                    cAppend("Applying options.\n");

                    if (rbEzFactoryMode.IsChecked == true)
                    {
                        /////Set easy mode
                        factoryEzMode = true;
                        //Set ez mode options
                        flashBootloader = true;
                        flashRadio = true;
                        formatUserdata = true;
                    }
                    else
                    {
                        //Set easy mode - false
                        factoryEzMode = false;
                        //Else option remains false
                        if (cbFlashBootloader.IsChecked == true)
                            flashBootloader = true;
                        if (cbFlashRadio.IsChecked == true)
                            flashRadio = true;
                        if (cbFlashBoot.IsChecked == true)
                            flashBoot = true;
                        if (cbFlashCache.IsChecked == true)
                            flashCache = true;
                        if (cbFlashRecovery.IsChecked == true)
                            flashRecovery = true;
                        if (cbFlashSystem.IsChecked == true)
                            flashSystem = true;
                        if (cbFlashVendor.IsChecked == true)
                            flashVendor = true;
                        if (cbFormatUserdata.IsChecked == true)
                            formatUserdata = true;
                    }

                    if (factoryEzMode == true)
                    {
                        var startflashResult = await this.ShowMessageAsync("Using Easy Mode Settings",
                                "Are you ready to flash the factory image using those settings?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                        if (startflashResult == MessageDialogResult.Negative)
                        {
                            cAppend("Canceled Factory Image Flash.\n");
                            return;
                        }
                    }
                    else if (factoryEzMode == false)
                    {
                        var startflashResult = await this.ShowMessageAsync("Using Advanced Mode Settings",
                                "Are you ready to flash the factory image using those settings?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                        if (startflashResult == MessageDialogResult.Negative)
                        {
                            cAppend("Canceled Factory Image Flash.\n");
                            return;
                        }
                    }

                    flashFactoryImage(true);
                }
            }
            else //if (stockBuildList.SelectedIndex != -1)
            {
                IniFileName iniStock = new IniFileName("./Data/.cached/StockBuildList.ini");
                string entryValue = iniStock.GetEntryValue(fullDeviceName, stockBuildList.SelectedValue.ToString()).ToString();

                stockListStrLineElements = entryValue.Split(';').ToList();

                //Set required variables from ini
                //Example link: https://dl.google.com/dl/android/aosp/angler-nrd90u-factory-7c9b6a2b.zip

                stockVersion = stockListStrLineElements[2];
                stockEdition = stockListStrLineElements[6];
                stockUniqueID = stockListStrLineElements[3];
                supSHA = stockListStrLineElements[4];
                isStockDev = stockListStrLineElements[5];

                pStockFileName = string.Format("{0}-{1}-{2}-{3}.zip", codeDeviceName, stockVersion, stockEdition, stockUniqueID/*, stockExtension*/);

                if (_stockClient != null && _stockClient.IsBusy == true)
                {
                    cAppend("Download client busy.");
                    await this.ShowMessageAsync("Download Busy", "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double clicking on the progress bar.");
                }
                else if (_twrpClient != null && _twrpClient.IsBusy == true)
                {
                    cAppend("Download client busy.");
                    await this.ShowMessageAsync("Download Busy", "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double clicking on the progress bar.");
                }
                else if (_stockClient != null && _stockClient.IsBusy == true)
                {
                    cAppend("Download client busy.");
                    await this.ShowMessageAsync("Download Busy", "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double clicking on the progress bar.");
                }
                else if (_driverClient != null && _driverClient.IsBusy == true)
                {
                    cAppend("Download client busy.");
                    await this.ShowMessageAsync("Download Busy", "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double clicking on the progress bar.");
                }
                else if (_otaClient != null && _otaClient.IsBusy == true)
                {
                    cAppend("Download client busy.");
                    await this.ShowMessageAsync("Download Busy", "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double clicking on the progress bar.");
                }
                else if (_modBootClient != null && _modBootClient.IsBusy == true)
                {
                    cAppend("Download client busy.");
                    await this.ShowMessageAsync("Download Busy", "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double clicking on the progress bar.");
                }
                else
                {
                    cAppend("Applying options.\n");

                    if (rbEzFactoryMode.IsChecked == true)
                    {
                        /////Set easy mode
                        factoryEzMode = true;
                        //Set ez mode options
                        flashBootloader = true;
                        flashRadio = true;
                        formatUserdata = true;
                    }
                    else
                    {
                        //Set easy mode - false
                        factoryEzMode = false;
                        //Else option remains false
                        if (cbFlashBootloader.IsChecked == true)
                            flashBootloader = true;
                        if (cbFlashRadio.IsChecked == true)
                            flashRadio = true;
                        if (cbFlashBoot.IsChecked == true)
                            flashBoot = true;
                        if (cbFlashCache.IsChecked == true)
                            flashCache = true;
                        if (cbFlashRecovery.IsChecked == true)
                            flashRecovery = true;
                        if (cbFlashSystem.IsChecked == true)
                            flashSystem = true;
                        if (cbFlashVendor.IsChecked == true)
                            flashVendor = true;
                        if (cbFormatUserdata.IsChecked == true)
                            formatUserdata = true;
                    }

                    if (factoryEzMode == true)
                    {
                        var startflashResult = await this.ShowMessageAsync("Using Easy Mode Settings",
                                "Are you ready to flash the factory image using those settings?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                        if (startflashResult == MessageDialogResult.Negative)
                        {
                            cAppend("Canceled Factory Image Flash.\n");
                            return;
                        }
                    }
                    else if (factoryEzMode == false)
                    {
                        var startflashResult = await this.ShowMessageAsync("Using Advanced Mode Settings",
                                "Are you ready to flash the factory image using those settings?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                        if (startflashResult == MessageDialogResult.Negative)
                        {
                            cAppend("Canceled Factory Image Flash.\n");
                            return;
                        }
                    }


                    //Start checking for existing tgz (if not exist)
                    if (!File.Exists(Path.Combine("./Data/Downloads/Stock", pStockFileName)))
                    {
                        //Start downloading selected tgz
                        cAppend("Start factory image download.\n");
                        cAppend(string.Format("Downloading {0}. Please wait... You can continue to use the program while I download it in the background.", pStockFileName));
                        cAppend("You can cancel the download by double clicking the progress bar at any time!\n");

                        // Starts the download

                        //Declare new webclient
                        _stockClient = new WebClient();
                        //Ask for proxy
                        IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                        if (defaultProxy != null)
                        {
                            defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                            _stockClient.Proxy = defaultProxy;
                        }
                        //Subscribe to download and completed event handlers
                        _stockClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        _stockClient.DownloadFileCompleted += new AsyncCompletedEventHandler(stockClient_DownloadFileCompleted);
                        if (isStockDev == "1")
                        {
                            _stockClient.DownloadFileAsync(new Uri(string.Format("http://storage.googleapis.com/androiddevelopers/shareables/preview/{0}", pStockFileName)), Path.Combine("./Data/Downloads/Stock", pStockFileName));
                        }
                        else if (isStockDev == "0")
                        {
                            _stockClient.DownloadFileAsync(new Uri(string.Format("https://dl.google.com/dl/android/aosp/{0}", pStockFileName)), Path.Combine("./Data/Downloads/Stock", pStockFileName));
                        }
                    }
                    else
                    {
                        dlStock.IsEnabled = false;
                        bool lResult = await Task.Run(() => checkSHA256("stock", pStockFileName));
                        if (lResult == true)
                        {
                            var startflashResult = await this.ShowMessageAsync(@"Ready to flash",
                                "Are you ready to flash the factory image? If your device is in a bootloop or softbrick" +
                                    ", boot your device into the bootloader and then start.", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                            if (startflashResult == MessageDialogResult.Affirmative)
                            {
                                //Start flashing process
                                IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                                if (state == IDDeviceState.FASTBOOT)
                                {
                                    cAppend("Device detected in fastboot.\n");
                                    App.Current.Dispatcher.Invoke((Action)delegate { tabControl.IsEnabled = false; });
                                    cAppend("Starting to flash factory image...");
                                    flashFactoryImage(false);
                                    App.Current.Dispatcher.Invoke((Action)delegate { tabControl.IsEnabled = true; });
                                }
                                else if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                                {
                                    cAppend("Device deteced in Android.\n");
                                    //Reboot into bootloader...
                                    tabControl.IsEnabled = false;
                                    statusProgress.IsIndeterminate = true;
                                    cAppend("Rebooting to Bootloader to start flashing process...");
                                    await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                                    cAppend("Starting to flash factory image...");
                                    flashFactoryImage(false);
                                    statusProgress.IsIndeterminate = false;
                                    tabControl.IsEnabled = true;
                                    cAppend("Finished flashing factory image...");
                                }
                                else
                                {
                                    cAppend("Device was not detected.");
                                    var resultNoDetect = await this.ShowMessageAsync("No device detected", "The connection monitor has not detected a device. You will need to manually boot into the bootloader. Once booted into the bootloader, press Yes to continue or no to flash later." +
                                        "\n\nAlso try selecting your device in the connection monitor again and try again.",
                                        MessageDialogStyle.AffirmativeAndNegative, mySettings);
                                    if (resultNoDetect == MessageDialogResult.Affirmative)
                                    {
                                        tabControl.IsEnabled = false;
                                        cAppend("Starting to flash factory image...");
                                        flashFactoryImage(false);
                                        cAppend("Finished flashing factory image...");
                                        tabControl.IsEnabled = true;
                                    }
                                }
                            }
                        }
                        dlStock.IsEnabled = true;
                    }
                }
            }
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                statusProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
            });
        }

        private async void stockClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            //Check if it was cancled
            if (e.Cancelled)
            {
                //Clean up
                _stockClient.Dispose();
                //Set progress to 0
                statusProgress.Value = 0;
                retryLvl = 0;
                cAppend("Download cancelled!");
                cAppend("Cleaning up...\n");
                //Delete attemped file download
                if (File.Exists(Path.Combine("./Data/Downloads/Stock", pStockFileName)))
                {
                    cAppend(string.Format("Deleting {0}...", pStockFileName));
                    cAppend("Ready for next download...");
                    File.Delete(Path.Combine("./Data/Downloads/Stock", pStockFileName));
                }
            }
            //In case of error
            else if (e.Error != null)
            {
                // We have an error! Retry a few times, then abort.
                retryLvl++;
                if (retryLvl < 3)
                {
                    cAppend(string.Format("Failed.. {0} retry...", retryLvl.ToString()));
                    cAppend(string.Format("Error: {0}\n", e.Error.Message));
                    dlStock_Click(new object(), new RoutedEventArgs());
                }
                else
                {
                    retryLvl = 0;
                    MessageBox.Show(string.Format("Failed after 3 retries... Error: {0}", e.Error.Message));
                }
            }
            //No error
            else if (!e.Cancelled)
            {
                //Clean Up
                _stockClient.Dispose();
                //Progress 0
                statusProgress.Value = 0;
                retryLvl = 0;
                //Start flashing
                bool lResult = await Task.Run(() => checkSHA256("stock", pStockFileName));
                if (lResult == true)
                {
                    var startflashResult = await this.ShowMessageAsync(@"Ready to flash",
                                @"Are you ready to flash the factory image? If your device is in a bootloop or softbrick
                                    , boot your device into the bootloader and then start.", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (startflashResult == MessageDialogResult.Affirmative)
                    {
                        //Start flashing process
                        IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                        if (state == IDDeviceState.FASTBOOT)
                        {
                            cAppend("Device detected in fastboot.\n");
                            App.Current.Dispatcher.Invoke((Action)delegate { tabControl.IsEnabled = false; });
                            cAppend("Starting to flash factory image...");
                            flashFactoryImage(false);
                            App.Current.Dispatcher.Invoke((Action)delegate { tabControl.IsEnabled = true; });
                        }
                        else if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                        {
                            cAppend("Device deteced in Android.\n");
                            //Reboot into bootloader...
                            tabControl.IsEnabled = false;
                            statusProgress.IsIndeterminate = true;
                            cAppend("Rebooting to Bootloader to start flashing process...");
                            await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                            cAppend("Starting to flash factory image...");
                            flashFactoryImage(false);
                            statusProgress.IsIndeterminate = false;
                            tabControl.IsEnabled = true;
                            cAppend("Finished flashing factory image...");
                        }
                        else
                        {
                            cAppend("Device was not detected.");
                            var resultNoDetect = await this.ShowMessageAsync("No device detected", "The connection monitor has not detected a device. You will need to manually boot into the bootloader. Once booted into the bootloader, press Yes to continue or no to flash later.",
                                MessageDialogStyle.AffirmativeAndNegative, mySettings);
                            if (resultNoDetect == MessageDialogResult.Affirmative)
                            {
                                tabControl.IsEnabled = false;
                                cAppend("Starting to flash factory image...");
                                flashFactoryImage(false);
                                cAppend("Finished flashing factory image...");
                                tabControl.IsEnabled = true;
                            }
                        }
                    }
                }
                dlStock.IsEnabled = true;
            }
        }

        private void extractTGZ(string stockFile, string extractDir)
        {
            Stream inStream = File.OpenRead(stockFile);
            Stream gzipStream = new GZipInputStream(inStream);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
            tarArchive.ExtractContents(extractDir);
            tarArchive.Close();

            gzipStream.Close();
            inStream.Close();
        }

        public void FastZipUnpack(string zipFileName, string targetDir)
        {

            FastZip fastZip = new FastZip();
            string fileFilter = null;

            // Will always overwrite if target filenames already exist
            fastZip.ExtractZip(zipFileName, targetDir, fileFilter);
        }

        private async void installDrivers_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            //Download driver
            if (_suClient != null && _suClient.IsBusy == true)
            {
                await this.ShowMessageAsync("Download in progress...",
                    "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                    MessageDialogStyle.Affirmative, mySettings);
            }
            else if (_twrpClient != null && _twrpClient.IsBusy == true)
            {
                await this.ShowMessageAsync("Download in progress...",
                    "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                    MessageDialogStyle.Affirmative, mySettings);
            }
            else if (_stockClient != null && _stockClient.IsBusy == true)
            {
                await this.ShowMessageAsync("Download in progress...",
                    "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                    MessageDialogStyle.Affirmative, mySettings);
            }
            else if (_driverClient != null && _driverClient.IsBusy == true)
            {
                await this.ShowMessageAsync("Download in progress...",
                    "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                    MessageDialogStyle.Affirmative, mySettings);
            }
            else if (_otaClient != null && _otaClient.IsBusy == true)
            {
                await this.ShowMessageAsync("Download in progress...",
                    "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                    MessageDialogStyle.Affirmative, mySettings);
            }
            else if (_modBootClient != null && _modBootClient.IsBusy == true)
            {
                await this.ShowMessageAsync("Download in progress...",
                    "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                    MessageDialogStyle.Affirmative, mySettings);
            }
            else
            {
                //Start checking for existing tgz
                if (!File.Exists("./Data/Downloads/google_usb_driver.zip"))
                {
                    //Start downloading selected tgz
                    cAppend("Downloading google_usb_driver.zip. Please wait... You can continue to use the program while I download it in the background.");
                    cAppend("You can cancel the download by double clicking the progress bar at any time!\n");
                    //Declare new webclient
                    _driverClient = new WebClient();
                    //Proxy for webClient
                    IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                    if (defaultProxy != null)
                    {
                        defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                        _driverClient.Proxy = defaultProxy;
                    }
                    //Subscribe to download and completed event handlers
                    _driverClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    _driverClient.DownloadFileCompleted += new AsyncCompletedEventHandler(driverClient_DownloadFileCompleted);
                    // Starts the download
                    _driverClient.DownloadFileAsync(new Uri(string.Format("https://dl-ssl.google.com//android/repository/latest_usb_driver_windows.zip")), "./Data/Downloads/google_usb_driver.zip");
                }
                else
                {
                    if (!Directory.Exists("./Data/Downloads/usb_driver/"))
                    {
                        //Extract downloaded file
                        await Task.Run(() => FastZipUnpack("./Data/Downloads/google_usb_driver.zip", "./Data/Downloads/"));
                        int result = await Task.Run(() => DriverPackagePreinstall("./Data/Downloads/usb_driver/android_winusb.inf", 1));
                        if (result != 0 && result != 5)
                        {
                            MessageBox.Show("Driver installation failed.", "Install Failed!", MessageBoxButton.OK, MessageBoxImage.Error);
                            cAppend(string.Format("Driver install failed! Error code: {0}", result.ToString()));
                        }
                        else if (result == 5)
                        {
                            MessageBox.Show("Driver has already been installed!", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                            cAppend(string.Format("Driver install failed! Error code: {0} (Driver already exists...)", result.ToString()));
                        }
                        else if (result == 0)
                            MessageBox.Show("Driver installation completed! Please reboot into the bootloader to test.", "Install Success!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        int result = DriverPackagePreinstall("./Data/Downloads/usb_driver/android_winusb.inf", 0);
                        if (result != 0 && result != 5)
                        {
                            MessageBox.Show("Driver installation failed.", "Install Failed!", MessageBoxButton.OK, MessageBoxImage.Error);
                            cAppend(string.Format("Driver install failed! Error code: {0}", result.ToString()));
                        }
                        else if (result == 5)
                        {
                            MessageBox.Show("Driver has already been installed!", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                            cAppend(string.Format("Driver install failed! Error code: {0} (Driver already exists...)", result.ToString()));
                        }
                        else if (result == 0)
                            MessageBox.Show("Driver installation completed! Please reboot into the bootloader to test.", "Install Success!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private async void driverClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //Check if it was cancled
            if (e.Cancelled)
            {
                //Clean up
                _driverClient.Dispose();
                //Set progress to 0
                statusProgress.Value = 0;
                retryLvl = 0;
                cAppend("Download cancelled!");
                cAppend("Cleaning up...\n");
                //Delete attemped file download
                if (File.Exists("./Data/Downloads/google_usb_driver.zip"))
                {
                    cAppend("Deleting google_usb_driver.zip");
                    cAppend("Ready for next download...\n");
                    File.Delete("./Data/Downloads/google_usb_driver.zip");
                }
            }
            //In case of error
            if (e.Error != null)
            {
                // We have an error! Retry a few times, then abort.
                retryLvl++;
                if (retryLvl < 3)
                {
                    cAppend(string.Format("Failed.. {0} retry...", retryLvl.ToString()));
                    cAppend(string.Format("Error: {0}\n", e.Error.Message));
                    installDrivers_Click(new object(), new RoutedEventArgs());
                }
                else
                {
                    retryLvl = 0;
                    MessageBox.Show(string.Format("Failed after 3 retries... Error: {0}", e.Error.Message));
                }
            }
            //No error
            if (!e.Cancelled)
            {
                //Clean Up
                _driverClient.Dispose();
                //Progress 0
                statusProgress.Value = 0;
                retryLvl = 0;
                //Start installing
                //Extract downloaded file
                await Task.Run(() => FastZipUnpack("./Data/Downloads/google_usb_driver.zip", "./Data/Downloads/"));
                int result = await Task.Run(() => DriverPackagePreinstall("./Data/Downloads/usb_driver/android_winusb.inf", 0));
                if (result != 0 && result != 5)
                {
                    MessageBox.Show("Driver installation failed.", "Install Failed!", MessageBoxButton.OK, MessageBoxImage.Error);
                    cAppend(string.Format("Driver install failed! Error code: {0}", result.ToString()));
                }
                else if (result == 5)
                {
                    MessageBox.Show("Driver has already been installed!", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                    cAppend(string.Format("Driver install failed! Error code: {0} (Driver already exists...)", result.ToString()));
                }
                else if (result == 0)
                    MessageBox.Show("Driver installation completed! Please reboot into the bootloader to test.", "Install Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void statusProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cAppend(string.Format("{0}% completed...", statusProgress.Value.ToString()));
        }

        private async void statusProgress_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            if (_stockClient != null)
            {
                if (_stockClient.IsBusy == true)
                {
                    var result = await this.ShowMessageAsync("Cancel Pending Download?", "Are you sure you want to cancel the current download?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _stockClient.CancelAsync();
                        _stockClient.Dispose();
                    }
                }
            }

            if (_twrpClient != null)
            {
                if (_twrpClient.IsBusy == true)
                {
                    var result = await this.ShowMessageAsync("Cancel Pending Download?", "Are you sure you want to cancel the current download?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _twrpClient.CancelAsync();
                        _twrpClient.Dispose();
                    }
                }
            }

            if (_suClient != null)
            {
                if (_suClient.IsBusy == true)
                {
                    var result = await this.ShowMessageAsync("Cancel Pending Download?", "Are you sure you want to cancel the current download?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _suClient.CancelAsync();
                        _suClient.Dispose();
                    }
                }
            }

            if (_otaClient != null)
            {
                if (_otaClient.IsBusy == true)
                {
                    var result = await this.ShowMessageAsync("Cancel Pending Download?", "Are you sure you want to cancel the current download?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _otaClient.CancelAsync();
                        _otaClient.Dispose();
                    }
                }
            }

            if (_driverClient != null)
            {
                if (_driverClient.IsBusy == true)
                {
                    var result = await this.ShowMessageAsync("Cancel Pending Download?", "Are you sure you want to cancel the current download?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _driverClient.CancelAsync();
                        _driverClient.Dispose();
                    }
                }
            }

            if (_modBootClient != null)
            {
                if (_modBootClient.IsBusy == true)
                {
                    var result = await this.ShowMessageAsync("Cancel Pending Download?", "Are you sure you want to cancel the current download?", MessageDialogStyle.AffirmativeAndNegative, mySettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _modBootClient.CancelAsync();
                        _modBootClient.Dispose();
                    }
                }
            }
        }

        private /*async*/ void adbBackup_Click(object sender, RoutedEventArgs e)
        {
            ////Backup All
            //if (backupAPKs.IsChecked == true && backupShared.IsChecked == true && backupSystem.IsChecked == true)
            //{
            //    await Task.Run(() => Add(ADB.Instance().Backup(Path.Combine("./Data/Backups", string.Format("{0}.ab", backupName.Text)), "-all -apk -shared -system")));
            //}
            ////Basic Backup All
            //else if (backupAPKs.IsChecked == false && backupShared.IsChecked == false && backupSystem.IsChecked == false)
            //{
            //    await Task.Run(() => Add(ADB.Instance().Backup(Path.Combine("./Data/Backups", string.Format("{0}.ab", backupName.Text)), "-all")));
            //}
            ////Backup APK Variants
            //else if (backupAPKs.IsChecked == true && backupShared.IsChecked == true && backupSystem.IsChecked == false)
            //{
            //    await Task.Run(() => Add(ADB.Instance().Backup(Path.Combine("./Data/Backups", string.Format("{0}.ab", backupName.Text)), "-all -apk -shared -nosystem")));
            //}
            //else if (backupAPKs.IsChecked == true && backupShared.IsChecked == false && backupSystem.IsChecked == true)
            //{
            //    await Task.Run(() => Add(ADB.Instance().Backup(Path.Combine("./Data/Backups", string.Format("{0}.ab", backupName.Text)), "-all -apk -noshared -system")));
            //}
            ////Backup Shared Variants
            //else if (backupAPKs.IsChecked == false && backupShared.IsChecked == true && backupSystem.IsChecked == true)
            //{
            //    await Task.Run(() => Add(ADB.Instance().Backup(Path.Combine("./Data/Backups", string.Format("{0}.ab", backupName.Text)), "-all -noapk -shared -system")));
            //}
            //else if (backupAPKs.IsChecked == false && backupShared.IsChecked == true && backupSystem.IsChecked == false)
            //{
            //    await Task.Run(() => Add(ADB.Instance().Backup(Path.Combine("./Data/Backups", string.Format("{0}.ab", backupName.Text)), "-all -noapk -shared -nosystem")));
            //}
            ////Backup System Variants
            //else if (backupAPKs.IsChecked == false && backupShared.IsChecked == false && backupSystem.IsChecked == true)
            //{
            //    await Task.Run(() => Add(ADB.Instance().Backup(Path.Combine("./Data/Backups", string.Format("{0}.ab", backupName.Text)), "-all -noapk -noshared -system")));
            //}
        }

        private void saveLog_Click(object sender, RoutedEventArgs e)
        {
            // Text from the rich textbox rtfMain
            string str = console.Document.ToString();
            // Create OpenFileDialog 
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            // Create a new SaveFileDialog object
            try
            {
                // Available file extensions
                dlg.Filter = "All Files (*.*)|*.*";
                // SaveFileDialog title
                dlg.Title = "Save Console Output";
                // Show SaveFileDialog
                if (dlg.ShowDialog() == true)
                {
                    TextRange range;
                    FileStream fStream;
                    range = new TextRange(console.Document.ContentStart, console.Document.ContentEnd);
                    fStream = new FileStream(dlg.FileName, System.IO.FileMode.Create);
                    range.Save(fStream, DataFormats.Text);
                    fStream.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "An error has occured while attempting to save the output...");
            }
        }

        private async void flashOTA_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var flashLaterSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "Later",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var noImageSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            if (otaBuildList.SelectedIndex == -1)
            {
                cAppend("No OTA Selected...");
                await this.ShowMessageAsync("No OTA selected", "Select an OTA from the dropdown menu and try again.", MessageDialogStyle.Affirmative, noImageSettings);
                otaBuildList.IsDropDownOpen = true;
            }
            if (otaBuildList.SelectedIndex == 0)
            {
                // Create OpenFileDialog 
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                // Set filter for file extension and default file extension and title
                dlg.Title = "Open file for OTA";
                dlg.DefaultExt = ".zip";
                dlg.Filter = "Over-the-Air Update (*.zip)|*.zip";

                // Display OpenFileDialog by calling ShowDialog method 
                Nullable<bool> result = dlg.ShowDialog();

                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;
                    //Check selection
                    if (otaManualCopy.IsChecked == true)
                    {
                        cAppend(string.Format("Pushing OTA: {0} to /sdcard/ \n", Path.GetFileName(filename)));
                        statusProgress.IsIndeterminate = true;
                        await Task.Run(() => ADB.Instance().PushPullUTF8.Push(filename, "/sdcard/"));
                        cAppend("Rebooting device into recovery...");
                        await Task.Run(() => ADB.Instance().Reboot(IDBoot.RECOVERY));
                        statusProgress.IsIndeterminate = false;
                    }
                    else if (otaAdbSideload.IsChecked == true)
                    {
                        cAppend("Rebooting device into recovery...");
                        statusProgress.IsIndeterminate = true;
                        await Task.Run(() => ADB.Instance().Reboot(IDBoot.RECOVERY));
                        if (otaSideloadAutoReboot.IsChecked == true)
                        {
                            //await Task.Run(() => Add(ADB.Instance().Reboot(IDBoot.SIDELOAD_AUTO_REBOOT)));

                        }
                        else
                        {
                            //await Task.Run(() => Add(ADB.Instance().Reboot(IDBoot.SIDELOAD)));
                        }
                        //Waiting for reboot sideload to work without root
                        await Task.Run(() => ADB.WaitForDevice());
                        flashOTAs("user");
                        cAppend("Navigate to reboot your phone using the volume keys and power button to select.");
                    }
                }
            }
            else if (otaBuildList.SelectedIndex != -1)
            {
                IniFileName iniOTA = new IniFileName("./Data/.cached/OTABuildList.ini");
                string entryValue = iniOTA.GetEntryValue(fullDeviceName, otaBuildList.SelectedValue.ToString()).ToString();

                otaListStrLineElements = entryValue.Split(';').ToList();

                //Check if its a new FOTA using regular dl link (no version, just string)
                if (otaListStrLineElements[1] == "true")
                {
                    isFOTA = false;
                    //Set FOTA variables
                    pfOTAFileName = string.Format("{0}.zip", otaListStrLineElements[0]);
                }
                else
                {
                    //Check if it's a full OTA file
                    if (otaListStrLineElements[4] == "true")
                    {
                        isFOTA = true;
                        //Set FOTA variables from ini
                        fOtaVersion = otaListStrLineElements[0];
                        fOtaID = otaListStrLineElements[1];
                        supSHA = otaListStrLineElements[2];
                        pfOTAFileName = string.Format("{0}-ota-{1}-{2}.zip", codeDeviceName, fOtaVersion, fOtaID);
                    }
                    else
                    {
                        isFOTA = false;
                        //Set required variables from ini
                        otaFromVer = otaListStrLineElements[0];
                        otaToVer = otaListStrLineElements[1];
                        otaID = otaListStrLineElements[2];
                        otaMD5 = otaListStrLineElements[3];
                        pOTAFileName = string.Format("{0}.signed-{1}-{2}-from-{3}.zip", otaID, codeDeviceName, otaToVer, otaFromVer);
                    }
                }

                if (_suClient != null && _suClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_twrpClient != null && _twrpClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_stockClient != null && _stockClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_driverClient != null && _driverClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_otaClient != null && _otaClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_modBootClient != null && _modBootClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else
                {
                    //Start checking for existing ota
                    if (!File.Exists(Path.Combine("./Data/Downloads/OTA", pOTAFileName)))
                    {
                        //Start downloading selected ota
                        cAppend(string.Format("Downloading {0}. Please wait... You can continue to use the program while I download it in the background.", pOTAFileName));
                        cAppend("You can cancel the download by double clicking the progress bar at any time!\n");
                        //Declare new webclient
                        _otaClient = new WebClient();
                        //Proxy settings for WebClient
                        IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                        if (defaultProxy != null)
                        {
                            defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                            _otaClient.Proxy = defaultProxy;
                        }
                        //Subscribe to download and completed event handlers
                        _otaClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        _otaClient.DownloadFileCompleted += new AsyncCompletedEventHandler(otaClient_DownloadFileCompleted);
                        // Starts the download, checks if it's a full OTA
                        if (isFOTA == true)
                        {
                            _otaClient.DownloadFileAsync(new Uri(string.Format("https://dl.google.com/dl/android/aosp//{0}", pfOTAFileName)), Path.Combine("./Data/Downloads/OTA", pfOTAFileName));
                        }
                        else if (isFOTA == false)
                        {
                            _otaClient.DownloadFileAsync(new Uri(string.Format("https://android.googleapis.com/packages/ota/google_angler_angler/{0}", pOTAFileName)), Path.Combine("./Data/Downloads/OTA", pOTAFileName));
                        }
                        else
                        {
                            MessageBox.Show("You're not supposed to get here! Please tell me on XDA: FOTA NOT DETECT, thanks!\n\nThe app will now exit.");
                            System.Windows.Application.Current.Shutdown();
                        }
                    }
                    else
                    {
                        statusProgress.IsIndeterminate = true;

                        if (checkOTAHash == true)
                        {

                            if (isFOTA == true)
                            {
                                bool otaSHAResult = await Task.Run(() => checkSHA256("OTA/Full", pfOTAFileName));
                                if (otaSHAResult == true)
                                {
                                    var resultfOTA = await this.ShowMessageAsync("Flash Full OTA", string.Format("The SHA sum returned a match! Are you ready to flash the full OTA {0}?", pfOTAFileName), MessageDialogStyle.AffirmativeAndNegative, flashLaterSettings);
                                    if (resultfOTA == MessageDialogResult.Affirmative)
                                    {
                                        flashOTAs("fOTA");
                                    }
                                    else
                                    {
                                        await this.ShowMessageAsync("Canceled Flash", "You can flash the OTA again anytime by selecting the same OTA and pressing 'Flash OTA'.");
                                    }
                                }
                                else
                                {

                                }
                            }
                            else if (isFOTA == false)
                            {
                                bool lMd5Result = await Task.Run(() => checkOTAMD5());
                                if (lMd5Result == true)
                                {
                                    var resultOTA = await this.ShowMessageAsync("Flash OTA", string.Format("The MD5 sum returned a match! Are you ready to flash the OTA {0}?", pOTAFileName), MessageDialogStyle.AffirmativeAndNegative, flashLaterSettings);
                                    if (resultOTA == MessageDialogResult.Affirmative)
                                    {
                                        flashOTAs("OTA");
                                    }
                                    else
                                    {
                                        await this.ShowMessageAsync("Canceled Flash", "You can flash the OTA again anytime by selecting the same OTA and pressing 'Flash OTA'.");
                                    }
                                }
                                else
                                {
                                    await this.ShowMessageAsync("MD5 Mismatch", "The MD5 returned as non-matching! The OTA has been deleted. Please try again.");
                                    try
                                    {
                                        File.Delete(Path.Combine("./Data/Downloads/OTA", pOTAFileName));
                                    }
                                    catch (Exception ex)
                                    {
                                        await this.ShowMessageAsync("Problem deleting OTA", "There was an error deleting the OTA file. Please check you have sufficent permissions to R/W!\n\n" + ex);
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("You're not supposed to get here! Please tell me on XDA: FOTA NOT DETECT, thanks!\n\nThe app will now exit.");
                                System.Windows.Application.Current.Shutdown();
                            }
                        }
                        else if (checkOTAHash == false)
                        {
                            var resultOTA = await this.ShowMessageAsync("Flash OTA", string.Format("Are you ready to flash the OTA {0}?", pOTAFileName), MessageDialogStyle.AffirmativeAndNegative, flashLaterSettings);
                            if (resultOTA == MessageDialogResult.Affirmative)
                            {
                                flashOTAs("OTA");
                            }
                            else 
                            {
                                await this.ShowMessageAsync("Canceled Flash", "You can flash the OTA again anytime by selecting the same OTA and pressing 'Flash OTA'.");
                            }
                        }

                        statusProgress.IsIndeterminate = false;                     
                    }
                }
            }
        }

        private async void otaClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var flashLaterSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "Later",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            //Check if it was cancled
            if (e.Cancelled)
            {
                //Clean up
                _otaClient.Dispose();
                //Set retries back to 0
                retryLvl = 0;
                //Set progress to 0
                statusProgress.Value = 0;
                cAppend("Download cancelled!");
                cAppend("Cleaning up...\n");
                //Delete attemped file download
                if (File.Exists(Path.Combine("./Data/Downloads/OTA", pOTAFileName)))
                {
                    cAppend(string.Format("Deleting {0}...", pOTAFileName));
                    cAppend("Ready for next download...");
                    File.Delete(Path.Combine("./Data/Downloads/OTA", pOTAFileName));
                }
            }
            //In case of error
            else if (e.Error != null)
            {
                // We have an error! Retry a few times, then abort.
                retryLvl++;
                if (retryLvl < 3)
                {
                    cAppend(string.Format("Failed.. {0} retry...", retryLvl.ToString()));
                    cAppend(string.Format("Error: {0}\n", e.Error.Message));
                    flashOTA_Click(new object(), new RoutedEventArgs());
                }
                else
                {
                    retryLvl = 0;
                    cAppend(string.Format("Failed after 3 retries... Error: {0}", e.Error.Message));
                }
            }
            //No error
            else if (!e.Cancelled)
            {
                //Clean Up
                _otaClient.Dispose();
                //Reset retrys
                retryLvl = 0;
                //Start flashing
                statusProgress.IsIndeterminate = true;
                //Check device state
                statusProgress.IsIndeterminate = true;
                if (isFOTA == true)
                {
                    bool otaSHAResult = await Task.Run(() => checkSHA256("OTA/Full", pfOTAFileName));
                    if (otaSHAResult == true)
                    {
                        var resultfOTA = await this.ShowMessageAsync("Flash Full OTA", string.Format("The SHA sum returned a match! Are you ready to flash the full OTA {0}?", pfOTAFileName), MessageDialogStyle.AffirmativeAndNegative, flashLaterSettings);
                        if (resultfOTA == MessageDialogResult.Affirmative)
                        {
                            flashOTAs("fOTA");
                        }
                        else
                        {
                            await this.ShowMessageAsync("Canceled Flash", "You can flash the OTA again anytime by selecting the same OTA and pressing 'Flash OTA'.");
                        }
                    }
                    else
                    {

                    }
                }
                else if (isFOTA == false)
                {
                    bool lMd5Result = await Task.Run(() => checkOTAMD5());
                    if (lMd5Result == true)
                    {
                        var resultOTA = await this.ShowMessageAsync("Flash OTA", string.Format("The MD5 sum returned a match! Are you ready to flash the OTA {0}?", pOTAFileName), MessageDialogStyle.AffirmativeAndNegative, flashLaterSettings);
                        if (resultOTA == MessageDialogResult.Affirmative)
                        {
                            flashOTAs("OTA");
                        }
                        else
                        {
                            await this.ShowMessageAsync("Canceled Flash", "You can flash the OTA again anytime by selecting the same OTA and pressing 'Flash OTA'.");
                        }
                    }
                    else
                    {
                        await this.ShowMessageAsync("MD5 Mismatch", "The MD5 returned as non-matching! The OTA has been deleted. Please try again.");
                        try
                        {
                            File.Delete(Path.Combine("./Data/Downloads/OTA", pOTAFileName));
                        }
                        catch (Exception ex)
                        {
                            await this.ShowMessageAsync("Problem deleting OTA", "There was an error deleting the OTA file. Please check you have sufficent permissions to R/W!\n\n" + ex);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("You're not supposed to get here! Please tell me on XDA: FOTA NOT DETECT, thanks!\n\nThe app will now exit.");
                    System.Windows.Application.Current.Shutdown();
                }
                statusProgress.IsIndeterminate = false;
            }
        }

        private void openDataFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(appPath + "/Data/");
        }

        private async void flashModBoot_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            var noImageSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            if (modBootBuildList.SelectedIndex == -1)
            {
                cAppend("No modified boot image selected...");
                await this.ShowMessageAsync("No modified boot image selected", "Please select a modified boot image and try again.", MessageDialogStyle.Affirmative, noImageSettings);
                modBootBuildList.IsDropDownOpen = true;
            }
            if (modBootBuildList.SelectedIndex == 0)
            {
                // Create OpenFileDialog 
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                // Set filter for file extension and default file extension and title
                dlg.Title = "Open file for Modified Boot image";
                dlg.DefaultExt = ".img";
                dlg.Filter = "Modified Boot Image (*.img)|*.img";

                // Display OpenFileDialog by calling ShowDialog method 
                Nullable<bool> result = dlg.ShowDialog();

                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;
                    cAppend(string.Format("Flashing Boot Image{0}\n", Path.GetFileName(filename)));
                    statusProgress.IsIndeterminate = true;
                    tabControl.IsEnabled = false;

                    var controllerFlashBoot = await this.ShowProgressAsync("Flashing Boot...", filename);
                    controllerFlashBoot.SetIndeterminate();

                    await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.BOOT, filename));

                    await controllerFlashBoot.CloseAsync();
                    tabControl.IsEnabled = false;
                    statusProgress.IsIndeterminate = false;
                }
            }
            else if (modBootBuildList.SelectedIndex != -1)
            {
                IniFileName ini = new IniFileName("./Data/.cached/ModBootBuildList.ini");
                string[] sectionValues = ini.GetEntryNames("Nexus 6P (Huawei Nexus 6P)");
                string entryValue = ini.GetEntryValue("Nexus 6P (Huawei Nexus 6P)", modBootBuildList.SelectedValue.ToString()).ToString();

                modBootListStrLineElements = entryValue.Split(';').ToList();

                //Set required variables from ini
                modBootVersion = modBootListStrLineElements[0];
                modBootExportCode = modBootListStrLineElements[1];
                pModBootFileName = string.Format("modified-boot-{0}-angler.img", modBootVersion);

                if (_suClient != null && _suClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_twrpClient != null && _twrpClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_stockClient != null && _stockClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_driverClient != null && _driverClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_otaClient != null && _otaClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else if (_modBootClient != null && _modBootClient.IsBusy == true)
                {
                    await this.ShowMessageAsync("Download in progress...",
                        "A download is already in progress! I cannot start another one, please wait for the exisiting download to finish or cancel it by double-clicking the progress bar.",
                        MessageDialogStyle.Affirmative, mySettings);
                }
                else
                {
                    //Start checking for existing tgz
                    if (!File.Exists(Path.Combine("./Data/Downloads/ModBoot", pModBootFileName)))
                    {
                        //Start downloading selected tgz
                        cAppend(string.Format("Downloading {0}. Please wait... You can continue to use the program while I download it in the background.", pModBootFileName));
                        cAppend("You can cancel the download by double clicking the progress bar at any time!\n");
                        //Declare new webclient
                        _modBootClient = new WebClient();
                        //Proxy for WebClient
                        IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                        if (defaultProxy != null)
                        {
                            defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                            _modBootClient.Proxy = defaultProxy;
                        }
                        //Subscribe to download and completed event handlers
                        _modBootClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        _modBootClient.DownloadFileCompleted += new AsyncCompletedEventHandler(modBootClient_DownloadFileCompleted);
                        // Starts the download
                        //_modBootClient.DownloadFileAsync(new Uri(string.Format("https://drive.google.com/uc?export=download&id={0}", modBootExportCode)), Path.Combine("./Data/Downloads/ModBoot", pModBootFileName));
                        _modBootClient.DownloadFileAsync(new Uri("http://opengapps.org/?download=true&arch=arm&api=7.0&variant=pico"), "./Data/Downloads/ModBoot/picogapps.zip");
                    }
                    else
                    {
                        statusProgress.IsIndeterminate = true;
                        {
                            var result = await this.ShowMessageAsync("Download complete", string.Format("Are you ready to flash {0}?", pModBootFileName), MessageDialogStyle.AffirmativeAndNegative, mySettings);
                            if (result == MessageDialogResult.Affirmative)
                            {
                                IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                                if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                                {
                                    var controller = await this.ShowProgressAsync("Waiting for device...", "");
                                    controller.SetIndeterminate();
                                    cAppend("Waiting for device...");
                                    await Task.Run(() => ADB.WaitForDevice());
                                    cAppend("Rebooting to the bootloader.");
                                    await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                                    cAppend("Flashing Modified Boot Image...");
                                    await controller.CloseAsync();
                                    flashModBootImage();
                                }
                                else if (state == IDDeviceState.FASTBOOT)
                                {
                                    flashModBootImage();
                                }
                                else
                                {
                                    cAppend("Your device is in the wrong state. Please put your device in the bootloader.\n");
                                }
                            }
                            else
                            {
                                cAppend("Flash canceled.\n");
                            }
                        }
                        statusProgress.IsIndeterminate = false;
                    }
                }
            }
        }

        private async void modBootClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            //Check if it was cancled
            if (e.Cancelled)
            {
                //Reset retrys
                retryLvl = 0;
                _modBootClient.Dispose();
                //Set progress to 0
                statusProgress.Value = 0;
                cAppend("Download cancelled!");
                cAppend("Cleaning up...\n");
                //Delete attemped file download
                if (File.Exists(Path.Combine("./Data/Downloads/ModBoot", pModBootFileName)))
                {
                    cAppend(string.Format("Deleting {0}...", pModBootFileName));
                    cAppend("Ready for next download.");
                    File.Delete(Path.Combine("./Data/Downloads/ModBoot", pModBootFileName));
                }
            }
            //In case of error
            else if (e.Error != null)
            {
                // We have an error! Retry a few times, then abort.
                retryLvl++;
                if (retryLvl < 3)
                {
                    cAppend(string.Format("Failed.. {0} retry...", retryLvl.ToString()));
                    cAppend(string.Format("Error: {0}\n", e.Error.Message));
                    flashModBoot_Click(new object(), new RoutedEventArgs());
                }
                else
                {
                    retryLvl = 0;
                    cAppend(string.Format("Failed after 3 retries... Error: {0}", e.Error.Message));
                }
            }
            //No error
            else if (!e.Cancelled)
            {
                //Reset retrys
                retryLvl = 0;
                //Clean up webclient
                _modBootClient.Dispose();
                statusProgress.Value = 0;
                //start flashing
                statusProgress.IsIndeterminate = true;

                var result = await this.ShowMessageAsync("Download complete", string.Format("Are you ready to flash {0}?", pModBootFileName), MessageDialogStyle.AffirmativeAndNegative, mySettings);
                if (result == MessageDialogResult.Affirmative)
                {
                    IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);
                    if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY)
                    {
                        var controllerRebootingRecoveryFlash = await this.ShowProgressAsync("Waiting for device...", "");
                        controllerRebootingRecoveryFlash.SetIndeterminate();
                        cAppend("Waiting for device...");
                        await Task.Run(() => ADB.WaitForDevice());
                        cAppend("Rebooting to the bootloader.");
                        await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                        cAppend("Flashing Modified Boot Image...");
                        await controllerRebootingRecoveryFlash.CloseAsync();
                        flashModBootImage();
                    }
                    else if (state == IDDeviceState.FASTBOOT)
                    {
                        flashModBootImage();
                    }
                    else
                    {
                        cAppend("Your device is in the wrong state. Please put your device in the bootloader.\n");
                    }
                }
                statusProgress.IsIndeterminate = false;
            }
        }

        private void xdaThread_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://forum.xda-developers.com/nexus-6p/orig-development/squabbi-s-nexus-6p-toolkit-v1-0-26-12-15-t3279045");
        }

        private void rbEzFactoryMode_Checked(object sender, RoutedEventArgs e)
        {
            //Set visual abilities
            gEzFactoryOptions.IsEnabled = true;
            gEzFactoryOptions.Background = (Brush)new BrushConverter().ConvertFrom("#FFF7F7F7");
            gAdvFlashOptions.IsEnabled = false;
            gAdvFlashOptions.Background = (Brush)new BrushConverter().ConvertFrom("#FFCDCDCD");
        }

        private void rbAdvFactoryMode_Checked(object sender, RoutedEventArgs e)
        {
            //Set visual abilities
            gEzFactoryOptions.IsEnabled = false;
            gEzFactoryOptions.Background = (Brush)new BrushConverter().ConvertFrom("#FFCDCDCD");
            gAdvFlashOptions.IsEnabled = true;
            gAdvFlashOptions.Background = (Brush)new BrushConverter().ConvertFrom("#FFF7F7F7");
        }

        private async void howToFactoryResetStock_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            await this.ShowMessageAsync("Showing stock recovery options", "Once you're in recovery and you see an Android lying down with it's chest open.\n\n" + 
                "Hold the power button (for less than 10 seconds or your phone will reboot!) and press the volume up button once!", MessageDialogStyle.Affirmative, mySettings);
        }

        private void showProxySettings_Click(object sender, RoutedEventArgs e)
        {
                        
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 1;
        }

        private async void menuResetListVersion_Click(object sender, RoutedEventArgs e)
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/MaterialDesignThemes.MahApps;component/Themes/MaterialDesignTheme.MahApps.Dialogs.xaml");

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "No",
                SuppressDefaultResources = true,
                CustomResourceDictionary = dictionary
            };

            Squabbi.Toolkit.Nexus6P.Properties.Settings.Default["listVersion"] = 0;
            Squabbi.Toolkit.Nexus6P.Properties.Settings.Default.Save();

            await this.ShowMessageAsync("Reset List Version Counter", "Restart the toolkit for effect.", MessageDialogStyle.Affirmative, mySettings);
        }
    }
}
