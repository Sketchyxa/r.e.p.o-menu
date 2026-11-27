using System;
using UnityEngine;

namespace r.e.p.o_cheat
{
    /// <summary>Точка входа для инжектора: r.e.p.o_cheat.Loader.Init / UnloadCheat</summary>
    public class Loader
    {
        private const string Name = "REPOMenu";
        private static GameObject holder;

        private static GameObject FindExisting()
        {
            // объект скрыт (HideAndDontSave), обычный Find его не видит; ищем среди всех загруженных.
            // Нужно, потому что каждый инжект грузит новую копию сборки со своим статическим holder.
            foreach (GameObject g in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (g != null && g.name == Name && g.transform.parent == null) return g;
            }
            return null;
        }

        public static void Init()
        {
            try
            {
                if (holder != null || FindExisting() != null) return;
                try { System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "REPOMenu_loaded.txt"), DateTime.Now.ToString("o")); } catch { }
                holder = new GameObject(Name);
                holder.AddComponent<Menu>();
                UnityEngine.Object.DontDestroyOnLoad(holder);
                holder.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception e)
            {
                Debug.Log("[REPOMenu] Init: " + e);
            }
        }

        /// <summary>F10 или повторный инжект с -m UnloadCheat: откатывает изменения и удаляет объект меню.</summary>
        public static void UnloadCheat()
        {
            try
            {
                GameObject g = holder != null ? holder : FindExisting();
                if (g != null) UnityEngine.Object.Destroy(g);
                holder = null;
            }
            catch (Exception e)
            {
                Debug.Log("[REPOMenu] Unload: " + e);
            }
        }
    }
}
