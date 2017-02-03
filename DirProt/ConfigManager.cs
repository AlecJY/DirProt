using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace DirProt {
    public class ConfigManager {
        public static Config LoadConfig(string filename) {
            string jsonString = "";
            try {
                jsonString = File.ReadAllText(filename);
            } catch (Exception) {
                Console.Error.WriteLine("Config file not found");
                Environment.Exit(-1);
            }
            try {
                return JsonConvert.DeserializeObject<Config>(jsonString);
            } catch (JsonReaderException e) {
                Console.Error.WriteLine("Cannot parse config file");
                Console.Error.WriteLine(e);
                Environment.Exit(-1);
                return null;
            }
        }

        public static void SaveConfig(Config config, string filename) {
            string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
            try {
                File.WriteAllText(filename, jsonString);
            } catch (Exception e) {
                Console.Error.WriteLine(e);
            }
        }

        public static DirTable LoadDirTable(string filename) {
            string jsonString = "";
            try {
                jsonString = File.ReadAllText(filename);
            } catch (Exception) {
                Console.WriteLine("Dir table not found\nCreate new dir table");
            }
            try {
                DirTable dirTable = JsonConvert.DeserializeObject<DirTable>(jsonString);
                if (dirTable != null) {
                    return dirTable;
                } else {
                    return new DirTable();
                }
            } catch (JsonReaderException e) {
                Console.Error.WriteLine("Cannot parse config file");
                Console.Error.WriteLine(e);
                Environment.Exit(-1);
                return null;
            }
        }

        public static void SaveDirTable(DirTable dirTable, string path) {
            string jsonString = JsonConvert.SerializeObject(dirTable, Formatting.Indented);
            try {
                File.WriteAllText(path, jsonString);
            }
            catch (Exception e) {
                Console.Error.WriteLine(e);
            }
        }
    }

    public class Config {
        public bool Enabled { get; set; }
        public List<string> ProtectedDir { get; set; } = new List<string>();
        public List<string> ProtectedTaskbar { get; set; } = new List<string>();
    }

    public class DirTable {
        public List<DirPath> Directorys { get; set; } = new List<DirPath>();
        public List<RegistryDir> Registries { get; set; } = new List<RegistryDir>();
    }

    public class DirPath {
        public string path { get; set; } = "";
        public string hash { get; set; } = "";
        public uint index { get; set; }
    }

    public class RegistryDir {
        public string path { get; set; } = "";
        public List<RegistryData> RegistryData { get; set; } = new List<RegistryData>();
    }

    public class RegistryData {
        public string name { get; set; } = "";
        public RegistryValueKind type { get; set; }
        public Object value { get; set; }
    }
}