using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    /// <summary>Freecam, ночное зрение, цвета ESP, бесконечные батареи, авто-эвакуация.</summary>
    public sealed partial class Menu
    {
        // ---------- цвета ESP ----------
        private static readonly string[] ColNames = { "Враги", "Ценности", "Предметы (items)", "Игроки", "Эвакуация", "Ловушки" };
        private int colSel;

        private Color EspCol(int i)
        {
            switch (i)
            {
                case 0: return new Color(cfg.colEnemyR, cfg.colEnemyG, cfg.colEnemyB);
                case 1: return new Color(cfg.colValuableR, cfg.colValuableG, cfg.colValuableB);
                case 2: return new Color(cfg.colItemR, cfg.colItemG, cfg.colItemB);
                case 3: return new Color(cfg.colPlayerR, cfg.colPlayerG, cfg.colPlayerB);
                case 4: return new Color(cfg.colExtractR, cfg.colExtractG, cfg.colExtractB);
                default: return new Color(cfg.colTrapR, cfg.colTrapG, cfg.colTrapB);
            }
        }

        private void SetEspCol(int i, float r, float g, float b)
        {
            switch (i)
            {
                case 0: cfg.colEnemyR = r; cfg.colEnemyG = g; cfg.colEnemyB = b; break;
                case 1: cfg.colValuableR = r; cfg.colValuableG = g; cfg.colValuableB = b; break;
                case 2: cfg.colItemR = r; cfg.colItemG = g; cfg.colItemB = b; break;
                case 3: cfg.colPlayerR = r; cfg.colPlayerG = g; cfg.colPlayerB = b; break;
                case 4: cfg.colExtractR = r; cfg.colExtractG = g; cfg.colExtractB = b; break;
                default: cfg.colTrapR = r; cfg.colTrapG = g; cfg.colTrapB = b; break;
            }
        }

        private static Color DefaultEspCol(int i)
        {
            switch (i)
            {
                case 0: return new Color(0.93f, 0.31f, 0.24f);
                case 1: return new Color(1f, 0.8f, 0.25f);
                case 2: return new Color(0.4f, 0.75f, 1f);
                case 3: return new Color(0.3f, 0.85f, 0.55f);
                case 4: return new Color(0.75f, 0.45f, 1f);
                default: return new Color(1f, 0.55f, 0.15f);
            }
        }

        // ---------- freecam ----------
        private bool freecam;
        private bool fcWas;
        private Camera fcCam;
        private Vector3 fcPos;
        private float fcYaw;
        private float fcPitch;
        private float fcEvX;
        private float fcEvY;

        private void SetFreecam(bool on)
        {
            freecam = on;
            if (!on) fcWas = false;
            Flip("Freecam", on);
        }

        private void FreecamTick()
        {
            if (!freecam) { fcWas = false; return; }
            if (!fcWas)
            {
                fcCam = GetCam();
                if (fcCam == null) { freecam = false; extraStatus = "Камера не найдена"; return; }
                fcPos = fcCam.transform.position;
                Vector3 eu = fcCam.transform.eulerAngles;
                fcYaw = eu.y;
                fcPitch = eu.x > 180f ? eu.x - 360f : eu.x;
                fcWas = true;
            }
            if (fcCam == null || !fcCam.enabled) fcCam = GetCam();
            if (fcCam == null) return;

            object im = R.GetS("InputManager", "instance");
            if (R.IsAlive(im))
            {
                R.Call(im, "DisableMovement", 0.1f);
                R.Call(im, "DisableAiming", 0.1f);
            }

            float dt = Time.unscaledDeltaTime;
            if (!open)
            {
                float lx = 0f, ly = 0f;
                if (R.IsAlive(im))
                {
                    object d = R.Call(im, "GetRawMouseDelta");
                    if (d is Vector2) { lx = ((Vector2)d).x * 0.1f; ly = ((Vector2)d).y * 0.1f; }
                }
                if (Mathf.Abs(lx) < 0.0001f && Mathf.Abs(ly) < 0.0001f) { lx = fcEvX * 0.1f; ly = fcEvY * 0.1f; }
                fcYaw += lx * cfg.fcSens;
                fcPitch = Mathf.Clamp(fcPitch + ly * cfg.fcSens, -89f, 89f);

                Quaternion rot = Quaternion.Euler(fcPitch, fcYaw, 0f);
                Vector3 mv = Vector3.zero;
                if (Held(KeyCode.W)) mv += rot * Vector3.forward;
                if (Held(KeyCode.S)) mv -= rot * Vector3.forward;
                if (Held(KeyCode.D)) mv += rot * Vector3.right;
                if (Held(KeyCode.A)) mv -= rot * Vector3.right;
                if (Held(KeyCode.Space) || Held(KeyCode.E)) mv += Vector3.up;
                if (Held(KeyCode.LeftControl) || Held(KeyCode.Q)) mv -= Vector3.up;
                float sp = cfg.fcSpeed * (Held(KeyCode.LeftShift) ? 3f : 1f);
                if (mv.sqrMagnitude > 0.0001f) fcPos += mv.normalized * sp * dt;
            }
            fcEvX = 0f;
            fcEvY = 0f;
        }

        // вызывается прямо перед отрисовкой камеры, после всех LateUpdate игры
        private void OnPreCullCb(Camera c)
        {
            if (!freecam || !fcWas || fcCam == null || c != fcCam) return;
            c.transform.SetPositionAndRotation(fcPos, Quaternion.Euler(fcPitch, fcYaw, 0f));
        }

        // ---------- ночное зрение ----------
        private bool nvSaved;
        private Color nvAmb;
        private float nvInt;
        private UnityEngine.Rendering.AmbientMode nvMode;
        private Light nvLightObj;

        private void ApplyNight()
        {
            if (cfg.nightVision)
            {
                if (!nvSaved)
                {
                    nvAmb = RenderSettings.ambientLight;
                    nvInt = RenderSettings.ambientIntensity;
                    nvMode = RenderSettings.ambientMode;
                    nvSaved = true;
                }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.85f, 0.9f, 1f, 1f);
                RenderSettings.ambientIntensity = cfg.nvBrightness;

                if (cfg.nvLight)
                {
                    Camera cam = GetCam();
                    if (nvLightObj == null)
                    {
                        GameObject go = new GameObject("REPOMenu_NV");
                        go.transform.SetParent(transform, false);
                        nvLightObj = go.AddComponent<Light>();
                        nvLightObj.type = LightType.Point;
                        nvLightObj.shadows = LightShadows.None;
                        nvLightObj.color = new Color(0.9f, 0.96f, 1f);
                    }
                    nvLightObj.enabled = cam != null;
                    nvLightObj.range = cfg.nvRange;
                    nvLightObj.intensity = cfg.nvPower;
                    if (cam != null) nvLightObj.transform.position = cam.transform.position;
                }
                else if (nvLightObj != null) nvLightObj.enabled = false;
            }
            else RestoreNight();
        }

        private void RestoreNight()
        {
            if (nvLightObj != null) nvLightObj.enabled = false;
            if (!nvSaved) return;
            RenderSettings.ambientMode = nvMode;
            RenderSettings.ambientLight = nvAmb;
            RenderSettings.ambientIntensity = nvInt;
            nvSaved = false;
        }

        // ---------- бесконечные батареи ----------
        private float nextBattery;

        private void BatteryTick()
        {
            if (!cfg.infBattery) return;
            if (Time.unscaledTime < nextBattery) return;
            nextBattery = Time.unscaledTime + 0.15f;
            foreach (UnityEngine.Object o in R.FindAll("ItemBattery"))
            {
                if (o == null) continue;
                if (R.F(o, "batteryLife", 100f) < 99f)
                {
                    R.Call(o, "SetBatteryLife", 100f);
                    if (R.F(o, "batteryLife", 100f) < 99f) R.Set(o, "batteryLife", 100f);
                }
            }
        }

        // ---------- авто-эвакуация ----------
        private bool evacFired;
        private float evacFiredAt;
        private float evacCollected;

        private void AutoEvacTick()
        {
            if (!cfg.autoEvac) { evacFired = false; return; }
            object rd = R.GetS("RoundDirector", "instance");
            if (!R.IsAlive(rd)) return;
            object ep = ActiveExtraction();
            if (ep == null) { evacCollected = 0f; return; }
            Component epc = ep as Component;
            if (epc == null) return;
            GameObject area = R.Get(ep, "extractionArea") as GameObject;
            Vector3 center = area != null ? area.transform.position : epc.transform.position;

            // собрано = ценности, уже лежащие у точки, или haul текущей эвакуации
            float near = 0f;
            foreach (UnityEngine.Object o in R.FindAll("ValuableObject"))
            {
                Component c = o as Component;
                if (c == null) continue;
                if (Vector3.Distance(c.transform.position, center) <= 15f) near += R.F(o, "dollarValueCurrent");
            }
            evacCollected = Mathf.Max(near, (float)R.I(rd, "currentHaul"));

            if (evacFired)
            {
                if (!R.B(rd, "extractionPointActive") && Time.unscaledTime > evacFiredAt + 20f && evacCollected < cfg.autoEvacTarget) evacFired = false;
                return;
            }
            if (evacCollected < cfg.autoEvacTarget) return;

            evacFired = true;
            evacFiredAt = Time.unscaledTime;
            TeleportSelf(center + Vector3.up * 1.2f);
            if (cfg.autoEvacActivate)
            {
                object st = R.Get(ep, "currentState");
                if (st != null && st.ToString() == "Idle") R.Call(ep, "OnClick");
            }
            extraStatus = "Авто-эвакуация: собрано $" + Mathf.RoundToInt(evacCollected) + ", телепорт на точку";
            Note(extraStatus);
        }

        // ---------- вкладка "Визуал" ----------
        private void TabVisual()
        {
            Cols(delegate
            {
                Ui.Section("01", "Freecam");
                bool f = Ui.Toggle("Свободная камера", freecam, "Камера отлетает от персонажа. W A S D, Space/E вверх, Ctrl/Q вниз, Shift быстрее. Персонаж стоит.");
                if (f != freecam) SetFreecam(f);
                cfg.fcSpeed = Mathf.Round(Ui.Slide("Скорость камеры", cfg.fcSpeed, 2f, 60f, "0", "м/с", "Shift даёт x3"));
                cfg.fcSens = Ui.Slide("Чувствительность", cfg.fcSens, 0.2f, 4f, "0.0#", "x", "Поворот мышью. Меню закрой, чтобы управлять.");
                Ui.Desc("Закрой меню (Insert), чтобы летать. Повторный Insert возвращает курсор, камера остаётся.");

                Ui.Section("02", "Ночное зрение");
                cfg.noFog = Ui.Toggle("Без тумана", cfg.noFog, "Отключает туман на уровне.");
                cfg.nightVision = Ui.Toggle("Ночное зрение / яркость", cfg.nightVision, "Поднимает общий свет сцены вместо тёмной атмосферы.");
                if (cfg.nightVision)
                {
                    cfg.nvBrightness = Ui.Slide("Яркость", cfg.nvBrightness, 0.5f, 4f, "0.0#", "x", "Общее освещение. 1 примерно как днём.");
                    cfg.nvLight = Ui.Toggle("Свет у камеры", cfg.nvLight, "Точечный источник света, следует за камерой.");
                    if (cfg.nvLight)
                    {
                        cfg.nvRange = Mathf.Round(Ui.Slide("Радиус света", cfg.nvRange, 5f, 80f, "0", "м", "Дальность подсветки"));
                        cfg.nvPower = Ui.Slide("Сила света", cfg.nvPower, 0.2f, 4f, "0.0#", "x", "Интенсивность источника");
                    }
                }
            },
            delegate
            {
                Ui.Section("03", "Цвета ESP");
                Ui.Desc("Выбери тип и настрой цвет ползунками. Цвета сохраняются в профиле и действуют на метки, рамки, радар и chams.");
                for (int i = 0; i < ColNames.Length; i++)
                {
                    Rect r = GUILayoutUtility.GetRect(10f, 38f, GUILayout.ExpandWidth(true));
                    if (Ui.Nav(r, "", ColNames[i], colSel == i)) colSel = i;
                    if (Event.current.type == EventType.Repaint)
                        Ui.Round(new Rect(r.x + 14f, r.y + (r.height - 26f) / 2f, 26f, 26f), EspCol(i), 8);
                }
                Ui.Section("04", "Цвет: " + ColNames[colSel]);
                Color c = EspCol(colSel);
                float cr = Ui.Slide("Красный", c.r, 0f, 1f, "0.00", "", null);
                float cg = Ui.Slide("Зелёный", c.g, 0f, 1f, "0.00", "", null);
                float cb = Ui.Slide("Синий", c.b, 0f, 1f, "0.00", "", null);
                if (cr != c.r || cg != c.g || cb != c.b) SetEspCol(colSel, cr, cg, cb);
                Rect sw = GUILayoutUtility.GetRect(10f, 26f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint) Ui.Round(sw, EspCol(colSel), 8);
                if (Ui.Button("Сбросить этот цвет", "Вернуть стандартный цвет")) { Color d = DefaultEspCol(colSel); SetEspCol(colSel, d.r, d.g, d.b); }
            });
        }
    }
}
