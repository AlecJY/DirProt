using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DirProt {
    public class ConfigManager {
        public Config LoadConfig(string filename) {
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

        public DirTable LoadDirTable(string filename) {
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

        public void SaveDirectorys(List<DirPath> directorys, string path) {
            DirTable dirTable = new DirTable();
            dirTable.Directorys = directorys;
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
    }

    public class DirTable {
        public List<DirPath> Directorys { get; set; } = new List<DirPath>();
    }

    public class DirPath {
        public string path { get; set; } = "";
        public string hash { get; set; } = "";
        public uint index { get; set; }
    }
}