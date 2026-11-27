using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public sealed partial class Menu
    {
        private const int KindEnemy = 0;
        private const int KindItem = 1;
        private const int KindPlayer = 2;
        private const int KindExtraction = 3;
        private const int KindTrap = 4;

        private sealed class Ent
        {
            public int Kind;
            public string Label;
            public Transform Tr;
            public GameObject Root;
            public Color Col;
            public float Value;
            public bool Box;
            public float Hp = -1f;
            public float MaxHp = 1f;
        }

        private readonly List<Ent> ents = new List<Ent>();
        private float nextRefresh;
        private bool chamLogged;

        private readonly Dictionary<Renderer, Material[]> chamOrig = new Dictionary<Renderer, Material[]>();
        private readonly Dictionary<int, Material> chamMats = new Dictionary<int, Material>();

        private static string Clean(string n)
        {
            if (string.IsNullOrEmpty(n)) return "?";
            n = n.Replace("(Clone)", "").Trim();
            string[] pre = { "Valuable ", "Enemy - ", "Item " };
            foreach (string p in pre) if (n.StartsWith(p)) n = n.Substring(p.Length);
            return n;
        }

        private bool KindEnabled(int kind)
        {
            switch (kind)
            {
                case KindEnemy: return cfg.espEnemies;
                case KindItem: return cfg.espItems;
                case KindPlayer: return cfg.espPlayers;
                case KindTrap: return cfg.espTraps;
                default: return cfg.espExtraction;
            }
        }

        private void RefreshEntities()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            ents.Clear();
            if (!cfg.espEnemies && !cfg.espItems && !cfg.espPlayers && !cfg.espExtraction && !cfg.espTraps && !cfg.radar) return;

            if (cfg.espEnemies || cfg.radar) CollectEnemies();
            if (cfg.espItems) CollectItems();
            if (cfg.espPlayers || cfg.radar) CollectPlayers();
            if (cfg.espExtraction || cfg.radar) CollectExtraction();
            if (cfg.espTraps) CollectTraps();
        }

        private void CollectEnemies()
        {
            object dir = R.GetS("EnemyDirector", "instance");
            foreach (object ep in R.List(R.Get(dir, "enemiesSpawned")))
            {
                if (!R.IsAlive(ep)) continue;
                if (!R.B(ep, "Spawned", true)) continue;
                object en = R.Get(ep, "Enemy");
                Transform tr = R.Get(en, "CenterTransform") as Transform;
                if (tr == null)
                {
                    Component c = en as Component;
                    if (c != null) tr = c.transform;
                }
                if (tr == null) continue;
                Component epc = ep as Component;
                Ent e = new Ent();
                e.Kind = KindEnemy;
                e.Tr = tr;
                e.Root = epc != null ? epc.gameObject : tr.gameObject;
                e.Col = EspCol(0);
                e.Box = true;
                object hp = R.Get(en, "Health");
                int h = R.I(hp, "healthCurrent", -1);
                e.Label = Clean(R.S(ep, "enemyName"));
                if (h >= 0) { e.Hp = h; e.MaxHp = Mathf.Max(1, R.I(hp, "health", h)); }
                ents.Add(e);
            }
        }

        private void CollectItems()
        {
            foreach (UnityEngine.Object o in R.FindAll("ValuableObject"))
            {
                Component c = o as Component;
                if (c == null) continue;
                float v = R.F(o, "dollarValueCurrent");
                if (v < cfg.espMinValue) continue;
                Ent e = new Ent();
                e.Kind = KindItem;
                e.Tr = c.transform;
                e.Root = c.gameObject;
                e.Value = v;
                e.Col = EspCol(1);
                e.Label = Clean(c.name);
                ents.Add(e);
            }
            foreach (UnityEngine.Object o in R.FindAll("ItemAttributes"))
            {
                Component c = o as Component;
                if (c == null) continue;
                float v = R.I(o, "value");
                if (v < cfg.espMinValue) continue;
                string nm = R.S(o, "itemName");
                Ent e = new Ent();
                e.Kind = KindItem;
                e.Tr = c.transform;
                e.Root = c.gameObject;
                e.Value = v;
                e.Col = EspCol(2);
                e.Label = Clean(string.IsNullOrEmpty(nm) ? c.name : nm);
                ents.Add(e);
            }
        }

        private void CollectPlayers()
        {
            foreach (object a in R.List(R.CallS("SemiFunc", "PlayerGetAll")))
            {
                if (!R.IsAlive(a) || R.B(a, "isLocal")) continue;
                Transform tr = R.Get(a, "playerTransform") as Transform;
                if (tr == null) continue;
                object ph = R.Get(a, "playerHealth");
                bool dead = R.B(a, "deadSet");
                Ent e = new Ent();
                e.Kind = KindPlayer;
                e.Tr = tr;
                e.Root = (a as Component) != null ? ((Component)a).gameObject : tr.gameObject;
                e.Col = dead ? Ui.Dim : EspCol(3);
                e.Box = true;
                e.Label = (R.S(a, "playerName") ?? "player") + (dead ? " (dead)" : "");
                if (!dead) { e.Hp = R.I(ph, "health"); e.MaxHp = Mathf.Max(1, R.I(ph, "maxHealth", 100)); }
                ents.Add(e);
            }
        }

        private void CollectTraps()
        {
            foreach (UnityEngine.Object o in R.FindAll("Trap"))
            {
                Component c = o as Component;
                if (c == null) continue;
                Ent e = new Ent();
                e.Kind = KindTrap;
                e.Tr = c.transform;
                e.Root = c.gameObject;
                e.Col = EspCol(5);
                e.Label = "TRAP " + Clean(c.name) + (R.B(o, "trapTriggered") ? " (used)" : "");
                ents.Add(e);
            }
        }

        private void CollectExtraction()
        {
            object rd = R.GetS("RoundDirector", "instance");
            Type ept = R.T("ExtractionPoint");
            foreach (object g in R.List(R.Get(rd, "extractionPointList")))
            {
                GameObject go = g as GameObject;
                if (go == null) continue;
                string state = "";
                if (ept != null)
                {
                    Component ep = go.GetComponent(ept);
                    if (ep != null)
                    {
                        object st = R.Get(ep, "currentState");
                        state = st == null ? "" : " [" + st + "]";
                    }
                }
                Ent e = new Ent();
                e.Kind = KindExtraction;
                e.Tr = go.transform;
                e.Root = go;
                e.Col = EspCol(4);
                e.Label = "EXTRACTION" + state;
                ents.Add(e);
            }
        }

        // ---------- отрисовка ----------
        private Camera GetCam()
        {
            Camera c = Camera.main;
            if (c != null && c.enabled) return c;
            Camera best = null;
            foreach (Camera x in Camera.allCameras)
            {
                if (x.enabled && (best == null || x.depth > best.depth)) best = x;
            }
            return best;
        }

        /// <summary>Мир -> пиксели экрана (Y сверху). Через viewport: игра рендерит в текстуру другого размера, и WorldToScreenPoint давал координаты не в масштабе экрана.</summary>
        private static Vector3 W2S(Camera cam, Vector3 p)
        {
            Vector3 v = cam.WorldToViewportPoint(p);
            return new Vector3(v.x * Screen.width, (1f - v.y) * Screen.height, v.z);
        }

        private int espBudget;

        private static Vector3 EntPos(Ent e)
        {
            if (e.Kind == KindItem && e.Root != null)
            {
                Collider c = e.Root.GetComponentInChildren<Collider>();
                if (c != null) return c.bounds.center;
            }
            return e.Tr.position;
        }

        private void Corners(float l, float t, float w, float h, Color c)
        {
            float cl = Mathf.Max(4f, Mathf.Min(w, h) * 0.28f);
            Ui.Fill(new Rect(l, t, cl, 1f), c); Ui.Fill(new Rect(l, t, 1f, cl), c);
            Ui.Fill(new Rect(l + w - cl, t, cl, 1f), c); Ui.Fill(new Rect(l + w - 1f, t, 1f, cl), c);
            Ui.Fill(new Rect(l, t + h - 1f, cl, 1f), c); Ui.Fill(new Rect(l, t + h - cl, 1f, cl), c);
            Ui.Fill(new Rect(l + w - cl, t + h - 1f, cl, 1f), c); Ui.Fill(new Rect(l + w - 1f, t + h - cl, 1f, cl), c);
        }

        private void DrawEsp()
        {
            if (ents.Count == 0) return;
            Camera cam = GetCam();
            if (cam == null) return;
            Vector3 me = cam.transform.position;
            Color old = GUI.color;
            GUIStyle st = Ui.Small;
            st.fontSize = 11;

            espBudget = 0;
            foreach (Ent e in ents)
            {
                if (!KindEnabled(e.Kind) || e.Tr == null) continue;
                if (espBudget >= 60) break;
                Vector3 p = EntPos(e);
                float dist = Vector3.Distance(me, p);
                if (dist > cfg.espMaxDist) continue;
                Vector3 s = W2S(cam, p);
                if (s.z <= 0.1f) continue;
                float x = s.x;
                float y = s.y;
                if (x < -50f || x > Screen.width + 50f || y < -50f || y > Screen.height + 50f) continue;
                espBudget++;
                float top = y - 8f;
                float bottom = y + 8f;

                if (e.Box)
                {
                    Vector3 hi = W2S(cam, p + Vector3.up * 1f);
                    Vector3 lo = W2S(cam, p - Vector3.up * 1f);
                    float h = Mathf.Max(14f, Mathf.Abs(hi.y - lo.y));
                    float w = h * 0.5f;
                    float l = x - w / 2f;
                    float t = y - h / 2f;
                    if (cfg.espBoxes)
                    {
                        Ui.Outline(new Rect(l - 1f, t - 1f, w + 2f, h + 2f), new Color(0f, 0f, 0f, 0.55f));
                        Corners(l, t, w, h, e.Col);
                    }
                    if (cfg.espHealth && e.Hp >= 0f)
                    {
                        float f = Mathf.Clamp01(e.Hp / e.MaxHp);
                        Ui.Fill(new Rect(l - 6f, t - 1f, 4f, h + 2f), new Color(0f, 0f, 0f, 0.6f));
                        Ui.Fill(new Rect(l - 5f, t + h * (1f - f), 2f, h * f), Ui.HpCol(f));
                    }
                    top = t - 15f;
                    bottom = t + h + 2f;
                }
                else
                {
                    Rect m = new Rect(x - 4f, y - 4f, 8f, 8f);
                    Ui.Fill(new Rect(m.x - 1f, m.y - 1f, 10f, 10f), new Color(0f, 0f, 0f, 0.6f));
                    Ui.Fill(m, e.Col);
                    top = y - 22f;
                    bottom = y + 6f;
                }

                Ui.Shadow(new Rect(x - 150f, top, 300f, 16f), e.Label, e.Col, st);
                string sub = "";
                if (cfg.espDist) sub += Mathf.RoundToInt(dist) + "m";
                if (e.Hp >= 0f && !cfg.espHealth) sub += "  " + Mathf.RoundToInt(e.Hp) + "hp";
                if (sub.Length > 0) Ui.Shadow(new Rect(x - 150f, bottom, 300f, 16f), sub, Ui.Dim, st);
                if (e.Kind == KindItem && e.Value > 0f)
                    Ui.Shadow(new Rect(x - 150f, bottom + (sub.Length > 0 ? 12f : 0f), 300f, 16f), "$" + Mathf.RoundToInt(e.Value), Ui.Gold, st);
            }
            GUI.color = old;
        }

        // ---------- chams ----------
        private Material ChamMat(Color c)
        {
            int key = ((int)(c.r * 255f) << 16) | ((int)(c.g * 255f) << 8) | (int)(c.b * 255f);
            Material m;
            if (chamMats.TryGetValue(key, out m) && m != null) return m;
            Shader sh = Shader.Find("Hidden/Internal-Colored");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) { Log.Add("Chams: шейдер не найден"); return null; }
            if (!chamLogged) { Log.Add("Chams шейдер: " + sh.name); chamLogged = true; }
            m = new Material(sh);
            m.hideFlags = HideFlags.HideAndDontSave;
            m.SetColor("_Color", c);
            m.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            m.SetInt("_ZWrite", 0);
            m.SetInt("_Cull", 0);
            m.renderQueue = 4000;
            chamMats[key] = m;
            return m;
        }

        private void ApplyChams()
        {
            if (!cfg.chams) { if (chamOrig.Count > 0) RestoreChams(); return; }
            HashSet<Renderer> wanted = new HashSet<Renderer>();
            foreach (Ent e in ents)
            {
                if (!KindEnabled(e.Kind) || e.Kind == KindExtraction || e.Kind == KindTrap || e.Root == null) continue;
                Material m = ChamMat(e.Col);
                if (m == null) continue;
                foreach (Renderer r in e.Root.GetComponentsInChildren<Renderer>())
                {
                    if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                    wanted.Add(r);
                    if (chamOrig.ContainsKey(r)) continue;
                    Material[] src = r.sharedMaterials;
                    chamOrig[r] = src;
                    Material[] arr = new Material[src.Length];
                    for (int i = 0; i < arr.Length; i++) arr[i] = m;
                    r.sharedMaterials = arr;
                }
            }
            List<Renderer> drop = new List<Renderer>();
            foreach (KeyValuePair<Renderer, Material[]> kv in chamOrig)
            {
                if (kv.Key == null) { drop.Add(kv.Key); continue; }
                if (!wanted.Contains(kv.Key)) { kv.Key.sharedMaterials = kv.Value; drop.Add(kv.Key); }
            }
            foreach (Renderer r in drop) chamOrig.Remove(r);
        }

        private void RestoreChams()
        {
            foreach (KeyValuePair<Renderer, Material[]> kv in chamOrig)
            {
                if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            }
            chamOrig.Clear();
        }
    }
}
