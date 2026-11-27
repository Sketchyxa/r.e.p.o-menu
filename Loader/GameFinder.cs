using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace REPOLoader
{
    internal static class GameFinder
    {
        public const string ProcessName = "REPO";

        public static Process FindProcess()
        {
            Process[] ps = Process.GetProcessesByName(ProcessName);
            return ps.Length > 0 ? ps[0] : null;
        }

        /// <summary>Ищет папку игры: запущенный процесс, реестр Steam, libraryfolders.vdf, соседние папки, стандартный путь.</summary>
        public static string Find()
        {
            try
            {
                Process p = FindProcess();
                if (p != null)
                {
                    string f = p.MainModule.FileName;
                    if (!string.IsNullOrEmpty(f)) return Path.GetDirectoryName(f);
                }
            }
            catch { }

            List<string> steams = new List<string>();
            try
            {
                object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null);
                if (v != null) steams.Add(v.ToString().Replace('/', '\\'));
                v = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null);
                if (v != null) steams.Add(v.ToString());
            }
            catch { }
            steams.Add(@"C:\Program Files (x86)\Steam");

            foreach (string steam in steams)
            {
                List<string> libs = new List<string>();
                libs.Add(steam);
                try
                {
                    string vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
                    if (File.Exists(vdf))
                    {
                        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                            libs.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
                    }
                }
                catch { }
                foreach (string lib in libs)
                {
                    string dir = Path.Combine(lib, @"steamapps\common\REPO");
                    if (File.Exists(Path.Combine(dir, "REPO.exe"))) return dir;
                }
            }

            try
            {
                string d = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(d); i++)
                {
                    if (File.Exists(Path.Combine(d, "REPO.exe"))) return d;
                    d = Path.GetDirectoryName(d.TrimEnd('\\'));
                }
            }
            catch { }
            return null;
        }

        public static string FindDll()
        {
            string b = AppDomain.CurrentDomain.BaseDirectory;
            string[] c =
            {
                Path.Combine(b, "REPOMenu.dll"),
                Path.GetFullPath(Path.Combine(b, @"..\bin\Release\REPOMenu.dll")),
                Path.GetFullPath(Path.Combine(b, @"..\..\bin\Release\REPOMenu.dll")),
                Path.GetFullPath(Path.Combine(b, @"..\dist\REPOMenu.dll")),
            };
            foreach (string p in c) if (File.Exists(p)) return p;
            return null;
        }

        public static bool MonoLoaded(Process p)
        {
            try
            {
                foreach (ProcessModule m in p.Modules)
                    if (m.ModuleName.StartsWith("mono-2.0", StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { }
            return false;
        }
    }
}
