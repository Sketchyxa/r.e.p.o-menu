using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace r.e.p.o_cheat
{
    /// <summary>Настройки меню. Публичные поля (bool/int/float/KeyCode) сохраняются в профиль автоматически.</summary>
    internal sealed class Cfg
    {
        // self
        public bool god;
        public bool stamina;
        public bool noclip;
        public float noclipSpeed = 8f;
        public float speedMul = 1f;
        public float strengthMul = 1f;
        public float jumpMul = 1f;
        public float gravityMul = 1f;
        public float grabRangeMul = 1f;
        public bool noFog;
        public bool fovOn;
        public float fov = 90f;

        // esp
        public bool espEnemies;
        public bool espItems;
        public bool espPlayers;
        public bool espExtraction;
        public bool chams;
        public bool espBoxes = true;
        public bool espHealth = true;
        public bool espDist = true;
        public bool espTraps;
        public float espMinValue;
        public float espMaxDist = 150f;

        // extra
        public bool noTumble;
        public bool freeze;
        public bool killAura;
        public float killRadius = 15f;
        public bool autoPack;
        public bool freeShop;

        // items
        public float pullRange = 60f;
        public bool pullValuables = true;
        public bool pullItems;

        // visual
        public bool nightVision;
        public float nvBrightness = 1.6f;
        public bool nvLight;
        public float nvRange = 35f;
        public float nvPower = 1.2f;
        public float fcSpeed = 10f;
        public float fcSens = 1f;

        // tools / evac
        public bool infBattery;
        public bool autoEvac;
        public float autoEvacTarget = 20000f;
        public bool autoEvacActivate = true;

        // esp colors (RGB 0..1)
        public float colEnemyR = 0.93f, colEnemyG = 0.31f, colEnemyB = 0.24f;
        public float colValuableR = 1f, colValuableG = 0.8f, colValuableB = 0.25f;
        public float colItemR = 0.4f, colItemG = 0.75f, colItemB = 1f;
        public float colPlayerR = 0.3f, colPlayerG = 0.85f, colPlayerB = 0.55f;
        public float colExtractR = 0.75f, colExtractG = 0.45f, colExtractB = 1f;
        public float colTrapR = 1f, colTrapG = 0.55f, colTrapB = 0.15f;

        // radar / info
        public bool radar;
        public float radarRange = 40f;
        public bool hudLoot = true;

        // ui
        public bool hud = true;
        public KeyCode menuKey = KeyCode.Insert;

        public Dictionary<string, KeyCode> hotkeys = new Dictionary<string, KeyCode>();

        public static string Profile = "default";

        private static string Dir { get { return Application.persistentDataPath; } }

        public static string PathFor(string profile)
        {
            return System.IO.Path.Combine(Dir, "REPOMenu." + profile + ".cfg");
        }

        public static string Path { get { return PathFor(Profile); } }

        public static List<string> Profiles()
        {
            List<string> r = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(Dir, "REPOMenu.*.cfg"))
                {
                    string n = System.IO.Path.GetFileName(f);
                    n = n.Substring("REPOMenu.".Length);
                    n = n.Substring(0, n.Length - ".cfg".Length);
                    if (n.Length > 0) r.Add(n);
                }
            }
            catch { }
            r.Sort();
            return r;
        }

        public static string Sanitize(string n)
        {
            if (string.IsNullOrEmpty(n)) return "default";
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) n = n.Replace(c.ToString(), "");
            n = n.Replace(".", "").Trim();
            return n.Length == 0 ? "default" : n;
        }

        public void Save()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (FieldInfo f in typeof(Cfg).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType == typeof(bool) || f.FieldType == typeof(int) || f.FieldType == typeof(float) || f.FieldType == typeof(KeyCode))
                        lines.Add(f.Name + "=" + Convert.ToString(f.GetValue(this), CultureInfo.InvariantCulture));
                }
                foreach (KeyValuePair<string, KeyCode> kv in hotkeys) lines.Add("hk." + kv.Key + "=" + kv.Value);
                File.WriteAllLines(Path, lines.ToArray());
                Log.Add("Профиль сохранён: " + Profile);
            }
            catch (Exception e) { Log.Add("Save: " + e.Message); }
        }

        public bool Load()
        {
            try
            {
                if (!File.Exists(Path)) { Log.Add("Профиля нет: " + Profile); return false; }
                foreach (string line in File.ReadAllLines(Path))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim();
                    string v = line.Substring(i + 1).Trim();
                    if (k.StartsWith("hk."))
                    {
                        try { hotkeys[k.Substring(3)] = (KeyCode)Enum.Parse(typeof(KeyCode), v); } catch { }
                        continue;
                    }
                    FieldInfo f = typeof(Cfg).GetField(k, BindingFlags.Public | BindingFlags.Instance);
                    if (f == null) continue;
                    try
                    {
                        if (f.FieldType == typeof(KeyCode)) f.SetValue(this, Enum.Parse(typeof(KeyCode), v));
                        else f.SetValue(this, Convert.ChangeType(v, f.FieldType, CultureInfo.InvariantCulture));
                    }
                    catch { }
                }
                Log.Add("Профиль загружен: " + Profile);
                return true;
            }
            catch (Exception e) { Log.Add("Load: " + e.Message); return false; }
        }

        public static void Delete(string profile)
        {
            try { File.Delete(PathFor(profile)); Log.Add("Профиль удалён: " + profile); }
            catch (Exception e) { Log.Add("Delete: " + e.Message); }
        }
    }

    internal sealed class HotkeyAction
    {
        public string Name;
        public KeyCode Default;
        public Action Run;
    }
}
