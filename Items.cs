using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public sealed partial class Menu
    {
        private sealed class SpawnEntry
        {
            public string Name;
            public object Prefab; // PrefabRef
        }

        private List<SpawnEntry> itemList;
        private List<SpawnEntry> valuableList;
        private string itemSearch = "";
        private string valSearch = "";
        private string valueText = "5000";
        private string status = "";

        // ---------- списки ----------
        private static string PrefabPath(object prefabRef)
        {
            string p = R.S(prefabRef, "ResourcePath");
            if (string.IsNullOrEmpty(p)) p = R.S(prefabRef, "resourcePath");
            return p;
        }

        private void BuildItemList()
        {
            itemList = new List<SpawnEntry>();
            object sm = R.GetS("StatsManager", "instance");
            IDictionary dict = R.Get(sm, "itemDictionary") as IDictionary;
            if (dict == null) { status = "StatsManager.itemDictionary недоступен (зайди в игру)"; return; }
            foreach (DictionaryEntry de in dict)
            {
                object item = de.Value;
                if (item == null) continue;
                SpawnEntry e = new SpawnEntry();
                e.Name = R.S(item, "itemName") ?? Convert.ToString(de.Key);
                e.Prefab = R.Get(item, "prefab");
                itemList.Add(e);
            }
            itemList.Sort(delegate (SpawnEntry a, SpawnEntry b) { return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); });
        }

        private void AddValuables(Dictionary<string, SpawnEntry> map, object listObj)
        {
            foreach (object pr in R.List(listObj))
            {
                string path = PrefabPath(pr);
                if (string.IsNullOrEmpty(path) || map.ContainsKey(path)) continue;
                SpawnEntry e = new SpawnEntry();
                e.Prefab = pr;
                e.Name = R.S(pr, "PrefabName");
                if (string.IsNullOrEmpty(e.Name)) e.Name = path.Substring(path.LastIndexOf('/') + 1);
                map[path] = e;
            }
        }

        private void BuildValuableList()
        {
            Dictionary<string, SpawnEntry> map = new Dictionary<string, SpawnEntry>();
            object rm = R.GetS("RunManager", "instance");
            string[] sizes = { "tiny", "small", "medium", "big", "wide", "tall", "veryTall" };

            // как делает /spawn valuable в игре: пресеты ценностей всех уровней (RunManager.levels), плюс текущий
            List<object> levels = R.List(R.Get(rm, "levels"));
            object cur = R.Get(rm, "levelCurrent");
            if (cur != null && !levels.Contains(cur)) levels.Add(cur);
            foreach (object level in levels)
                foreach (object preset in R.List(R.Get(level, "ValuablePresets")))
                    foreach (string sz in sizes) AddValuables(map, R.Get(preset, sz));

            object vd = R.GetS("ValuableDirector", "instance");
            string[] dl = { "tinyValuables", "smallValuables", "mediumValuables", "bigValuables", "wideValuables", "tallValuables", "veryTallValuables" };
            foreach (string sz in dl) AddValuables(map, R.Get(vd, sz));

            valuableList = new List<SpawnEntry>(map.Values);
            valuableList.Sort(delegate (SpawnEntry x, SpawnEntry y) { return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase); });
            if (valuableList.Count == 0) status = "Список ценностей пуст: зайди в уровень или лобби и нажми «Обновить»";
            else Log.Add("Ценностей в списке: " + valuableList.Count);
        }

        // ---------- спавн ----------
        private Vector3 FrontOfPlayer(float dist)
        {
            Camera cam = GetCam();
            if (cam == null) return Vector3.zero;
            return cam.transform.position + cam.transform.forward * dist;
        }

        /// <summary>Как делает сама игра (PunManager.ItemSpawnApply): мастер в мультиплеере - InstantiateRoomObject по ResourcePath, иначе (соло) - Object.Instantiate(PrefabRef.Prefab).</summary>
        private GameObject SpawnLikeGame(object prefabRef, string path, Vector3 at)
        {
            // как в DebugCommandHandler / ItemSpawnApply: GameManager.gameMode == 0 (соло) -> Object.Instantiate(PrefabRef.Prefab), иначе InstantiateRoomObject
            object gm = R.GetS("GameManager", "instance");
            bool solo;
            if (R.IsAlive(gm)) solo = R.I(gm, "gameMode", 0) == 0;
            else { object m = R.CallS("SemiFunc", "IsMasterClient"); solo = !(m is bool && (bool)m); }
            if (!solo) return PhotonNetwork.InstantiateRoomObject(path, at, Quaternion.identity, 0, null);
            GameObject prefab = R.Get(prefabRef, "Prefab") as GameObject;
            if (prefab == null) { Log.Add("PrefabRef.Prefab = null для " + path); return null; }
            return UnityEngine.Object.Instantiate(prefab, at, Quaternion.identity);
        }

        private bool SpawnPrefab(SpawnEntry e, int setValue)
        {
            if (!R.IsHost()) { status = "Спавн работает только у хоста / в одиночке"; return false; }
            string path = PrefabPath(e.Prefab);
            if (string.IsNullOrEmpty(path)) { status = "У " + e.Name + " нет ResourcePath"; return false; }
            try
            {
                Vector3 at = FrontOfPlayer(2.5f) + Vector3.up * 0.3f;
                GameObject go = SpawnLikeGame(e.Prefab, path, at);
                if (go == null) { status = "Instantiate вернул null: " + path; return false; }
                if (true)
                {
                    Type vt = R.T("ValuableObject");
                    Component v = vt != null ? go.GetComponent(vt) : null;
                    if (v != null)
                    {
                        if (setValue > 0) SetValue(v, setValue);
                        else R.Call(v, "DollarValueSetLogic");
                    }
                }
                status = "Заспавнено: " + e.Name;
                return true;
            }
            catch (Exception ex)
            {
                status = "Spawn " + e.Name + ": " + ex.Message;
                Log.Add(status + " (path=" + path + ")");
                return false;
            }
        }

        private void SpawnMoneyBag()
        {
            if (valuableList == null || valuableList.Count == 0) BuildValuableList();
            Type mt = R.T("MoneyValuable");
            SpawnEntry found = null;
            if (mt != null)
            {
                foreach (SpawnEntry e in valuableList)
                {
                    GameObject prefab = R.Get(e.Prefab, "Prefab") as GameObject;
                    if (prefab != null && prefab.GetComponentInChildren(mt, true) != null) { found = e; break; }
                }
            }
            if (found == null)
            {
                string[] keys = { "money", "cash", "bag", "coin", "gold", "dollar" };
                foreach (SpawnEntry e in valuableList)
                {
                    foreach (string k in keys)
                        if (e.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) { found = e; break; }
                    if (found != null) break;
                }
            }
            if (found == null)
            {
                status = "Денежный префаб не найден (" + valuableList.Count + " ценностей проверено). Названия в LOG";
                foreach (SpawnEntry e in valuableList) Log.Add("valuable: " + e.Name);
                return;
            }
            int v = 0;
            int.TryParse(valueText, out v);
            SpawnPrefab(found, v);
            status = "Денежная ценность: " + found.Name + (status.StartsWith("Заспавнено") ? "" : " | " + status);
        }

        // ---------- значение ----------
        private void SetValue(object valuable, int val)
        {
            R.Set(valuable, "dollarValueOverride", val);
            R.Set(valuable, "dollarValueOriginal", (float)val);
            R.Set(valuable, "dollarValueCurrent", (float)val);
            R.Set(valuable, "dollarValueSet", true);
            try
            {
                PhotonView pv = R.Get(valuable, "photonView") as PhotonView;
                if (pv != null) pv.RPC("DollarValueSetRPC", RpcTarget.All, (float)val);
            }
            catch (Exception ex) { Log.Add("DollarValueSetRPC: " + ex.Message); }
        }

        private void SetValueGrabbed()
        {
            int val;
            if (!int.TryParse(valueText, out val)) { status = "Неверное число"; return; }
            object grabber = R.GetS("PhysGrabber", "instance");
            Component pgo = R.Get(grabber, "grabbedPhysGrabObject") as Component;
            if (pgo == null) pgo = R.Get(grabber, "currentlyLookingAtPhysGrabObject") as Component;
            Type vt = R.T("ValuableObject");
            Component v = (pgo != null && vt != null) ? pgo.GetComponent(vt) : null;
            if (v == null) { status = "Нет ценности в руках или под прицелом: наведи прицел / возьми, закрой меню и нажми хоткей 'Set value on grabbed' (вкладка KEYS)"; return; }
            SetValue(v, val);
            status = "Значение выставлено: $" + val;
        }

        private void SetValueAll()
        {
            int val;
            if (!int.TryParse(valueText, out val)) { status = "Неверное число"; return; }
            int n = 0;
            foreach (UnityEngine.Object o in R.FindAll("ValuableObject")) { SetValue(o, val); n++; }
            status = "Значение $" + val + " выставлено на " + n + " шт.";
        }

        // ---------- pull / pack / sell ----------
        private List<object> PhysObjects(bool valuables, bool items, float range)
        {
            List<object> res = new List<object>();
            Camera cam = GetCam();
            Vector3 me = cam != null ? cam.transform.position : Vector3.zero;
            if (valuables)
                foreach (UnityEngine.Object o in R.FindAll("ValuableObject"))
                    AddPhys(res, o, me, range);
            if (items)
                foreach (UnityEngine.Object o in R.FindAll("ItemAttributes"))
                    AddPhys(res, o, me, range);
            return res;
        }

        private void AddPhys(List<object> res, UnityEngine.Object o, Vector3 me, float range)
        {
            Component c = o as Component;
            if (c == null) return;
            if (range > 0f && Vector3.Distance(me, c.transform.position) > range) return;
            object pgo = R.Get(o, "physGrabObject");
            if (R.IsAlive(pgo)) res.Add(pgo);
        }

        private void PullItems()
        {
            Camera cam = GetCam();
            if (cam == null) return;
            List<object> list = PhysObjects(cfg.pullValuables, cfg.pullItems, cfg.pullRange);
            Vector3 center = cam.transform.position + cam.transform.forward * 2.5f;
            int n = 0;
            foreach (object pgo in list)
            {
                float a = n * 0.9f;
                Vector3 pos = center + new Vector3(Mathf.Cos(a), 0.2f + 0.25f * (n / 8), Mathf.Sin(a)) * 0.7f;
                R.Call(pgo, "Teleport", pos, Quaternion.identity);
                n++;
            }
            status = "Притянуто: " + n;
        }

        private object ActiveExtraction()
        {
            object rd = R.GetS("RoundDirector", "instance");
            object cur = R.Get(rd, "extractionPointCurrent");
            if (R.IsAlive(cur)) return cur;
            Type ept = R.T("ExtractionPoint");
            if (ept == null) return null;
            foreach (object g in R.List(R.Get(rd, "extractionPointList")))
            {
                GameObject go = g as GameObject;
                if (go == null) continue;
                Component ep = go.GetComponent(ept);
                if (ep != null && !R.B(ep, "isShop")) return ep;
            }
            return null;
        }

        private void PackToExtraction()
        {
            object ep = ActiveExtraction();
            if (ep == null) { status = "Точка эвакуации не найдена"; return; }
            GameObject area = R.Get(ep, "extractionArea") as GameObject;
            Vector3 center = area != null ? area.transform.position : ((Component)ep).transform.position;
            List<object> list = PhysObjects(true, false, 0f);
            int n = 0;
            foreach (object pgo in list)
            {
                Vector3 pos = center + new Vector3((n % 3 - 1) * 0.7f, 0.8f + 0.5f * (n / 9), ((n / 3) % 3 - 1) * 0.7f);
                R.Call(pgo, "Teleport", pos, Quaternion.identity);
                n++;
            }
            status = "Перенесено на эвакуацию: " + n;
        }

        private void RemoteSell()
        {
            if (!R.IsHost()) { status = "Remote sell: только хост / одиночка"; return; }
            object ep = ActiveExtraction();
            if (ep == null) { status = "Точка эвакуации не найдена"; return; }
            object st = R.Get(ep, "currentState");
            string state = st == null ? "" : st.ToString();
            if (state == "Idle") R.Call(ep, "OnClick");
            PackToExtraction();
            status = "Remote sell: состояние точки было " + state + ", ценности перенесены (" + status + ")";
        }
    }
}
