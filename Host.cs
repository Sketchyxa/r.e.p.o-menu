using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    /// <summary>Действия над другими игроками и мобами: доступны только хосту (MasterClient) или в одиночке.</summary>
    public sealed partial class Menu
    {
        private List<UnityEngine.Object> setups;
        private string hostStatus = "";

        private void BuildSetups()
        {
            setups = new List<UnityEngine.Object>();
            object dir = R.GetS("EnemyDirector", "instance");
            string[] lists = { "enemiesDifficulty1", "enemiesDifficulty2", "enemiesDifficulty3" };
            foreach (string l in lists)
                foreach (object s in R.List(R.Get(dir, l)))
                {
                    UnityEngine.Object u = s as UnityEngine.Object;
                    if (u != null && !setups.Contains(u)) setups.Add(u);
                }
            setups.Sort(delegate (UnityEngine.Object a, UnityEngine.Object b) { return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase); });
        }

        private Vector3 NavSnap(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out hit, 20f)) p = hit.point;
            UnityEngine.AI.NavMeshHit nh;
            if (UnityEngine.AI.NavMesh.SamplePosition(p, out nh, 8f, UnityEngine.AI.NavMesh.AllAreas)) p = nh.position;
            return p;
        }

        private void SpawnEnemy(UnityEngine.Object setup)
        {
            object lg = R.GetS("LevelGenerator", "Instance");
            if (lg == null) { hostStatus = "LevelGenerator недоступен (зайди в уровень)"; return; }
            Vector3 pos = NavSnap(FrontOfPlayer(6f));
            HashSet<UnityEngine.Object> before = new HashSet<UnityEngine.Object>(R.FindAll("EnemyParent"));
            R.Call(lg, "EnemySpawn", setup, pos);
            hostStatus = "EnemySpawn вызван: " + setup.name;
            StartCoroutine(PostSpawn(before, setup.name));
        }

        /// <summary>Игра создаёт врага "спящим": ждём SetupDone и будим EnemyParent.Spawn(), как это делает EnemyDirector.</summary>
        private System.Collections.IEnumerator PostSpawn(HashSet<UnityEngine.Object> before, string name)
        {
            for (int i = 0; i < 24; i++)
            {
                yield return new WaitForSeconds(0.25f);
                foreach (UnityEngine.Object o in R.FindAll("EnemyParent"))
                {
                    if (before.Contains(o)) continue;
                    if (!R.B(o, "SetupDone")) continue;
                    R.Call(o, "Spawn");
                    object dir = R.GetS("EnemyDirector", "instance");
                    System.Collections.IList l = R.Get(dir, "enemiesSpawned") as System.Collections.IList;
                    if (l != null && !l.Contains(o)) l.Add(o);
                    hostStatus = "Заспавнен: " + name;
                    yield break;
                }
            }
            hostStatus = "Моб не появился: " + name + " (смотри вкладку LOG)";
        }

        private void PlayerAction(object avatar, string what)
        {
            if (!R.IsHost()) { hostStatus = "Только хост"; return; }
            object ph = R.Get(avatar, "playerHealth");
            string nm = R.S(avatar, "playerName");
            switch (what)
            {
                case "heal":
                    R.Call(ph, "HealOther", Math.Max(1, R.I(ph, "maxHealth", 100)), true);
                    break;
                case "revive":
                    R.Call(avatar, "Revive", false);
                    break;
                case "kill":
                    R.Call(avatar, "PlayerDeath", -1);
                    break;
                case "bring":
                    {
                        Camera cam = GetCam();
                        if (cam != null)
                            R.Call(avatar, "Spawn", cam.transform.position + cam.transform.forward * 1.5f, Quaternion.identity, false);
                        break;
                    }
                case "goto":
                    {
                        Transform tr = R.Get(avatar, "playerTransform") as Transform;
                        object pc = R.LocalController();
                        Component pcc = pc as Component;
                        if (tr != null && pcc != null)
                        {
                            Vector3 p = tr.position + Vector3.up * 0.5f;
                            Rigidbody rb = R.Get(pc, "rb") as Rigidbody;
                            pcc.transform.position = p;
                            if (rb != null) rb.position = p;
                        }
                        break;
                    }
            }
            hostStatus = what + ": " + nm;
        }
    }
}
