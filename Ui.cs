using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    /// <summary>Современная тёмная тема: скруглённые 9-slice текстуры, анимации, свои переключатели и слайдеры поверх IMGUI.</summary>
    internal static class Ui
    {
        public static readonly Color Bg = new Color(0.055f, 0.055f, 0.066f, 0.98f);
        public static readonly Color Side = new Color(0.074f, 0.074f, 0.087f, 1f);
        public static readonly Color Panel = new Color(0.098f, 0.098f, 0.114f, 1f);
        public static readonly Color Panel2 = new Color(0.137f, 0.137f, 0.157f, 1f);
        public static readonly Color Border = new Color(0.19f, 0.19f, 0.215f, 1f);
        public static readonly Color Accent = new Color(0.93f, 0.31f, 0.24f, 1f);
        public static readonly Color Text = new Color(0.9f, 0.9f, 0.93f, 1f);
        public static readonly Color Dim = new Color(0.52f, 0.52f, 0.58f, 1f);
        public static readonly Color Green = new Color(0.3f, 0.85f, 0.55f, 1f);
        public static readonly Color Gold = new Color(1f, 0.8f, 0.25f, 1f);

        public static float Alpha = 1f;
        public static GUISkin Skin;
        public static string Tip = "";

        public static GUIStyle Label, DimLabel, Head, Btn, BtnAccent, Field, Row, Value, Small, Title, Big, Wrap;

        private static bool inited;
        private static readonly List<Texture2D> texs = new List<Texture2D>();
        private static readonly Dictionary<int, GUIStyle> fillSt = new Dictionary<int, GUIStyle>();
        private static readonly Dictionary<int, GUIStyle> ringSt = new Dictionary<int, GUIStyle>();
        private static readonly Dictionary<string, float> anims = new Dictionary<string, float>();

        // ---------- текстуры ----------
        private static Texture2D Bake(Color col, int r, bool ring)
        {
            int S = 2 * r + 3;
            Texture2D t = new Texture2D(S, S, TextureFormat.RGBA32, false);
            float half = S / 2f;
            Color[] px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half) - (half - r);
                    float dy = Mathf.Abs(y + 0.5f - half) - (half - r);
                    float qx = Mathf.Max(dx, 0f);
                    float qy = Mathf.Max(dy, 0f);
                    float d = Mathf.Sqrt(qx * qx + qy * qy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (ring) a -= Mathf.Clamp01(0.5f - (d + 1f));
                    px[y * S + x] = new Color(col.r, col.g, col.b, col.a * Mathf.Max(0f, a));
                }
            }
            t.SetPixels(px);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.hideFlags = HideFlags.HideAndDontSave;
            t.Apply();
            texs.Add(t);
            return t;
        }

        private static GUIStyle RoundStyle(int r, bool ring)
        {
            Dictionary<int, GUIStyle> d = ring ? ringSt : fillSt;
            GUIStyle st;
            if (d.TryGetValue(r, out st)) return st;
            st = new GUIStyle();
            st.normal.background = Bake(Color.white, r, ring);
            st.border = new RectOffset(r + 1, r + 1, r + 1, r + 1);
            d[r] = st;
            return st;
        }

        private static void RoundBg(GUIStyle s, Color n, Color h, Color a, int r)
        {
            s.normal.background = Bake(n, r, false);
            s.hover.background = Bake(h, r, false);
            s.active.background = Bake(a, r, false);
            s.focused.background = s.hover.background;
            s.onNormal.background = s.normal.background;
            s.onHover.background = s.hover.background;
            s.onActive.background = s.active.background;
            s.onFocused.background = s.hover.background;
            s.border = new RectOffset(r + 1, r + 1, r + 1, r + 1);
        }

        private static GUIStyle Base(GUIStyle src, Color txt, int size)
        {
            GUIStyle s = new GUIStyle(src);
            s.fontSize = size;
            s.normal.textColor = txt; s.hover.textColor = txt; s.active.textColor = txt; s.focused.textColor = txt;
            s.onNormal.textColor = txt; s.onHover.textColor = txt; s.onActive.textColor = txt; s.onFocused.textColor = txt;
            s.normal.background = null; s.hover.background = null; s.active.background = null; s.focused.background = null;
            s.onNormal.background = null; s.onHover.background = null; s.onActive.background = null; s.onFocused.background = null;
            return s;
        }

        public static void Init()
        {
            if (inited) return;
            inited = true;
            GUISkin sk = GUI.skin;

            Label = Base(sk.label, Text, 13);
            Label.wordWrap = false;
            Label.richText = true;
            DimLabel = Base(sk.label, Dim, 12);
            DimLabel.richText = true;
            Head = Base(sk.label, Text, 12);
            Head.richText = true;
            Head.fontStyle = FontStyle.Bold;
            Head.margin = new RectOffset(0, 0, 14, 6);
            Value = Base(sk.label, Accent, 13);
            Value.alignment = TextAnchor.MiddleCenter;
            Value.fontStyle = FontStyle.Bold;
            Small = Base(sk.label, Text, 11);
            Small.fontStyle = FontStyle.Bold;
            Small.alignment = TextAnchor.UpperCenter;
            Small.wordWrap = false;
            Title = Base(sk.label, Text, 15);
            Title.fontStyle = FontStyle.Bold;
            Title.richText = true;
            Title.alignment = TextAnchor.MiddleLeft;
            Big = Base(sk.label, Text, 20);
            Big.fontStyle = FontStyle.Bold;
            Big.richText = true;
            Big.alignment = TextAnchor.MiddleLeft;
            Wrap = new GUIStyle(DimLabel);
            Wrap.wordWrap = true;
            Wrap.margin = new RectOffset(2, 2, 0, 8);

            Btn = Base(sk.button, Text, 13);
            RoundBg(Btn, Panel2, new Color(0.19f, 0.19f, 0.22f, 1f), Accent, 8);
            Btn.alignment = TextAnchor.MiddleCenter;
            Btn.padding = new RectOffset(14, 14, 6, 6);
            Btn.margin = new RectOffset(2, 2, 3, 3);
            Btn.fixedHeight = 34f;

            BtnAccent = new GUIStyle(Btn);
            RoundBg(BtnAccent, Accent, new Color(1f, 0.4f, 0.32f, 1f), new Color(0.7f, 0.2f, 0.15f, 1f), 8);
            BtnAccent.fontStyle = FontStyle.Bold;

            Row = new GUIStyle(Btn);
            RoundBg(Row, Panel, Panel2, Accent, 6);
            Row.alignment = TextAnchor.MiddleLeft;
            Row.padding = new RectOffset(14, 14, 4, 4);
            Row.margin = new RectOffset(0, 0, 2, 2);
            Row.fixedHeight = 30f;

            Field = Base(sk.textField, Text, 13);
            RoundBg(Field, Panel2, Panel2, Panel2, 6);
            Field.padding = new RectOffset(10, 10, 6, 6);
            Field.margin = new RectOffset(2, 2, 3, 3);
            Field.fixedHeight = 32f;
            Field.alignment = TextAnchor.MiddleLeft;

            Skin = UnityEngine.Object.Instantiate(sk);
            Skin.hideFlags = HideFlags.HideAndDontSave;
            GUIStyle vs = Skin.verticalScrollbar;
            vs.fixedWidth = 8f;
            vs.normal.background = Bake(new Color(1f, 1f, 1f, 0.05f), 4, false);
            vs.border = new RectOffset(5, 5, 5, 5);
            vs.margin = new RectOffset(6, 0, 0, 0);
            GUIStyle th = Skin.verticalScrollbarThumb;
            th.fixedWidth = 8f;
            th.normal.background = Bake(new Color(1f, 1f, 1f, 0.24f), 4, false);
            th.hover.background = Bake(Accent, 4, false);
            th.active.background = th.hover.background;
            th.border = new RectOffset(5, 5, 5, 5);
            GUIStyle[] hide = { Skin.verticalScrollbarUpButton, Skin.verticalScrollbarDownButton, Skin.horizontalScrollbarLeftButton, Skin.horizontalScrollbarRightButton };
            foreach (GUIStyle h in hide)
            {
                h.normal.background = null; h.hover.background = null; h.active.background = null;
                h.fixedWidth = 1f; h.fixedHeight = 1f;
                h.border = new RectOffset(); h.padding = new RectOffset(); h.margin = new RectOffset();
            }
            Skin.horizontalScrollbar.fixedHeight = 1f;
            Skin.horizontalScrollbar.normal.background = null;
            Skin.horizontalScrollbarThumb.fixedHeight = 1f;
            Skin.horizontalScrollbarThumb.normal.background = null;
        }

        public static void Destroy()
        {
            foreach (Texture2D t in texs) if (t != null) UnityEngine.Object.Destroy(t);
            texs.Clear();
            fillSt.Clear();
            ringSt.Clear();
            anims.Clear();
            if (Skin != null) UnityEngine.Object.Destroy(Skin);
            Skin = null;
            inited = false;
        }

        // ---------- анимации ----------
        public static float Anim(string key, float target, float speed)
        {
            float v;
            if (!anims.TryGetValue(key, out v)) v = target;
            Event e = Event.current;
            if (e != null && e.type == EventType.Repaint)
            {
                v = Mathf.Lerp(v, target, 1f - Mathf.Exp(-speed * Mathf.Min(Time.unscaledDeltaTime, 0.05f)));
                if (Mathf.Abs(v - target) < 0.003f) v = target;
            }
            anims[key] = v;
            return v;
        }

        public static void SetAnim(string key, float v) { anims[key] = v; }

        // ---------- рисование ----------
        public static void Fill(Rect r, Color c)
        {
            Color o = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, c.a * Alpha);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = o;
        }

        public static void Outline(Rect r, Color c)
        {
            Fill(new Rect(r.x, r.y, r.width, 1f), c);
            Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
            Fill(new Rect(r.x, r.y, 1f, r.height), c);
            Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
        }

        /// <summary>Скруглённый прямоугольник (заливка) или рамка (ring).</summary>
        public static void Round(Rect rect, Color c, int radius, bool ring = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (c.a * Alpha <= 0.002f) return;
            int rr = Mathf.Min(radius, Mathf.FloorToInt(Mathf.Min(rect.width, rect.height) / 2f) - 1);
            if (rr < 1) { if (!ring) Fill(rect, c); return; }
            Color old = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, c.a * Alpha);
            RoundStyle(rr, ring).Draw(rect, GUIContent.none, false, false, false, false);
            GUI.color = old;
        }

        public static void Txt(Rect r, string s, GUIStyle st, Color c)
        {
            Color old = st.normal.textColor;
            st.normal.textColor = new Color(c.r, c.g, c.b, c.a * Alpha);
            GUI.Label(r, s, st);
            st.normal.textColor = old;
        }

        public static void Shadow(Rect r, string s, Color c, GUIStyle st)
        {
            Color old = st.normal.textColor;
            st.normal.textColor = new Color(0f, 0f, 0f, Alpha);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), s, st);
            st.normal.textColor = new Color(c.r, c.g, c.b, c.a * Alpha);
            GUI.Label(r, s, st);
            st.normal.textColor = old;
        }

        public static void Chip(Rect r, string s, Color c)
        {
            Round(r, new Color(c.r, c.g, c.b, 0.16f), 7);
            Round(r, new Color(c.r, c.g, c.b, 0.6f), 7, true);
            Txt(new Rect(r.x, r.y + (r.height - 16f) / 2f, r.width, 18f), s, Small, c);
        }

        private static void SetTip(Rect r, string tip)
        {
            if (!string.IsNullOrEmpty(tip) && Event.current.type == EventType.Repaint && r.Contains(Event.current.mousePosition)) Tip = tip;
        }

        // ---------- контролы ----------
        public static void Section(string num, string title)
        {
            GUILayout.Label("<color=#ED4F3D>/</color>  <size=11>" + num + "   " + title.ToUpperInvariant() + "</size>", Head);
            Rect r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            Fill(r, Border);
            GUILayout.Space(6f);
        }

        public static void Desc(string text)
        {
            GUILayout.Label(text, Wrap);
        }

        public static bool Toggle(string label, bool v, string desc = null)
        {
            float h = string.IsNullOrEmpty(desc) ? 40f : 56f;
            Rect r = GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true));
            r.y += 2f;
            r.height -= 4f;
            Event e = Event.current;
            bool hover = r.Contains(e.mousePosition);
            if (e.type == EventType.MouseDown && e.button == 0 && hover) { v = !v; e.Use(); }
            if (e.type == EventType.Repaint)
            {
                float t = Anim("tg" + label, v ? 1f : 0f, 14f);
                float hv = Anim("th" + label, hover ? 1f : 0f, 16f);
                Round(r, Color.Lerp(Panel, Panel2, hv), 9);
                Round(r, Color.Lerp(Border, new Color(Accent.r, Accent.g, Accent.b, 0.85f), t), 9, true);
                Color tc = Color.Lerp(Dim, Text, t);
                bool hasDesc = !string.IsNullOrEmpty(desc);
                Txt(new Rect(r.x + 16f, hasDesc ? r.y + 7f : r.y + (r.height - 20f) / 2f, r.width - 120f, 20f), label, Label, tc);
                if (hasDesc) Txt(new Rect(r.x + 16f, r.y + 28f, r.width - 120f, 18f), desc, DimLabel, Dim);
                Rect tr = new Rect(r.xMax - 58f, r.y + (r.height - 24f) / 2f, 42f, 24f);
                Round(tr, Color.Lerp(new Color(0.22f, 0.22f, 0.25f, 1f), Accent, t), 12);
                Round(new Rect(Mathf.Lerp(tr.x + 3f, tr.xMax - 21f, t), tr.y + 3f, 18f, 18f), Color.white, 9);
            }
            SetTip(r, desc);
            return v;
        }

        private static float FromMouse(float mx, Rect tr, float min, float max)
        {
            float f = Mathf.Clamp01((mx - tr.x - 8f) / Mathf.Max(1f, tr.width - 16f));
            return Mathf.Lerp(min, max, f);
        }

        public static float Slide(string label, float v, float min, float max, string fmt, string unit = "", string desc = null)
        {
            Rect top = GUILayoutUtility.GetRect(10f, 24f, GUILayout.ExpandWidth(true));
            Rect tr = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
            Rect hint = GUILayoutUtility.GetRect(10f, 20f, GUILayout.ExpandWidth(true));
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            Rect hit = new Rect(tr.x, tr.y - 4f, tr.width, tr.height + 8f);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && hit.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        v = FromMouse(e.mousePosition.x, tr, min, max);
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        v = FromMouse(e.mousePosition.x, tr, min, max);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
            v = Mathf.Clamp(v, min, max);

            if (e.type == EventType.Repaint)
            {
                bool active = GUIUtility.hotControl == id;
                float hv = Anim("sh" + label, (hit.Contains(e.mousePosition) || active) ? 1f : 0f, 16f);
                Txt(new Rect(top.x + 2f, top.y + 2f, top.width - 120f, 20f), label, Label, Text);
                string vt = v.ToString(fmt) + (unit.Length > 0 ? " " + unit : "");
                float cw = Mathf.Max(64f, Value.CalcSize(new GUIContent(vt)).x + 20f);
                Rect chip = new Rect(top.xMax - cw, top.y, cw, 24f);
                Round(chip, Panel2, 7);
                Txt(new Rect(chip.x, chip.y + 2f, chip.width, 20f), vt, Value, Accent);

                Rect track = new Rect(tr.x + 8f, tr.y + tr.height / 2f - 3f, tr.width - 16f, 6f);
                float f = Mathf.Clamp01((v - min) / Mathf.Max(0.0001f, max - min));
                Round(track, new Color(0.2f, 0.2f, 0.23f, 1f), 3);
                Round(new Rect(track.x, track.y, Mathf.Max(6f, f * track.width), 6f), Accent, 3);
                float cx = track.x + f * track.width;
                float cy = track.y + 3f;
                float kr = 8f + hv * 1.5f;
                Round(new Rect(cx - kr - 3f, cy - kr - 3f, (kr + 3f) * 2f, (kr + 3f) * 2f), new Color(Accent.r, Accent.g, Accent.b, 0.22f * hv), 12);
                Round(new Rect(cx - kr, cy - kr, kr * 2f, kr * 2f), Color.white, 10);

                string hs = min.ToString(fmt) + "  …  " + max.ToString(fmt) + (unit.Length > 0 ? " " + unit : "") + (string.IsNullOrEmpty(desc) ? "" : "     " + desc);
                Txt(new Rect(hint.x + 2f, hint.y + 1f, hint.width, 18f), hs, DimLabel, Dim);
            }
            SetTip(new Rect(top.x, top.y, top.width, hint.yMax - top.y), desc);
            GUILayout.Space(4f);
            return v;
        }

        public static bool Button(string text, params GUILayoutOption[] o)
        {
            return GUILayout.Button(text, Btn, o);
        }

        public static bool Button(string text, string tip, params GUILayoutOption[] o)
        {
            bool b = GUILayout.Button(text, Btn, o);
            SetTip(GUILayoutUtility.GetLastRect(), tip);
            return b;
        }

        public static bool Primary(string text, params GUILayoutOption[] o)
        {
            return GUILayout.Button(text, BtnAccent, o);
        }

        public static bool Primary(string text, string tip, params GUILayoutOption[] o)
        {
            bool b = GUILayout.Button(text, BtnAccent, o);
            SetTip(GUILayoutUtility.GetLastRect(), tip);
            return b;
        }

        public static string TextField(string v, params GUILayoutOption[] o)
        {
            return GUILayout.TextField(v, Field, o);
        }

        /// <summary>Сегментированный переключатель-«пилюли».</summary>
        public static int Tabs(int sel, string[] names)
        {
            Rect row = GUILayoutUtility.GetRect(10f, 38f, GUILayout.ExpandWidth(true));
            Event e = Event.current;
            float x = row.x;
            for (int i = 0; i < names.Length; i++)
            {
                float w = Small.CalcSize(new GUIContent(names[i])).x + 32f;
                Rect r = new Rect(x, row.y + 4f, w, 30f);
                x += w + 6f;
                bool on = i == sel;
                bool hover = r.Contains(e.mousePosition);
                if (e.type == EventType.MouseDown && e.button == 0 && hover) { sel = i; on = true; e.Use(); }
                if (e.type == EventType.Repaint)
                {
                    float t = Anim("sg" + names[i], on ? 1f : 0f, 14f);
                    float hv = Anim("sgh" + names[i], hover ? 1f : 0f, 16f);
                    Round(r, Color.Lerp(Color.Lerp(Panel, Panel2, hv), Accent, t), 8);
                    Txt(new Rect(r.x, r.y + 7f, r.width, 18f), names[i], Small, Color.Lerp(Dim, Color.white, Mathf.Max(t, hv * 0.6f)));
                }
            }
            return sel;
        }

        /// <summary>Пункт бокового меню: значок с кодом, название, акцентная полоска.</summary>
        public static bool Nav(Rect r, string code, string title, bool on)
        {
            Event e = Event.current;
            bool hover = r.Contains(e.mousePosition);
            bool click = false;
            if (e.type == EventType.MouseDown && e.button == 0 && hover) { click = true; e.Use(); }
            if (e.type == EventType.Repaint)
            {
                float t = Anim("nv" + title, on ? 1f : 0f, 14f);
                float hv = Anim("nh" + title, hover ? 1f : 0f, 16f);
                Round(r, new Color(Panel2.r, Panel2.g, Panel2.b, Mathf.Max(t, hv * 0.55f)), 9);
                Round(new Rect(r.x, r.y + 9f, 3f, r.height - 18f), new Color(Accent.r, Accent.g, Accent.b, t), 2);
                Rect ic = new Rect(r.x + 14f, r.y + (r.height - 26f) / 2f, 26f, 26f);
                Round(ic, Color.Lerp(Panel2, Accent, t), 8);
                Txt(new Rect(ic.x, ic.y + 5f, ic.width, 16f), code, Small, Color.Lerp(Dim, Color.white, Mathf.Max(t, hv)));
                Txt(new Rect(ic.xMax + 12f, r.y + (r.height - 20f) / 2f, r.width - 60f, 20f), title, Label, Color.Lerp(Dim, Text, Mathf.Max(t, hv)));
            }
            return click;
        }

        public static string HpColor(float f)
        {
            return f > 0.6f ? "#4DD98C" : (f > 0.3f ? "#FFCC40" : "#ED4F3D");
        }

        public static Color HpCol(float f)
        {
            return f > 0.6f ? Green : (f > 0.3f ? Gold : Accent);
        }
    }
}
