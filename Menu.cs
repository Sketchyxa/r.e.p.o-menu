using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public sealed partial class Menu : MonoBehaviour
    {
        internal Cfg cfg = new Cfg();

        private bool open = true;
        private Rect win = new Rect(40f, 40f, 980f, 660f);
        private int tab;
        private readonly Vector2[] scrolls = new Vector2[10];

        private readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        private readonly List<HotkeyAction> actions = new List<HotkeyAction>();
        private string rebind;
        private Texture2D white;
        private bool dragging;
        private Vector2 dragOff;

        private sealed class Toast { public string Text; public float Until; public bool On; public bool Plain; }
        private readonly List<Toast> toasts = new List<Toast>();

        // ---------- жизненный цикл ----------
        private void Awake()
        {
            cfg.Load();
            BuildActions();
            Camera.onPreCull += OnPreCullCb;
            white = Texture2D.whiteTexture;
            Ui.SetAnim("win", 0f);
            Log.Add("Меню загружено. Insert - показать/скрыть, F10 - выгрузить.");
        }

        private void OnDestroy()
        {
            try { CleanupSelf(); } catch (Exception e) { Log.Add("CleanupSelf: " + e.Message); }
            try { RestoreChams(); } catch (Exception e) { Log.Add("RestoreChams: " + e.Message); }
            try { Ui.Destroy(); } catch { }
            try { Camera.onPreCull -= OnPreCullCb; } catch { }
        }

        private void Update()
        {
            try
            {
                if (open) KeepCursor();
                ApplySelf();
                ExtrasTick();
                FreecamTick();
                BatteryTick();
                RefreshEntities();
                ApplyChams();
            }
            catch (Exception e) { Log.Add("Update: " + e.Message); }
        }

        private void LateUpdate()
        {
            try { if (open) KeepCursor(); ApplyVisuals(); }
            catch (Exception e) { Log.Add("LateUpdate: " + e.Message); }
        }

        private void KeepCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            object im = R.GetS("InputManager", "instance");
            R.Call(im, "DisableMovement", 0.1f);
            R.Call(im, "DisableAiming", 0.1f);
            object cm = R.GetS("CursorManager", "instance");
            R.Set(cm, "unlockTimer", 0.1f);
        }

        // ---------- хоткеи ----------
        private void Flip(string name, bool on)
        {
            Toast t = new Toast();
            t.Text = name;
            t.On = on;
            t.Until = Time.unscaledTime + 2f;
            toasts.Add(t);
            if (toasts.Count > 5) toasts.RemoveAt(0);
        }

        private void Note(string text)
        {
            Toast t = new Toast();
            t.Text = text;
            t.Plain = true;
            t.Until = Time.unscaledTime + 2.5f;
            toasts.Add(t);
            if (toasts.Count > 5) toasts.RemoveAt(0);
        }

        private void BuildActions()
        {
            Add("God mode", KeyCode.F1, delegate { cfg.god = !cfg.god; Flip("God mode", cfg.god); });
            Add("Infinite stamina", KeyCode.F2, delegate { cfg.stamina = !cfg.stamina; Flip("Infinite stamina", cfg.stamina); });
            Add("Noclip", KeyCode.F3, delegate { cfg.noclip = !cfg.noclip; Flip("Noclip", cfg.noclip); });
            Add("No fog", KeyCode.F4, delegate { cfg.noFog = !cfg.noFog; Flip("No fog", cfg.noFog); });
            Add("ESP enemies", KeyCode.F5, delegate { cfg.espEnemies = !cfg.espEnemies; Flip("ESP enemies", cfg.espEnemies); });
            Add("ESP items", KeyCode.F6, delegate { cfg.espItems = !cfg.espItems; Flip("ESP items", cfg.espItems); });
            Add("ESP players", KeyCode.F7, delegate { cfg.espPlayers = !cfg.espPlayers; Flip("ESP players", cfg.espPlayers); });
            Add("Chams", KeyCode.F8, delegate { cfg.chams = !cfg.chams; Flip("Chams", cfg.chams); });
            Add("ESP extraction", KeyCode.None, delegate { cfg.espExtraction = !cfg.espExtraction; Flip("ESP extraction", cfg.espExtraction); });
            Add("Pull items to me", KeyCode.F9, delegate { PullItems(); Note(status); });
            Add("Pack to extraction", KeyCode.None, delegate { PackToExtraction(); Note(status); });
            Add("Remote sell", KeyCode.None, delegate { RemoteSell(); Note(status); });
            Add("Set value on grabbed", KeyCode.None, delegate { SetValueGrabbed(); Note(status); });
            Add("Full heal (me)", KeyCode.None, delegate { FullHeal(); });
            Add("Refill stamina", KeyCode.None, delegate { RefillStamina(); });
            Add("Radar", KeyCode.None, delegate { cfg.radar = !cfg.radar; Flip("Radar", cfg.radar); });
            Add("Save waypoint 1", KeyCode.None, delegate { SaveWp(0); });
            Add("Go to waypoint 1", KeyCode.None, delegate { GoWp(0); });
            Add("Teleport to cursor", KeyCode.None, TpCursor);
            Add("TP to nearest valuable", KeyCode.None, TpNearestValuable);
            Add("Activate extraction", KeyCode.None, ActivateExtraction);
            Add("Freeze enemies", KeyCode.None, delegate { cfg.freeze = !cfg.freeze; Flip("Freeze enemies", cfg.freeze); });
            Add("Kill aura", KeyCode.None, delegate { cfg.killAura = !cfg.killAura; Flip("Kill aura", cfg.killAura); });
            Add("No tumble", KeyCode.None, delegate { cfg.noTumble = !cfg.noTumble; Flip("No tumble", cfg.noTumble); });
            Add("Kill all enemies", KeyCode.None, KillAllEnemies);
            Add("Freecam", KeyCode.None, delegate { SetFreecam(!freecam); });
            Add("Night vision", KeyCode.None, delegate { cfg.nightVision = !cfg.nightVision; Flip("Night vision", cfg.nightVision); });
            Add("Infinite batteries", KeyCode.None, delegate { cfg.infBattery = !cfg.infBattery; Flip("Infinite batteries", cfg.infBattery); });
            Add("Auto evacuation", KeyCode.None, delegate { cfg.autoEvac = !cfg.autoEvac; Flip("Auto evacuation", cfg.autoEvac); });
            Add("HUD", KeyCode.None, delegate { cfg.hud = !cfg.hud; });
        }

        private void Add(string name, KeyCode def, Action run)
        {
            HotkeyAction a = new HotkeyAction();
            a.Name = name;
            a.Default = def;
            a.Run = run;
            actions.Add(a);
            if (!cfg.hotkeys.ContainsKey(name)) cfg.hotkeys[name] = def;
        }

        private bool Held(KeyCode k) { return held.Contains(k); }

        private void HandleKey(Event e)
        {
            KeyCode k = e.keyCode;
            if (rebind != null)
            {
                if (k != KeyCode.Escape)
                {
                    if (rebind == "\u0001menu") cfg.menuKey = k;
                    else cfg.hotkeys[rebind] = k;
                }
                rebind = null;
                e.Use();
                return;
            }
            if (held.Contains(k)) return;
            held.Add(k);

            if (k == KeyCode.F10) { Loader.UnloadCheat(); return; }
            if (k == cfg.menuKey) { open = !open; return; }
            if (open && GUIUtility.keyboardControl != 0) return;

            foreach (HotkeyAction a in actions)
            {
                KeyCode hk;
                if (cfg.hotkeys.TryGetValue(a.Name, out hk) && hk != KeyCode.None && hk == k)
                {
                    try { a.Run(); } catch (Exception ex) { Log.Add(a.Name + ": " + ex.Message); }
                }
            }
        }

        // ---------- IMGUI ----------
        private void OnGUI()
        {
            Event e = Event.current;
            if (open && e.type == EventType.Repaint) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (freecam && (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)) { fcEvX += e.delta.x; fcEvY += e.delta.y; }
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None) HandleKey(e);
            else if (e.type == EventType.KeyUp) held.Remove(e.keyCode);

            try
            {
                Ui.Init();
                if (e.type == EventType.Repaint) { DrawEsp(); if (cfg.radar) DrawRadar(); }
                if (cfg.hud && e.type == EventType.Repaint) DrawHud();
                DrawMain();
            }
            catch (Exception ex) { Log.Add("OnGUI: " + ex.Message); }
        }

        private static readonly string[] NavCodes = { "PL", "ES", "VI", "IT", "EX", "HO", "KY", "PF", "LG" };
        private static readonly string[] NavTitles = { "Игрок", "ESP", "Визуал", "Предметы", "Экстра", "Хост", "Клавиши", "Профили", "Журнал" };
        private static readonly string[] NavSubs =
        {
            "Выживание, обзор и параметры персонажа",
            "Подсветка врагов, ценностей, игроков и ловушек",
            "Freecam, ночное зрение и цвета ESP",
            "Цена ценностей, перемещение и спавнер",
            "Телепорт, бой, эвакуация, экономика, радар",
            "Действия над игроками и спавн мобов",
            "Назначение горячих клавиш",
            "Сохранение и загрузка настроек",
            "Что происходит под капотом",
        };
        private int lastTab = -1;
        private float contentW = 700f;

        private void DrawMain()
        {
            Event e = Event.current;
            float a = Ui.Anim("win", open ? 1f : 0f, 12f);
            if (a < 0.02f) return;

            GUISkin prevSkin = GUI.skin;
            Matrix4x4 prevMat = GUI.matrix;
            Color prevColor = GUI.color;
            GUI.skin = Ui.Skin;
            GUI.matrix = Matrix4x4.TRS(new Vector3(0f, (1f - a) * 18f, 0f), Quaternion.identity, Vector3.one);
            Ui.Alpha = a;
            GUI.color = new Color(1f, 1f, 1f, a);

            if (e.type == EventType.Repaint) Ui.Tip = "";
            Rect w = win;
            float sbW = 210f;

            // фон окна и боковая панель
            Ui.Round(w, Ui.Bg, 16);
            Rect sb = new Rect(w.x, w.y, sbW, w.height);
            Ui.Round(sb, Ui.Side, 16);
            Ui.Fill(new Rect(sb.xMax - 16f, sb.y, 16f, sb.height), Ui.Side);
            Ui.Fill(new Rect(sb.xMax, sb.y + 1f, 1f, sb.height - 2f), Ui.Border);
            Ui.Round(w, Ui.Border, 16, true);

            // перетаскивание за верхнюю полосу
            Rect drag = new Rect(w.x, w.y, w.width, 60f);
            if (e.type == EventType.MouseDown && e.button == 0 && drag.Contains(e.mousePosition)) { dragging = true; dragOff = e.mousePosition - new Vector2(win.x, win.y); e.Use(); }
            else if (e.type == EventType.MouseDrag && dragging) { win.x = e.mousePosition.x - dragOff.x; win.y = e.mousePosition.y - dragOff.y; e.Use(); }
            else if (e.type == EventType.MouseUp) dragging = false;

            // логотип
            GUI.Label(new Rect(w.x + 22f, w.y + 16f, sbW - 30f, 30f), "<color=#ED4F3D>/</color>  R.E.P.O", Ui.Big);
            Ui.Txt(new Rect(w.x + 24f, w.y + 44f, sbW - 30f, 18f), "M E N U", Ui.DimLabel, Ui.Dim);

            // навигация
            for (int i = 0; i < NavTitles.Length; i++)
            {
                Rect nr = new Rect(w.x + 10f, w.y + 86f + i * 46f, sbW - 20f, 42f);
                if (Ui.Nav(nr, NavCodes[i], NavTitles[i], tab == i)) tab = i;
            }
            bool host = R.IsHost();
            Ui.Chip(new Rect(w.x + 16f, w.yMax - 74f, sbW - 32f, 24f), host ? "HOST / SOLO" : "CLIENT", host ? Ui.Green : Ui.Dim);
            Ui.Txt(new Rect(w.x + 16f, w.yMax - 44f, sbW - 32f, 16f), "Insert  скрыть меню", Ui.DimLabel, Ui.Dim);
            Ui.Txt(new Rect(w.x + 16f, w.yMax - 26f, sbW - 32f, 16f), "F10  выгрузить", Ui.DimLabel, Ui.Dim);

            if (tab != lastTab) { lastTab = tab; Ui.SetAnim("tab", 0f); }
            float ta = Ui.Anim("tab", 1f, 12f);

            // шапка вкладки
            float cx = w.x + sbW + 24f;
            float cw = w.width - sbW - 48f;
            contentW = cw;
            Ui.Alpha = a * ta;
            GUI.Label(new Rect(cx, w.y + 16f, cw - 200f, 30f), NavTitles[tab], Ui.Big);
            Ui.Txt(new Rect(cx + 1f, w.y + 46f, cw - 200f, 18f), NavSubs[tab], Ui.DimLabel, Ui.Dim);
            Ui.Chip(new Rect(w.xMax - 24f - 140f, w.y + 22f, 140f, 26f), "профиль: " + Cfg.Profile, Ui.Dim);
            Ui.Fill(new Rect(cx, w.y + 76f, cw, 1f), Ui.Border);

            // содержимое
            GUI.color = new Color(1f, 1f, 1f, a * ta);
            GUILayout.BeginArea(new Rect(cx, w.y + 84f, cw, w.height - 84f - 56f));
            scrolls[tab] = GUILayout.BeginScrollView(scrolls[tab]);
            switch (tab)
            {
                case 0: TabSelf(); break;
                case 1: TabEsp(); break;
                case 2: TabVisual(); break;
                case 3: TabItems(); break;
                case 4: TabExtra(); break;
                case 5: TabHost(); break;
                case 6: TabKeys(); break;
                case 7: TabProfiles(); break;
                default: TabLog(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // нижняя строка: подсказка / статус
            Ui.Alpha = a;
            GUI.color = new Color(1f, 1f, 1f, a);
            Rect foot = new Rect(cx, w.yMax - 46f, cw, 32f);
            Ui.Round(foot, Ui.Panel, 9);
            string text = Ui.Tip.Length > 0 ? Ui.Tip : (status.Length > 0 ? status : "Наведи курсор на элемент, чтобы увидеть подсказку");
            Ui.Txt(new Rect(foot.x + 14f, foot.y + 7f, foot.width - 28f, 20f), text, Ui.Tip.Length > 0 ? Ui.Label : Ui.DimLabel, Ui.Tip.Length > 0 ? Ui.Text : Ui.Dim);

            GUI.matrix = prevMat;
            GUI.skin = prevSkin;
            GUI.color = prevColor;
            Ui.Alpha = 1f;
        }

        // ---------- HUD ----------
        private void AddHud(List<string> l, bool on, string s) { if (on) l.Add(s); }

        private void DrawHud()
        {
            List<string> l = new List<string>();
            if (cfg.hudLoot) { UpdateLoot(); if (lootCount >= 0) { l.Add("LOOT " + lootCount + "  $" + lootTotal); if (haulGoal > 0) l.Add("HAUL " + haulCur + " / " + haulGoal); } }
            AddHud(l, cfg.god, "GOD MODE");
            AddHud(l, cfg.stamina, "STAMINA");
            AddHud(l, cfg.noclip, "NOCLIP");
            AddHud(l, cfg.noFog, "NO FOG");
            AddHud(l, Mathf.Abs(cfg.speedMul - 1f) > 0.01f, "SPEED x" + cfg.speedMul.ToString("0.0"));
            AddHud(l, Mathf.Abs(cfg.jumpMul - 1f) > 0.01f, "JUMP x" + cfg.jumpMul.ToString("0.0"));
            AddHud(l, cfg.strengthMul > 1.01f, "GRAB x" + cfg.strengthMul.ToString("0.0"));
            AddHud(l, cfg.fovOn, "FOV " + Mathf.RoundToInt(cfg.fov));
            AddHud(l, cfg.espEnemies || cfg.espItems || cfg.espPlayers || cfg.espExtraction, "ESP");
            AddHud(l, cfg.chams, "CHAMS");
            AddHud(l, cfg.noTumble, "NO TUMBLE");
            AddHud(l, cfg.freeze, "FREEZE");
            AddHud(l, cfg.killAura, "KILL AURA");
            AddHud(l, cfg.autoPack, "AUTO PACK");
            AddHud(l, cfg.freeShop, "FREE SHOP");

            for (int i = toasts.Count - 1; i >= 0; i--) if (toasts[i].Until < Time.unscaledTime) toasts.RemoveAt(i);
            if (l.Count == 0 && toasts.Count == 0) return;

            float wd = 190f;
            float x = Screen.width - wd - 14f;
            float y = 14f;
            float h = 26f + l.Count * 17f + (toasts.Count > 0 ? 8f + toasts.Count * 17f : 0f);
            Ui.Round(new Rect(x, y, wd, h), new Color(0.055f, 0.055f, 0.066f, 0.86f), 10);
            Ui.Round(new Rect(x, y, wd, h), Ui.Border, 10, true);
            Ui.Round(new Rect(x + 1f, y + 10f, 3f, h - 20f), Ui.Accent, 2);
            GUIStyle s = Ui.Small;
            s.alignment = TextAnchor.UpperLeft;
            Ui.Shadow(new Rect(x + 12f, y + 5f, wd, 18f), "/ R.E.P.O", Ui.Accent, s);
            float cy = y + 24f;
            foreach (string t in l) { Ui.Shadow(new Rect(x + 12f, cy, wd, 16f), t, Ui.Green, s); cy += 17f; }
            if (toasts.Count > 0)
            {
                cy += 8f;
                foreach (Toast t in toasts) { Ui.Shadow(new Rect(x + 12f, cy, wd - 12f, 16f), t.Plain ? t.Text : t.Text + (t.On ? "  ON" : "  OFF"), (t.Plain || t.On) ? Ui.Text : Ui.Dim, s); cy += 17f; }
            }
            s.alignment = TextAnchor.UpperCenter;
        }
    }
}
