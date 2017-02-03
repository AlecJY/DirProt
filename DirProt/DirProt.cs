using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace DirProt {
    class DirProt {
        private static readonly string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                         Path.DirectorySeparatorChar;

        public DirProt() {
            ConfigManager configManager = new ConfigManager();
            Config config = configManager.LoadConfig(AppDir + "config.json");
            List<DirPath> directorys = configManager.LoadDirTable(AppDir + "data" + Path.DirectorySeparatorChar + "dirtable.json").Directorys;
            if (config.Enabled) {
                foreach (string iPath in config.ProtectedDir) {
                    string path = iPath.ToLower();
                    bool isBackuped = false;
                    DirPath backupDir = null;
                    foreach (DirPath directory in directorys) {
                        if (path.Equals(directory.path)) {
                            isBackuped = true;
                            backupDir = directory;
                        }
                    }
                    if (isBackuped) {
                        DirectoryInfo directoryInfo = new DirectoryInfo(path);
                        if (directoryInfo.Exists) {
                            foreach (FileSystemInfo childInfo in directoryInfo.GetFileSystemInfos()) {
                                DeleteReadOnly(childInfo);
                            }
                        }
                        CopyDirectory(AppDir + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + backupDir.hash + Path.DirectorySeparatorChar + backupDir.index, path);
                    } else {
                        Backup(path, directorys);
                    }
                }
                configManager.SaveDirectorys(directorys, AppDir + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar + "dirtable.json");
            }
        }

        public void Backup(string path, List<DirPath> directorys) {
            SHA256 sha256 = new SHA256CryptoServiceProvider();
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
                        Console.Error.WriteLine(e);
                    }
                }
            }
            else {
                Console.Error.WriteLine("Directory not found: " + srcPath);
            }
        }

        private void DeleteReadOnly(FileSystemInfo fileSystemInfo) {
            var directoryInfo = fileSystemInfo as DirectoryInfo;
            if (directoryInfo != null && directoryInfo.Exists) {
                foreach (FileSystemInfo childInfo in directoryInfo.GetFileSystemInfos()) {
                    DeleteReadOnly(childInfo);
                }
            }
            try {
                fileSystemInfo.Attributes = FileAttributes.Normal;
                fileSystemInfo.Delete();
            }
            catch (Exception e) {
                Console.Error.WriteLine(e);
            }
        }

        static void Main(string[] args) {
            new DirProt();
        }
    }
}
