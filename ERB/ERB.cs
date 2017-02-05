using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using DirProt;

namespace ERB {
    class ERB {
        private static readonly string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                         Path.DirectorySeparatorChar;

        public static void Main(string[] args) {
            Config config = ConfigManager.LoadConfig(AppDir + "config.json");
            if (config.Enabled && config.EmptyRecycleBin) {
                EmptyRecycleBin();
            }
        }

        private static void EmptyRecycleBin() {
            uint code = SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlag.SHERB_NOCONFIRMATION | RecycleFlag.SHERB_NOPROGRESSUI | RecycleFlag.SHERB_NOSOUND);
            if (code != 0) {
                Console.Error.WriteLine(code + " Empty recycle bin failed.");
            }
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlag dwFlags);

        enum RecycleFlag: uint {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }
    }
}
