using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public sealed partial class Menu
    {
        private int itemsMode;          // 0 предметы, 1 ценности
        private Vector2 listScroll;
        private string mobSearch = "";
        private Vector2 mobScroll;
        private string profileName = "default";

        private void Cols(Action left, Action right)
        {
            float half = (contentW - 22f - 20f) / 2f;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(half));
            left();
            GUILayout.EndVertical();
            GUILayout.Space(20f);
            GUILayout.BeginVertical(GUILayout.Width(half));
            right();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static readonly Dictionary<string, string> KeyDesc = new Dictionary<string, string>
        {
            { "God mode", "Бессмертие: здоровье не падает" },
            { "Freecam", "Свободная камера (персонаж стоит)" },
            { "Night vision", "Ночное зрение / яркость" },
            { "Infinite batteries", "Бесконечные батареи предметов" },
            { "Auto evacuation", "Авто-телепорт на эвакуацию по сумме" },
            { "Infinite stamina", "Бесконечная выносливость" },
            { "Noclip", "Полёт сквозь стены" },
            { "No fog", "Убрать туман" },
            { "ESP enemies", "Показ врагов сквозь стены" },
            { "ESP items", "Показ ценностей и предметов" },
            { "ESP players", "Показ других игроков" },
            { "ESP extraction", "Показ точек эвакуации" },
            { "Chams", "Подсветка силуэтов через стены" },
            { "Pull items to me", "Притянуть ценности к себе" },
            { "Pack to extraction", "Перенести ценности на эвакуацию" },
            { "Remote sell", "Активировать точку и сложить туда ценности" },
            { "Set value on grabbed", "Задать цену ценности под прицелом" },
            { "Full heal (me)", "Полностью вылечить себя" },
            { "Refill stamina", "Заполнить выносливость" },
            { "Radar", "Мини-радар врагов, игроков, эвакуации" },
            { "Save waypoint 1", "Запомнить позицию (точка 1)" },
            { "Go to waypoint 1", "Телепорт к сохранённой точке 1" },
            { "Teleport to cursor", "Телепорт в точку, куда смотришь" },
            { "TP to nearest valuable", "Телепорт к ближайшей ценности" },
            { "Activate extraction", "Нажать кнопку точки эвакуации" },
            { "Freeze enemies", "Заморозка всех врагов (хост)" },
            { "Kill aura", "Враги рядом умирают (хост)" },
            { "No tumble", "Не падать и не кувыркаться" },
            { "Kill all enemies", "Убить всех врагов сразу (хост)" },
            { "HUD", "Панель статуса справа сверху" },
        };

        // ---------- PLAYER ----------
        private void TabSelf()
        {
            Cols(delegate
            {
                Ui.Section("01", "Выживание");
                cfg.god = Ui.Toggle("God mode", cfg.god, "Здоровье не падает, урон игнорируется.");
                cfg.stamina = Ui.Toggle("Бесконечная выносливость", cfg.stamina, "Бег и прыжки не тратят энергию.");
                cfg.noTumble = Ui.Toggle("Без падений (no tumble)", cfg.noTumble, "Персонаж не валится и не кувыркается от ударов.");
                cfg.noclip = Ui.Toggle("Noclip (полёт)", cfg.noclip, "W A S D, Space вверх, Ctrl вниз, Shift быстрее.");
                if (cfg.noclip) cfg.noclipSpeed = Ui.Slide("Скорость полёта", cfg.noclipSpeed, 2f, 40f, "0.#", "м/с", "Shift даёт ещё x2.5");
                Ui.Section("02", "Обзор");
                cfg.noFog = Ui.Toggle("Без тумана", cfg.noFog, "Отключает туман на уровне: видно дальше.");
                cfg.fovOn = Ui.Toggle("Свой угол обзора (FOV)", cfg.fovOn, "Заменяет FOV игры на значение ниже.");
                if (cfg.fovOn) cfg.fov = Mathf.Round(Ui.Slide("FOV", cfg.fov, 60f, 120f, "0", "°", "Обычно около 70-90"));
            },
            delegate
            {
                Ui.Section("03", "Параметры персонажа");
                Ui.Desc("Множители поверх обычных значений. Применяются каждый кадр и держатся после смены уровня. 1.0 = как в игре.");
                cfg.speedMul = Ui.Slide("Скорость бега", cfg.speedMul, 0.5f, 5f, "0.0#", "x", "Ходьба, бег и присед");
                cfg.jumpMul = Ui.Slide("Сила прыжка", cfg.jumpMul, 0.5f, 4f, "0.0#", "x", "Высота прыжка");
                cfg.gravityMul = Ui.Slide("Гравитация", cfg.gravityMul, 0f, 2f, "0.0#", "x", "0 = невесомость");
                cfg.strengthMul = Ui.Slide("Сила хвата", cfg.strengthMul, 1f, 10f, "0.0#", "x", "Тяжёлые предметы и бросок");
                cfg.grabRangeMul = Ui.Slide("Дальность хвата", cfg.grabRangeMul, 1f, 5f, "0.0#", "x", "Как далеко можно схватить");
                if (Ui.Button("Сбросить параметры", "Вернуть все множители на 1.0"))
                    cfg.speedMul = cfg.jumpMul = cfg.gravityMul = cfg.strengthMul = cfg.grabRangeMul = 1f;
            });
        }

        // ---------- ESP ----------
        private void TabEsp()
        {
            Cols(delegate
            {
                Ui.Section("01", "Что показывать");
                cfg.espEnemies = Ui.Toggle("Враги", cfg.espEnemies, "Имя, здоровье и дистанция врагов.");
                cfg.espItems = Ui.Toggle("Ценности и предметы", cfg.espItems, "Название и цена. Фильтр цены справа.");
                cfg.espPlayers = Ui.Toggle("Игроки", cfg.espPlayers, "Другие игроки и их здоровье.");
                cfg.espExtraction = Ui.Toggle("Точки эвакуации", cfg.espExtraction, "Метки точек и их состояние.");
                cfg.espTraps = Ui.Toggle("Ловушки", cfg.espTraps, "Ловушки на уровне (использованные помечены).");
                cfg.chams = Ui.Toggle("Chams", cfg.chams, "Цветной силуэт сквозь стены у выбранных целей.");
            },
            delegate
            {
                Ui.Section("02", "Вид");
                cfg.espBoxes = Ui.Toggle("Рамки", cfg.espBoxes, "Уголки вокруг врагов и игроков.");
                cfg.espHealth = Ui.Toggle("Полоски здоровья", cfg.espHealth, "Слева от рамки.");
                cfg.espDist = Ui.Toggle("Дистанция", cfg.espDist, "Метры под меткой.");
                Ui.Section("03", "Фильтры");
                cfg.espMaxDist = Mathf.Round(Ui.Slide("Макс. дистанция", cfg.espMaxDist, 10f, 500f, "0", "м", "Дальше метки не рисуются"));
                cfg.espMinValue = Mathf.Round(Ui.Slide("Мин. цена", cfg.espMinValue, 0f, 20000f, "0", "$", "Скрыть дешёвые ценности") / 100f) * 100f;
                Ui.Desc("Рисуется максимум 60 ближайших меток в поле зрения, чтобы не было каши.");
            });
        }

        // ---------- ITEMS ----------
        private void TabItems()
        {
            Cols(delegate
            {
                Ui.Section("01", "Цена ценности");
                Ui.Desc("Меняет стоимость ценности. «Под прицелом»: навёл прицел или держишь предмет, закрыл меню, нажал хоткей из вкладки KEYS.");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Цена $", Ui.Label, GUILayout.Width(70f));
                valueText = Ui.TextField(valueText, GUILayout.Width(100f));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (Ui.Button("Под прицелом", "Выставить цену на ценность в руках или под прицелом")) SetValueGrabbed();
                if (Ui.Button("На все ценности", "Выставить цену на все ценности на уровне")) SetValueAll();
                GUILayout.EndHorizontal();
                if (Ui.Primary("Заспавнить мешок денег", "Ищет денежный префаб по компоненту MoneyValuable во всех уровнях")) SpawnMoneyBag();
                Ui.Desc("Появится перед тобой с ценой из поля выше. Только хост или соло.");
            },
            delegate
            {
                Ui.Section("02", "Перемещение предметов");
                cfg.pullValuables = Ui.Toggle("Ценности", cfg.pullValuables, "Включать ценности в перемещение.");
                cfg.pullItems = Ui.Toggle("Предметы", cfg.pullItems, "Включать предметы (магазинные и найденные).");
                cfg.pullRange = Mathf.Round(Ui.Slide("Радиус притяжения", cfg.pullRange, 5f, 300f, "0", "м", "Только для кнопки Pull to me"));
                GUILayout.BeginHorizontal();
                if (Ui.Button("Притянуть", "Все выбранные объекты в радиусе падают перед тобой")) PullItems();
                if (Ui.Button("На эвакуацию", "Все ценности переносятся в зону точки эвакуации")) PackToExtraction();
                GUILayout.EndHorizontal();
                if (Ui.Button("Remote sell", "Активирует точку (если ещё не нажата) и сразу складывает туда все ценности")) RemoteSell();
            });

            Ui.Section("03", "Спавнер");
            itemsMode = Ui.Tabs(itemsMode, new string[] { "ПРЕДМЕТЫ", "ЦЕННОСТИ" });
            GUILayout.BeginHorizontal();
            GUILayout.Label("Поиск", Ui.DimLabel, GUILayout.Width(50f));
            if (itemsMode == 0) itemSearch = Ui.TextField(itemSearch); else valSearch = Ui.TextField(valSearch);
            if (Ui.Button("Обновить", "Перечитать список из игры", GUILayout.Width(100f))) { if (itemsMode == 0) BuildItemList(); else BuildValuableList(); }
            GUILayout.EndHorizontal();

            List<SpawnEntry> src;
            string q;
            if (itemsMode == 0) { if (itemList == null) BuildItemList(); src = itemList; q = itemSearch; }
            else { if (valuableList == null) BuildValuableList(); src = valuableList; q = valSearch; }
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Height(230f));
            int shown = 0;
            if (src != null)
            {
                foreach (SpawnEntry e in src)
                {
                    if (q.Length > 0 && e.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    shown++;
                    if (GUILayout.Button(e.Name, Ui.Row))
                    {
                        int v = 0;
                        if (itemsMode == 1) int.TryParse(valueText, out v);
                        SpawnPrefab(e, v);
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Label(shown + " найдено. Клик по строке = заспавнить перед собой (хост / соло)." + (itemsMode == 1 ? " Цена берётся из поля «Цена $»." : ""), Ui.DimLabel);
        }

        // ---------- HOST ----------
        private void TabHost()
        {
            bool host = R.IsHost();
            Ui.Desc(host ? "Ты хост или играешь соло: действия доступны." : "Ты не хост: действия отключены, их может выполнять только хост.");
            if (hostStatus.Length > 0) GUILayout.Label(hostStatus, Ui.DimLabel);
            GUI.enabled = host;

            Ui.Section("01", "Игроки");
            Ui.Desc("Heal: полное лечение. Revive: воскресить. Kill: убить. Bring: телепорт игрока к тебе. Go to: телепорт тебя к игроку.");
            foreach (object a in R.List(R.CallS("SemiFunc", "PlayerGetAll")))
            {
                if (!R.IsAlive(a)) continue;
                bool local = R.B(a, "isLocal");
                object ph = R.Get(a, "playerHealth");
                int hp = R.I(ph, "health");
                int mx = Mathf.Max(1, R.I(ph, "maxHealth", 100));
                string nm = (R.S(a, "playerName") ?? "?") + (local ? "  (ты)" : "");
                GUILayout.BeginHorizontal();
                GUILayout.Label(nm + "   <color=" + Ui.HpColor((float)hp / mx) + ">" + hp + "/" + mx + "</color>", Ui.Label, GUILayout.Width(260f));
                GUILayout.FlexibleSpace();
                if (Ui.Button("Heal", "Полностью вылечить")) PlayerAction(a, "heal");
                if (Ui.Button("Revive", "Воскресить")) PlayerAction(a, "revive");
                if (!local)
                {
                    if (Ui.Button("Kill", "Убить игрока")) PlayerAction(a, "kill");
                    if (Ui.Button("Bring", "Телепортировать игрока к тебе")) PlayerAction(a, "bring");
                    if (Ui.Button("Go to", "Телепортироваться к игроку")) PlayerAction(a, "goto");
                }
                GUILayout.EndHorizontal();
            }

            Ui.Section("02", "Спавн мобов");
            Ui.Desc("Клик по строке создаёт врага перед тобой на навмеше и «будит» его. Если не появился, смотри вкладку LOG.");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Поиск", Ui.DimLabel, GUILayout.Width(50f));
            mobSearch = Ui.TextField(mobSearch);
            if (Ui.Button("Обновить", "Перечитать список врагов", GUILayout.Width(100f))) BuildSetups();
            GUILayout.EndHorizontal();
            if (setups == null) BuildSetups();
            mobScroll = GUILayout.BeginScrollView(mobScroll, GUILayout.Height(200f));
            if (setups != null)
            {
                foreach (UnityEngine.Object s in setups)
                {
                    if (s == null) continue;
                    string n = Clean(s.name);
                    if (mobSearch.Length > 0 && n.IndexOf(mobSearch, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (GUILayout.Button(n, Ui.Row)) SpawnEnemy(s);
                }
            }
            GUILayout.EndScrollView();
            GUI.enabled = true;
        }

        // ---------- KEYS ----------
        private void TabKeys()
        {
            Ui.Section("01", "Горячие клавиши");
            Ui.Desc("Нажми на кнопку справа, затем нужную клавишу. Esc отменяет, X очищает. F10 всегда выгружает чит. Хоткеи не срабатывают, пока ты печатаешь в поле ввода.");
            KeyRow("Меню", "Показать / скрыть это окно", "\u0001menu", cfg.menuKey, false);
            foreach (HotkeyAction a in actions)
            {
                KeyCode hk;
                cfg.hotkeys.TryGetValue(a.Name, out hk);
                string d;
                KeyDesc.TryGetValue(a.Name, out d);
                KeyRow(a.Name, d ?? "", a.Name, hk, true);
            }
        }

        private void KeyRow(string label, string desc, string id, KeyCode k, bool clearable)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(380f));
            GUILayout.Label(label, Ui.Label);
            if (desc.Length > 0) GUILayout.Label(desc, Ui.DimLabel);
            GUILayout.EndVertical();
            string t = rebind == id ? "нажми клавишу..." : (k == KeyCode.None ? "не задана" : k.ToString());
            if (GUILayout.Button(t, rebind == id ? Ui.BtnAccent : Ui.Btn, GUILayout.Width(190f))) rebind = id;
            if (clearable && Ui.Button("X", "Очистить клавишу", GUILayout.Width(34f))) cfg.hotkeys[id] = KeyCode.None;
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        // ---------- PROFILES ----------
        private void TabProfiles()
        {
            Cols(delegate
            {
                Ui.Section("01", "Профиль");
                Ui.Desc("Профиль хранит все переключатели, слайдеры и хоткеи. При запуске грузится профиль «default».");
                GUILayout.Label("Текущий: " + Cfg.Profile, Ui.Label);
                profileName = Ui.TextField(profileName);
                GUILayout.BeginHorizontal();
                if (Ui.Primary("Сохранить", "Записать текущие настройки в профиль с этим именем"))
                {
                    Cfg.Profile = Cfg.Sanitize(profileName);
                    cfg.Save();
                }
                if (Ui.Button("Загрузить", "Прочитать профиль с этим именем"))
                {
                    string old = Cfg.Profile;
                    Cfg.Profile = Cfg.Sanitize(profileName);
                    if (!cfg.Load()) Cfg.Profile = old;
                }
                GUILayout.EndHorizontal();
                Ui.Section("02", "Интерфейс");
                cfg.hud = Ui.Toggle("HUD статуса", cfg.hud, "Панель справа сверху: что сейчас включено.");
                GUILayout.Space(6f);
                if (Ui.Button("Выгрузить чит (F10)", "Откатить все изменения и убрать меню")) Loader.UnloadCheat();
            },
            delegate
            {
                Ui.Section("03", "Сохранённые профили");
                Ui.Desc("Клик по имени загружает профиль, X удаляет.");
                foreach (string p in Cfg.Profiles())
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(p, Ui.Row)) { profileName = p; Cfg.Profile = p; cfg.Load(); }
                    if (Ui.Button("X", "Удалить профиль", GUILayout.Width(34f))) Cfg.Delete(p);
                    GUILayout.EndHorizontal();
                }
                GUILayout.Label(Cfg.Path, Ui.DimLabel);
            });
        }

        // ---------- LOG ----------
        private void TabLog()
        {
            Ui.Section("01", "Журнал");
            Ui.Desc("Здесь пишется, какие типы, поля и методы игры не нашлись, и ошибки спавна. Новые записи сверху.");
            for (int i = Log.Lines.Count - 1; i >= 0; i--) GUILayout.Label(Log.Lines[i], Ui.DimLabel);
        }
    }
}
