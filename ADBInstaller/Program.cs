using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace ADBInstaller {
    internal class Program {
        internal static void Main(string[] args) {
            try {
                "┣━━━━━━  ".Write();
                "ADB Installer v1.2.0".Write(ConsoleColor.Green);
                " ━━━━━━┫".WriteLine();
                "".WriteLine();
                "".WriteLine();
                var f = false;
                for (int i = 0; i < args.Length; i++) {
                    if(args[i] == "--uninstall") {
                        f = true;
                        Uninstall();
                        break;
                    }
                }
                if(!f) Task.Run(async () => await Install()).Wait();
            } catch (Exception ex) {
                $"An unexpected error occurred\n{ex}".WriteInfo(InfoState.EXCEPTION);
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static async Task Install() { // 오우 쓋 이 garbage는 뭐야
            ApplicationInfo info = null;
            string adb_zip_file = null;
            string scrcpy_zip_file = null;
            try {
                var i = 0;
                while(i++ == 0) {
                    // 권한 확인
                    "Checking permissions".WriteLine();
                    try {
                        using (var f = Installer.AddApplication($"testApp_{new Random().Next(10000, 99999)}"))
                            f.Delete();
                    } catch (Exception ex) {
                        $"Insufficient permissions or an error occurred\n{ex}".WriteInfo(InfoState.ERROR);
                        break;
                    }

                    // scrcpy 추가
                    bool install_scrcpy_d = false;
                    while (true) {
                        ("Would you like to install additional scrcpy 1.17?\n" +
                        "https://github.com/Genymobile/scrcpy/\n (y or n) : ").Write(ConsoleColor.Yellow);
                        var r = Console.ReadKey().KeyChar.ToString().ToLower();
                        if(r == "n") {
                            install_scrcpy_d = false;
                            break;
                        } else if (r == "y") {
                            install_scrcpy_d = true;
                            break;
                        }
                        "".WriteLine();
                    }
                    "\n".WriteLine();

                    // 라이센스
                    if (install_scrcpy_d)
                        "scrcpy, Please read and accept the license\nhttps://github.com/Genymobile/scrcpy/blob/master/LICENSE\n".WriteLine();

                    "Please read and accept the license\nhttps://developer.android.com/studio/releases/platform-tools\nWaiting for 5 seconds\n".WriteLine();
                    await Task.Delay(5000); // 오우 쓋 겁나 형식적이야

                    // 다운로드
                    "Downloading ADB...".WriteLine(ConsoleColor.Magenta);
                    while (File.Exists(adb_zip_file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))) { }
                    if (!await DownloadFile("https://dl.google.com/android/repository/platform-tools-latest-windows.zip", adb_zip_file)) break;

                    // scrcpy 다운로드
                    if(install_scrcpy_d) {
                        "Downloading scrcpy...".WriteLine(ConsoleColor.Magenta);
                        while (File.Exists(scrcpy_zip_file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))) { }
                        if (!await DownloadFile("https://github.com/Genymobile/scrcpy/releases/download/v1.17/scrcpy-win32-v1.17.zip", scrcpy_zip_file)) break;
                    }

                    // 암축 풀기
                    "\nADB Decompressing...".WriteLine(ConsoleColor.Magenta);
                    var install_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ADB");
                    try {
                        Decompressing("ADB", install_dir, adb_zip_file, true);
                        if (install_scrcpy_d) {
                            "\nscrcpy Decompressing...".WriteLine(ConsoleColor.Magenta);
                            Decompressing("scrcpy", Path.Combine(install_dir, "platform-tools"), scrcpy_zip_file, false);
                        }
                    } catch (Exception ex) {
                        "".WriteLine();
                        $"Decompression failure\n{ex.Message}".WriteInfo(InfoState.ERROR);
                        break;
                    }

                    // 설치
                    "Installing..".WriteLine(ConsoleColor.Cyan);
                    string installer_path;
                    try {
                        installer_path = Path.Combine(install_dir, "ADBInstaller.exe");
                    } catch (Exception ex) {
                        $"Copy failure\n{ex}".WriteInfo(InfoState.ERROR);
                        return;
                    }

                    try { File.Copy(Assembly.GetExecutingAssembly().Location, installer_path, true); } catch (Exception ex) { ex.WriteInfo(InfoState.WARNING); }
                    try {
                        info = Installer.AddApplication("ADB");
                        info.DisplayName = "Android SDK Platform-tools";
                        info.DisplayVersion = "31.0.1 >=";
                        info.EstimatedSize = (int)Directory.CreateDirectory(install_dir).GetDirectorySize();
                        info.InstallLocation = install_dir;
                        info.DisplayIcon = Path.Combine(install_dir, "adb.exe");
                        info.UninstallString = $"\"{installer_path}\" --uninstall";
                        info.HelpLink = "https://developer.android.com/studio/command-line/adb";
                        info.URLInfoAbout = "https://github.com/Soju06/ADBInstaller";
                    } catch (Exception ex) {
                        $"Failure to add app to registry\n{ex}".WriteInfo(InfoState.EXCEPTION);
                    }

                    // 환경 변수
                    var h = Path.Combine(install_dir, "platform-tools");
                    var k = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
                    if (!k.Contains(h))
                        //Console.WriteLine($"{k};{h}");
                        Environment.SetEnvironmentVariable("Path", $"{k};{h};", EnvironmentVariableTarget.Machine);

                    "Installation is complete.".WriteLine(ConsoleColor.Green);
                }
            } catch (Exception ex) {
                $"An unexpected error occurred\n{ex}".WriteInfo(InfoState.EXCEPTION);
            } 
            finally {
                "Removing temporary files...".WriteLine();
                try {
                    if (File.Exists(adb_zip_file)) File.Delete(adb_zip_file);
                    if (File.Exists(scrcpy_zip_file)) File.Delete(scrcpy_zip_file);
                } catch {

                }
                info?.Dispose();
            }
        }

        private static void Uninstall() {
            var info = Installer.AddApplication("ADB");
            var installed_dir = info.InstallLocation;
            info.Delete();
            if (Directory.Exists(installed_dir)) {
                var dir = new DirectoryInfo(installed_dir);

                foreach (var item in dir.GetFiles()) {
                    try {
                        item.Delete();
                    } catch {

                    }
                }
                
                foreach (var item in dir.GetDirectories()) {
                    try {
                        item.Delete(true);
                    } catch {

                    }
                }
            }
            "Has been removed.".WriteLine(ConsoleColor.Green);
        }

        private async static Task<bool> DownloadFile(string url, string path) {
            using(var webClient = new WebClient()) {
                AsyncCompletedEventArgs args = null;
                string ls = "";
                webClient.DownloadProgressChanged += ProgressChanged;
                webClient.DownloadFileCompleted += Completed;
                webClient.DownloadFileAsync(new Uri(url), path);
                while (args == null) await Task.Delay(200);

                if(args.Cancelled || args.Error != null) {
                    $"The download operation has been cancelled.\n{args.Error}".WriteInfo(InfoState.ERROR);
                    return false;
                }

                void Completed(object sender, AsyncCompletedEventArgs e) {
                    string f = "";
                    for (int i = 0; i < ls.Length; i++) f += '\b';
                    ls = $"Downloading.. 100%";
                    $"{f}{ls}".WriteLine(ConsoleColor.Green);
                    args = e;
                }

                void ProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
                    string f = "";
                    for (int i = 0; i < ls.Length; i++) f += '\b';
                    ls = $"Downloading.. {e.ProgressPercentage}%";
                    $"{f}{ls}".Write(ConsoleColor.Yellow);
                }
                return true;
            }
        }

        private static void Decompressing(string name, string dir, string file, bool overwrite) {
            string ls = "";
            Directory.CreateDirectory(dir);
            using (var zip = ZipFile.OpenRead(file)) {
                int j = 0, lg = zip.Entries.Count;
                foreach (var entry in zip.Entries) {
                    string f = "";
                    for (int l = 0; l < ls.Length; l++) f += '\b';
                    ls = $"{name} Decompressing.. {j++ / lg}%";
                    $"{f}{ls}".Write(ConsoleColor.Yellow);
                    var filepath = Path.Combine(dir, entry.FullName);
                    var subDir = Path.GetDirectoryName(filepath);
                    if (!Directory.Exists(subDir))
                        Directory.CreateDirectory(subDir);
                    if(!string.IsNullOrWhiteSpace(entry.Name)) { // 폴더가 아니면
                        if (overwrite) entry.ExtractToFile(filepath, true);
                        else if (!File.Exists(filepath)) entry.ExtractToFile(filepath);
                    }
                }
            }
            string w = "";
            for (int e = 0; e < ls.Length; e++) w += '\b';
            ls = $"{name} Decompressing.. 100%";
            $"{w}{ls}".WriteLine(ConsoleColor.Green);
        }
    }

    internal static class ConsoleExp {
        private static readonly object ConsoleLock = new();
        public static void WriteLine(this string s, ConsoleColor? color = null) {
            lock (ConsoleLock) {
                var c = Console.ForegroundColor;
                if(color.HasValue)
                    Console.ForegroundColor = color.Value;
                Console.WriteLine(s);
                Console.ForegroundColor = c;
            }
        }

        public static void Write(this string s, ConsoleColor? color = null) {
            lock (ConsoleLock) {
                var c = Console.ForegroundColor;
                if (color.HasValue)
                    Console.ForegroundColor = color.Value;
                Console.Write(s);
                Console.ForegroundColor = c;
            }
        }

        public static void WriteLine(object s, ConsoleColor? color = null) {
            lock (ConsoleLock) {
                var c = Console.ForegroundColor;
                if(color.HasValue)
                    Console.ForegroundColor = color.Value;
                Console.WriteLine(s);
                Console.ForegroundColor = c;
            }
        }

        public static void Write(this object s, ConsoleColor? color = null) {
            lock (ConsoleLock) {
                var c = Console.ForegroundColor;
                if (color.HasValue)
                    Console.ForegroundColor = color.Value;
                Console.Write(s);
                Console.ForegroundColor = c;
            }
        }

        public static void WriteInfo(this object s, InfoState state) =>
            WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss} {state}] {s}", state switch {
                InfoState.ERROR or InfoState.EXCEPTION => ConsoleColor.Red,
                InfoState.WARNING => ConsoleColor.Yellow, _ => null });
    }

    internal enum InfoState {
        INFO,
        MESSAGE, 
        WARNING, 
        ERROR, 
        EXCEPTION
    }

    internal static class RegistryExp {
        public static void Delete(this RegistryKey key) {
            using (var parentKey = key.GetParent(true)) {
                var keyName = key.Name.Split('\\').Last();
                parentKey.DeleteSubKeyTree(keyName);
            }
        }

        public static RegistryKey GetParent(this RegistryKey key) =>
            key.GetParent(false);

        public static RegistryKey GetParent(this RegistryKey key, bool writable) {
            var items = key.Name.Split('\\');
            var hiveName = items.First();
            var parentKeyName = string.Join("\\", items.Skip(1).Reverse().Skip(1).Reverse());
            var hive = GetHive(hiveName);
            using var baseKey = RegistryKey.OpenBaseKey(hive, key.View);
            return baseKey.OpenSubKey(parentKeyName, writable);
        }

        private static RegistryHive GetHive(string hiveName) {
            if (hiveName.Equals("HKEY_CLASSES_ROOT", StringComparison.OrdinalIgnoreCase))
                return RegistryHive.ClassesRoot;
            else if (hiveName.Equals("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
                return RegistryHive.CurrentUser;
            else if (hiveName.Equals("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
                return RegistryHive.LocalMachine;
            else if (hiveName.Equals("HKEY_USERS", StringComparison.OrdinalIgnoreCase))
                return RegistryHive.Users;
            else if (hiveName.Equals("HKEY_PERFORMANCE_DATA", StringComparison.OrdinalIgnoreCase))
                return RegistryHive.PerformanceData;
            else if (hiveName.Equals("HKEY_CURRENT_CONFIG", StringComparison.OrdinalIgnoreCase))
                return RegistryHive.CurrentConfig;
            else if (hiveName.Equals("HKEY_DYN_DATA", StringComparison.OrdinalIgnoreCase))
                return RegistryHive.DynData;
            else throw new NotImplementedException(hiveName);
        }
    }

    internal static class DirectoryExp {
        internal static long GetDirectorySize(this DirectoryInfo info) {
            long size = 0;
            foreach (FileInfo fi in info.GetFiles("*", SearchOption.AllDirectories))
                size += fi.Length;
            return size;
        }
    }
}
