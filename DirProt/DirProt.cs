﻿using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using log4net;
using Microsoft.Win32;

namespace DirProt {
    public class DirProt {
        private static readonly string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                         Path.DirectorySeparatorChar;
        private static ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DirProt(bool backup) {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(AppDir + "log.config"));
            Logger.Info("Loading config");
            Config config = ConfigManager.LoadConfig(AppDir + "config.json");
            List<string> users = new List<string>();
            foreach (string user in config.ProtectedTaskbar) {
                try {
                    NTAccount account = new NTAccount(user);
                    SecurityIdentifier securityIdentifier =
                        (SecurityIdentifier) account.Translate(typeof(SecurityIdentifier));
                    string sid = securityIdentifier.ToString();
                    string homeDir = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\" + sid).GetValue("ProfileImagePath").ToString();
                    config.ProtectedDir.Add(homeDir + @"\AppData\Roaming\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
                    users.Add(sid);
                } catch (Exception e) {
                    Console.Error.WriteLine(e);
                }
            }
            DirTable dirTable =
                ConfigManager.LoadDirTable(AppDir + "data" + Path.DirectorySeparatorChar + "dirtable.json");
            List<DirPath> directorys = dirTable.Directorys;
            if (config.Enabled || backup) {
                foreach (string iPath in config.ProtectedDir) {
                    Logger.Info("Checking \"" + iPath + "\" backup...");
                    string path = iPath.ToLower();
                    bool isBackuped = false;
                    DirPath backupDir = null;
                    foreach (DirPath directory in directorys) {
                        if (path.Equals(directory.path)) {
                            isBackuped = true;
                            backupDir = directory;
                            break;
                        }
                    }
                    if (isBackuped) {
                        Logger.Info("Start restore backup");
                        DirectoryInfo directoryInfo = new DirectoryInfo(path);
                        if (directoryInfo.Exists) {
                            foreach (FileSystemInfo childInfo in directoryInfo.GetFileSystemInfos()) {
                                try {
                                    DeleteReadOnly(childInfo);
                                } catch (Exception e) {
                                    Console.WriteLine(e);
                                }
                            }
                        }
                        CopyDirectory(AppDir + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + backupDir.hash + Path.DirectorySeparatorChar + backupDir.index, path);
                        Logger.Info("Successfully restore");
                    } else {
                        Logger.Info("Start backup");
                        Backup(path, directorys);
                        Logger.Info("Successfully backup");
                    }
                }
                foreach (string user in users) {
                    Logger.Info("Checking " + user + " taskbar backup...");
                    bool isBackuped = false;
                    RegistryDir reg = null;
                    foreach (RegistryDir registryDir in dirTable.Registries) {
                        if (registryDir.path.Equals(user + @"\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband")) {
                            isBackuped = true;
                            reg = registryDir;
                            break;
                        }
                    }
                    if (isBackuped) {
                        Logger.Info("Start restore taskbar registry...");
                        RegistryKey taskband = Registry.Users.OpenSubKey(reg.path, true);
                        while (taskband == null) {
                            Thread.Sleep(5000);
                            taskband = Registry.Users.OpenSubKey(reg.path, true);
                        }
                        foreach (string valueName in taskband.GetValueNames()) {
                            taskband.DeleteValue(valueName);
                        }
                        foreach (RegistryData registryData in reg.RegistryData) {
                            if (registryData.type.Equals(RegistryValueKind.Binary)) {
                                registryData.value = Convert.FromBase64String((string) registryData.value);
                            }
                            taskband.SetValue(registryData.name, registryData.value, registryData.type);
                        }
                        Logger.Info("Successfully restore taskbar");
                    } else {
                        Logger.Info("Start backup taskbar registry...");
                        reg = new RegistryDir();
                        reg.path = user + @"\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband";
                        RegistryKey taskband = Registry.Users.OpenSubKey(reg.path);
                        while (taskband == null) {
                            Thread.Sleep(5000);
                            taskband = Registry.Users.OpenSubKey(reg.path);
                        }
                        foreach (string valueName in taskband.GetValueNames()) {
                            RegistryData regData = new RegistryData();
                            regData.name = valueName;
                            regData.type = taskband.GetValueKind(valueName);
                            regData.value = taskband.GetValue(valueName);
                            reg.RegistryData.Add(regData);
                        }
                        dirTable.Registries.Add(reg);
                        Logger.Info("Successfully backup taskbar");
                    }
                }
                ConfigManager.SaveDirTable(dirTable, AppDir + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "dirtable.json");
                if (backup) {
                    MessageBox.Show("Successfully backuped.", "DirProt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            } else {
                Logger.Info("DirProt is disabled.");
            }
            Environment.Exit(0);
        }

        public void Backup(string path, List<DirPath> directorys) {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(path));
            string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
            uint index = 0;
            foreach (DirPath directory in directorys) {
                if (hash.Equals(directory.hash)) {
                    index++;
                }
            }
            DirPath dirPath = new DirPath();
            dirPath.path = path;
            dirPath.hash = hash;
            dirPath.index = index;
            directorys.Add(dirPath);
            CopyDirectory(path, AppDir + "data" + Path.DirectorySeparatorChar + hash + Path.DirectorySeparatorChar + index);
        }

        private void CopyDirectory(string srcPath, string dstPath) {
            if (Directory.Exists(srcPath)) {
                if (!Directory.Exists(dstPath)) {
                    Directory.CreateDirectory(dstPath);
                }
                DirectoryInfo srcDir = new DirectoryInfo(srcPath);
                foreach (FileInfo file in srcDir.GetFiles()) {
                    try {
                        FileInfo dstFile = new FileInfo(dstPath + Path.DirectorySeparatorChar + file.Name);
                        if (dstFile.Exists) {
                            dstFile.Attributes = FileAttributes.Normal;
                        }
                        file.CopyTo(dstPath + Path.DirectorySeparatorChar + file.Name, true);
                    }
                    catch (Exception e) {
                        Logger.Warn(e);
                        Console.Error.WriteLine(e);
                    } 
                }
                foreach (DirectoryInfo directory in srcDir.GetDirectories()) {
                    try {
                        DirectoryInfo dstSubDir =
                            new DirectoryInfo(dstPath + Path.DirectorySeparatorChar + directory.Name);
                        dstSubDir.Create();
                        dstSubDir.Attributes = directory.Attributes;
                        CopyDirectory(directory.FullName, dstSubDir.FullName);
                    }
                    catch (Exception e) {
                        Logger.Warn(e);
                        Console.Error.WriteLine(e);
                    }
                }
            }
            else {
                Logger.Error("Directory not found: " + srcPath);
                Console.Error.WriteLine("Directory not found: " + srcPath);
            }
        }

        private static void DeleteReadOnly(FileSystemInfo fileSystemInfo) {
            var directoryInfo = fileSystemInfo as DirectoryInfo;
            if (directoryInfo != null && directoryInfo.Exists) {
                foreach (FileSystemInfo childInfo in directoryInfo.GetFileSystemInfos()) {
                    try {
                        DeleteReadOnly(childInfo);
                    } catch (Exception e) {
                        Logger.Warn(e);
                        Console.Error.WriteLine(e);
                    }
                }
            }
            try {
                fileSystemInfo.Attributes = FileAttributes.Normal;
                fileSystemInfo.Delete();
            }
            catch (Exception e) {
                Logger.Warn(e);
                Console.Error.WriteLine(e);
            }
        }

        static void Main(string[] args) {
            if (args.Length > 0) {
                if (args[0].ToLower().Equals("/install") && args.Length == 2) {
                    try {
                        ManagedInstallerClass.InstallHelper(new[] {AppDir + "DirProtSvc.exe"});
                    } catch (Exception e) {
                        Console.Error.WriteLine(e);
                    }
                    try {
                        NTAccount account = new NTAccount(args[1]);
                        SecurityIdentifier securityIdentifier =
                            (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                        string sid = securityIdentifier.ToString();
                        RegistryKey run =
                            Registry.Users.OpenSubKey(sid + @"\Software\Microsoft\Windows\CurrentVersion\Run", true);
                        run.SetValue("EmptyRecycleBin", AppDir + "ERB.exe");
                    } catch (Exception e) {
                        Console.Error.WriteLine(e);
                    }
                } else if (args[0].ToLower().Equals("/uninstall") && args.Length == 2) {
                    try {
                        ManagedInstallerClass.InstallHelper(new[] {"/u", AppDir + "DirProtSvc.exe"});
                    } catch (Exception e) {
                        Console.Error.WriteLine(e);
                    }
                    try {
                        NTAccount account = new NTAccount(args[1]);
                        SecurityIdentifier securityIdentifier =
                            (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                        string sid = securityIdentifier.ToString();
                        RegistryKey run =
                            Registry.Users.OpenSubKey(sid + @"\Software\Microsoft\Windows\CurrentVersion\Run", true);
                        run.DeleteValue("EmptyRecycleBin");
                    } catch (Exception e) {
                        Console.WriteLine(e);
                        throw;
                    }
                } else if (args[0].ToLower().Equals("/backup")) {
                    DirectoryInfo dataDir = new DirectoryInfo(AppDir + "data");
                    if (dataDir.Exists) {
                        DeleteReadOnly(dataDir);
                    }
                    new DirProt(true);
                } else if (args[0].ToLower().Equals("/enable")) {
                    Config config = ConfigManager.LoadConfig(AppDir + "config.json");
                    config.Enabled = true;
                    ConfigManager.SaveConfig(config, AppDir + "config.json");
                } else if (args[0].ToLower().Equals("/disable")) {
                    Config config = ConfigManager.LoadConfig(AppDir + "config.json");
                    config.Enabled = false;
                    ConfigManager.SaveConfig(config, AppDir + "config.json");
                } else {
                    Console.Error.WriteLine("Wrong arguments");
                }
            } else {
                new DirProt(false);
            }
        }
    }
}
