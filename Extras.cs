using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public sealed partial class Menu
    {
        private float nextExtras;
        private float nextLoot;
        private int lootCount = -1;
        private int lootTotal;
        private int haulCur;
        private int haulGoal;
        private readonly Vector3[] wps = new Vector3[5];
        private readonly bool[] wpSet = new bool[5];
        private float nextPack;
        private string extraStatus = "";
        private string moneyText = "100";

        private static readonly string[] UpgradeNames =
        {
            "Health", "Energy", "SprintSpeed", "ExtraJump", "GrabStrength", "ThrowStrength", "GrabRange", "TumbleLaunch", "TumbleClimb", "TumbleWings", "CrouchRest"
        };

        // ---------- движение ----------
        private void TeleportSelf(Vector3 p)
        {
            object pc = R.LocalController();
            Component c = pc as Component;
            if (c == null) { extraStatus = "Игрок не найден"; return; }
            c.transform.position = p;
            Rigidbody rb = R.Get(pc, "rb") as Rigidbody;
            if (rb != null)
            {
                rb.position = p;
                if (!R.Set(rb, "linearVelocity", Vector3.zero)) R.Set(rb, "velocity", Vector3.zero);
            }
        }

        private void TpCursor()
        {
            Camera cam = GetCam();
            if (cam == null) return;
            RaycastHit hit;
            Vector3 o = cam.transform.position + cam.transform.forward * 0.6f;
            if (Physics.Raycast(o, cam.transform.forward, out hit, 500f))
            {
                TeleportSelf(hit.point + hit.normal * 0.8f + Vector3.up * 0.3f);
                extraStatus = "Телепорт на " + Mathf.RoundToInt(hit.distance) + " м";
            }
            else extraStatus = "Курсор ни во что не попал";
            Note(extraStatus);
        }

        private void TpNearestValuable()
        {
            Camera cam = GetCam();
            if (cam == null) return;
            Component best = null;
            float bd = float.MaxValue;
            foreach (UnityEngine.Object o in R.FindAll("ValuableObject"))
            {
                Component c = o as Component;
                if (c == null) continue;
                float d = Vector3.Distance(cam.transform.position, c.transform.position);
                if (d < 2f || d >= bd) continue;
                bd = d; best = c;
            }
            if (best == null) { extraStatus = "Ценностей нет"; Note(extraStatus); return; }
            TeleportSelf(best.transform.position + Vector3.up * 1.2f);
            extraStatus = "Телепорт к ценности: " + Clean(best.name);
            Note(extraStatus);
        }

        // ---------- враги ----------
        private List<object> LiveEnemies()
        {
            List<object> res = new List<object>();
            object dir = R.GetS("EnemyDirector", "instance");
            foreach (object ep in R.List(R.Get(dir, "enemiesSpawned")))
            {
                if (!R.IsAlive(ep) || !R.B(ep, "Spawned", true)) continue;
                object en = R.Get(ep, "Enemy");
                if (R.IsAlive(en)) res.Add(en);
            }
            return res;
        }

        private void KillEnemy(object en)
        {
            object hp = R.Get(en, "Health");
            if (hp == null) return;
            R.Call(hp, "Hurt", 99999, Vector3.up);
        }

        private void KillAllEnemies()
        {
            if (!R.IsHost()) { extraStatus = "Только хост"; Note(extraStatus); return; }
            int n = 0;
            foreach (object en in LiveEnemies()) { KillEnemy(en); n++; }
            extraStatus = "Убито врагов: " + n;
            Note(extraStatus);
        }

        private void ActivateExtraction()
        {
            object ep = ActiveExtraction();
            if (ep == null) { extraStatus = "Точка эвакуации не найдена"; Note(extraStatus); return; }
            R.Call(ep, "OnClick");
            extraStatus = "Эвакуация: OnClick вызван";
            Note(extraStatus);
        }

        private void ExtrasTick()
        {
            if (Time.unscaledTime < nextExtras) return;
            nextExtras = Time.unscaledTime + 0.4f;

            if ((cfg.freeze || cfg.killAura) && R.IsHost())
            {
                Camera cam = GetCam();
                Vector3 me = cam != null ? cam.transform.position : Vector3.zero;
                foreach (object en in LiveEnemies())
                {
                    if (cfg.freeze) R.Call(en, "Freeze", 1f);
                    if (cfg.killAura)
                    {
                        Transform tr = R.Get(en, "CenterTransform") as Transform;
                        if (tr != null && Vector3.Distance(me, tr.position) <= cfg.killRadius) KillEnemy(en);
                    }
                }
            }

            if (cfg.autoEvac) AutoEvacTick();

            if (cfg.autoPack && R.IsHost() && Time.unscaledTime >= nextPack)
            {
                nextPack = Time.unscaledTime + 3f;
                object rd = R.GetS("RoundDirector", "instance");
                if (R.B(rd, "extractionPointActive")) PackToExtraction();
            }
        }

        // ---------- экономика ----------
        private void SetMoney()
        {
            int v;
            if (!int.TryParse(moneyText, out v)) { extraStatus = "Неверное число"; return; }
            R.CallS("SemiFunc", "StatSetRunCurrency", v);
            extraStatus = "Деньги выставлены: " + v + " (в единицах игры, обычно тысячи $)";
        }

        private string LocalSteamId()
        {
            object av = R.LocalAvatar();
            string id = R.S(av, "steamID");
            if (string.IsNullOrEmpty(id)) { object r = R.CallS("SemiFunc", "PlayerGetSteamID", av); id = r == null ? null : r.ToString(); }
            return id;
        }

        private void Upgrade(string name, int amount)
        {
            string id = LocalSteamId();
            if (string.IsNullOrEmpty(id)) { extraStatus = "SteamID не найден"; return; }
            object pm = R.GetS("PunManager", "instance");
            object res = R.Call(pm, "UpgradePlayer" + name, id, amount);
            extraStatus = name + " +" + amount + (res != null ? " -> уровень " + res : "");
        }

        // ---------- инфо об уровне ----------
        private void UpdateLoot()
        {
            if (Time.unscaledTime < nextLoot) return;
            nextLoot = Time.unscaledTime + 1.5f;
            UnityEngine.Object[] all = R.FindAll("ValuableObject");
            int n = 0;
            float sum = 0f;
            foreach (UnityEngine.Object o in all) { n++; sum += R.F(o, "dollarValueCurrent"); }
            lootCount = all.Length == 0 && !R.IsAlive(R.GetS("RoundDirector", "instance")) ? -1 : n;
            lootTotal = Mathf.RoundToInt(sum);
            object rd = R.GetS("RoundDirector", "instance");
            haulCur = R.I(rd, "currentHaul");
            haulGoal = R.I(rd, "haulGoal");
        }

        // ---------- лечение / точки ----------
        private void FullHeal()
        {
            object ph = R.Get(R.LocalAvatar(), "playerHealth");
            if (ph == null) return;
            R.Call(ph, "Heal", Math.Max(1, R.I(ph, "maxHealth", 100)), true);
            Note("Вылечен");
        }

        private void RefillStamina()
        {
            object pc = R.LocalController();
            if (pc == null) return;
            R.Set(pc, "EnergyCurrent", R.F(pc, "EnergyStart", 40f));
            Note("Выносливость заполнена");
        }

        private void SaveWp(int i)
        {
            object pc = R.LocalController();
            Component c = pc as Component;
            if (c == null) return;
            wps[i] = c.transform.position;
            wpSet[i] = true;
            extraStatus = "Точка " + (i + 1) + " сохранена";
            Note(extraStatus);
        }

        private void GoWp(int i)
        {
            if (!wpSet[i]) { extraStatus = "Точка " + (i + 1) + " ещё не сохранена"; Note(extraStatus); return; }
            TeleportSelf(wps[i] + Vector3.up * 0.2f);
            extraStatus = "Телепорт к точке " + (i + 1);
            Note(extraStatus);
        }

        // ---------- радар ----------
        private void DrawRadar()
        {
            Camera cam = GetCam();
            if (cam == null) return;
            float size = 180f;
            Rect r = new Rect(16f, 16f, size, size);
            Ui.Round(r, new Color(0.055f, 0.055f, 0.066f, 0.82f), 12);
            Ui.Round(r, Ui.Border, 12, true);
            float cx = r.x + size / 2f;
            float cy = r.y + size / 2f;
            Ui.Fill(new Rect(cx, r.y + 4f, 1f, size - 8f), new Color(1f, 1f, 1f, 0.07f));
            Ui.Fill(new Rect(r.x + 4f, cy, size - 8f, 1f), new Color(1f, 1f, 1f, 0.07f));
            Ui.Fill(new Rect(cx - 2f, cy - 2f, 5f, 5f), Color.white);

            Vector3 me = cam.transform.position;
            foreach (Ent e in ents)
            {
                if (e.Tr == null || e.Kind == KindTrap) continue;
                if (e.Kind == KindItem && !cfg.espItems) continue;
                Vector3 loc = cam.transform.InverseTransformDirection(e.Tr.position - me);
                float px = loc.x / cfg.radarRange * (size / 2f - 6f);
                float py = -loc.z / cfg.radarRange * (size / 2f - 6f);
                float len = Mathf.Sqrt(px * px + py * py);
                float lim = size / 2f - 6f;
                if (len > lim) { px = px / len * lim; py = py / len * lim; }
                Ui.Fill(new Rect(cx + px - 3f, cy + py - 3f, 6f, 6f), e.Col);
            }
            GUIStyle st = Ui.Small;
            TextAnchor oa = st.alignment;
            st.alignment = TextAnchor.UpperLeft;
            Ui.Shadow(new Rect(r.x + 6f, r.yMax - 16f, 160f, 14f), "RADAR  " + Mathf.RoundToInt(cfg.radarRange) + "m", Ui.Dim, st);
            st.alignment = oa;
        }

        // ---------- вкладка ----------
        private void TabExtra()
        {
            if (extraStatus.Length > 0) GUILayout.Label(extraStatus, Ui.DimLabel);
            Cols(delegate
            {
                Ui.Section("01", "Телепорт");
                Ui.Desc("Мгновенные перемещения. Для удобства назначь хоткеи во вкладке KEYS.");
                if (Ui.Button("К курсору", "Телепорт в точку, куда ты смотришь (до 500 м)")) TpCursor();
                if (Ui.Button("К ближайшей ценности", "Телепорт к ближайшей ценности на уровне")) TpNearestValuable();

                Ui.Section("02", "Бой (хост / соло)");
                cfg.freeze = Ui.Toggle("Заморозить врагов", cfg.freeze, "Враги стоят на месте (повтор каждые 0.4 с).");
                cfg.killAura = Ui.Toggle("Kill aura", cfg.killAura, "Враги в радиусе умирают.");
                if (cfg.killAura) cfg.killRadius = Mathf.Round(Ui.Slide("Радиус ауры", cfg.killRadius, 3f, 60f, "0", "м", "Вокруг тебя"));
                if (Ui.Button("Убить всех врагов", "Наносит смертельный урон всем активным врагам")) KillAllEnemies();

                Ui.Section("02б", "Радар и информация");
                cfg.radar = Ui.Toggle("Мини-радар", cfg.radar, "Квадрат слева сверху: враги, игроки, эвакуация вокруг тебя.");
                if (cfg.radar) cfg.radarRange = Mathf.Round(Ui.Slide("Дальность радара", cfg.radarRange, 10f, 120f, "0", "м", "Радиус, который показывает радар"));
                cfg.hudLoot = Ui.Toggle("Инфо об уровне в HUD", cfg.hudLoot, "Сколько ценностей осталось, их общая цена и прогресс хаула.");
                Ui.Section("02в", "Инструменты");
                cfg.infBattery = Ui.Toggle("Бесконечные батареи", cfg.infBattery, "Заряд всех предметов с батареей (дрон, оружие, инструменты) держится на максимуме. Фонарь в игре и так без батареи.");
                if (Ui.Button("Вылечить себя", "Полное лечение")) FullHeal();
                if (Ui.Button("Заполнить выносливость", "Мгновенно восстановить энергию")) RefillStamina();
            },
            delegate
            {
                Ui.Section("03", "Эвакуация");
                cfg.autoPack = Ui.Toggle("Авто-сбор на эвакуацию", cfg.autoPack, "Пока точка активна, ценности сами переносятся в её зону (раз в 3 с).");
                if (Ui.Button("Активировать точку", "Нажимает кнопку ближайшей / текущей точки эвакуации")) ActivateExtraction();
                cfg.autoEvac = Ui.Toggle("Авто-эвакуация по сумме", cfg.autoEvac, "Когда сумма у точки эвакуации достигнет цели, ты телепортируешься на точку.");
                if (cfg.autoEvac)
                {
                    cfg.autoEvacTarget = Mathf.Round(Ui.Slide("Цель", cfg.autoEvacTarget, 1000f, 300000f, "0", "$", "Сумма ценностей в радиусе 15 м от точки или haul эвакуации") / 500f) * 500f;
                    cfg.autoEvacActivate = Ui.Toggle("Сразу нажать кнопку точки", cfg.autoEvacActivate, "Если точка ждёт активации, она запустится сама.");
                    GUILayout.Label("Сейчас: $" + Mathf.RoundToInt(evacCollected) + " / $" + Mathf.RoundToInt(cfg.autoEvacTarget) + (evacFired ? "  (сработало)" : ""), Ui.DimLabel);
                }

                Ui.Section("04", "Экономика");
                cfg.freeShop = Ui.Toggle("Бесплатный магазин", cfg.freeShop, "Множитель цен в магазине = 0.");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Деньги", Ui.Label, GUILayout.Width(70f));
                moneyText = Ui.TextField(moneyText, GUILayout.Width(100f));
                if (Ui.Button("Выставить", "Задать текущие деньги забега (в единицах игры, обычно тысячи $)")) SetMoney();
                GUILayout.EndHorizontal();

                Ui.Section("04б", "Быстрые точки");
                Ui.Desc("Запомни позицию и телепортируйся к ней позже (сбрасывается при выгрузке).");
                for (int w = 0; w < wps.Length; w++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Точка " + (w + 1) + (wpSet[w] ? "  (есть)" : ""), Ui.Label, GUILayout.Width(150f));
                    if (Ui.Button("Сохранить", "Запомнить текущую позицию", GUILayout.Width(100f))) SaveWp(w);
                    if (Ui.Button("Телепорт", "Перенестись в сохранённую позицию", GUILayout.Width(100f))) GoWp(w);
                    GUILayout.EndHorizontal();
                }

                Ui.Section("05", "Апгрейды");
                Ui.Desc("Добавляют уровень выбранного апгрейда себе. +1 или +5.");
                foreach (string u in UpgradeNames)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(UpgradeRu(u), Ui.Label, GUILayout.Width(170f));
                    if (Ui.Button("+1", "Добавить 1 уровень", GUILayout.Width(56f))) Upgrade(u, 1);
                    if (Ui.Button("+5", "Добавить 5 уровней", GUILayout.Width(56f))) Upgrade(u, 5);
                    GUILayout.EndHorizontal();
                }
            });
        }

        private static string UpgradeRu(string u)
        {
            switch (u)
            {
                case "Health": return "Здоровье";
                case "Energy": return "Выносливость";
                case "SprintSpeed": return "Скорость бега";
                case "ExtraJump": return "Доп. прыжок";
                case "GrabStrength": return "Сила хвата";
                case "ThrowStrength": return "Сила броска";
                case "GrabRange": return "Дальность хвата";
                case "TumbleLaunch": return "Запуск при падении";
                case "TumbleClimb": return "Лазание";
                case "TumbleWings": return "Крылья";
                case "CrouchRest": return "Отдых в приседе";
            }
            return u;
        }
    }
}
