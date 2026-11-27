using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    internal static class Log
    {
        public static readonly List<string> Lines = new List<string>();

        public static void Add(string msg)
        {
            Lines.Add(msg);
            if (Lines.Count > 200) Lines.RemoveAt(0);
            Debug.Log("[REPOMenu] " + msg);
        }
    }
}
