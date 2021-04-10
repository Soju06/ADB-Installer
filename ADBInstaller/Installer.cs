using Microsoft.Win32;
using System.IO;
using System;

namespace ADBInstaller { 
    internal class Installer {
        public static ApplicationInfo AddApplication(string name) =>
            new(Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true), name);
    }

    internal class ApplicationInfo : IDisposable {
        protected readonly RegistryKey Key;

        public ApplicationInfo(RegistryKey registry, string name) {
            Key = registry.CreateSubKey(name);
        }

        public string Name {
            get => Path.GetDirectoryName(Key.Name);
        }

        public string FullName {
            get => Key.Name;
        }

        public string DisplayIcon { 
            get => Key.GetValue("DisplayIcon") as string;
            set => Key.SetValue("DisplayIcon", value);
        }

        public string DisplayName {
            get => Key.GetValue("DisplayName") as string;
            set => Key.SetValue("DisplayName", value);
        }

        public string DisplayVersion { 
            get => Key.GetValue("DisplayVersion") as string;
            set => Key.SetValue("DisplayVersion", value);
        }
        
        public string HelpLink { 
            get => Key.GetValue("HelpLink") as string;
            set => Key.SetValue("HelpLink", value);
        }
        
        public int? EstimatedSize {
            get => Key.GetValue("HelpLink") as int?;
            set => Key.SetValue("HelpLink", value);
        }

        public string InstallLocation {
            get => Key.GetValue("InstallLocation") as string;
            set => Key.SetValue("InstallLocation", value);
        }
        
        public string InstallDir { 
            get => Key.GetValue("InstallDir") as string;
            set => Key.SetValue("InstallDir", value);
        }

        public string Publisher {
            get => Key.GetValue("Publisher") as string;
            set => Key.SetValue("Publisher", value);
        }
        
        public string UninstallString {
            get => Key.GetValue("UninstallString") as string;
            set => Key.SetValue("UninstallString", value);
        }

        public string URLInfoAbout {
            get => Key.GetValue("URLInfoAbout") as string;
            set => Key.SetValue("URLInfoAbout", value);
        }
        
        public bool? NoModify {
            get => (Key.GetValue("NoModify") as int?) switch { 1 => true, 0 => false, _ => null };
            set => Key.SetValue("NoModify", value.Value ? 1 : 0);
        }

        public bool? NoRepair {
            get => (Key.GetValue("NoRepair") as int?) switch { 1 => true, 0 => false, _ => null };
            set => Key.SetValue("NoRepair", value.Value ? 1 : 0);
        }

        public void Delete() {
            Key?.Delete();
            Key?.Close();
            Key?.Dispose();
        }

        public void Dispose() {
            Key?.Close();
            Key?.Dispose();
        }
    }
}
