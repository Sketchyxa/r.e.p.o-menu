using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace r.e.p.o_cheat
{
    /// <summary>Доступ к типам игры через рефлексию: у части полей REPO модификатор internal, а имена лучше проверять в рантайме.</summary>
    internal static class R
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private static readonly Dictionary<string, Type> types = new Dictionary<string, Type>();
        private static readonly Dictionary<string, MemberInfo> members = new Dictionary<string, MemberInfo>();
        private static readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
        private static readonly HashSet<string> warned = new HashSet<string>();

        public static Type T(string name)
        {
            Type t;
            if (types.TryGetValue(name, out t)) return t;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                string an = a.GetName().Name;
                if (an != "Assembly-CSharp" && an != "Assembly-CSharp-firstpass") continue;
                try { t = a.GetType(name, false); } catch { t = null; }
                if (t != null) break;
            }
            if (t == null) Warn("type:" + name, "Тип не найден: " + name);
            types[name] = t;
            return t;
        }

        private static void Warn(string key, string msg)
        {
            if (warned.Add(key)) Log.Add(msg);
        }

        private static MemberInfo Member(Type t, string name)
        {
            string key = t.FullName + "." + name;
            MemberInfo m;
            if (members.TryGetValue(key, out m)) return m;
            for (Type c = t; c != null && m == null; c = c.BaseType)
            {
                m = (MemberInfo)c.GetField(name, All | BindingFlags.DeclaredOnly) ?? c.GetProperty(name, All | BindingFlags.DeclaredOnly);
            }
            if (m == null) Warn(key, "Член не найден: " + key);
            members[key] = m;
            return m;
        }

        // ---------- чтение ----------
        public static object Get(object o, string name)
        {
            if (o == null) return null;
            try
            {
                MemberInfo m = Member(o.GetType(), name);
                FieldInfo f = m as FieldInfo;
                if (f != null) return f.GetValue(f.IsStatic ? null : o);
                PropertyInfo p = m as PropertyInfo;
                if (p != null) return p.GetValue(o, null);
            }
            catch (Exception e) { Warn("get:" + name + e.GetType().Name, "Get " + name + ": " + e.Message); }
            return null;
        }

        public static object GetS(Type t, string name)
        {
            if (t == null) return null;
            try
            {
                MemberInfo m = Member(t, name);
                FieldInfo f = m as FieldInfo;
                if (f != null) return f.GetValue(null);
                PropertyInfo p = m as PropertyInfo;
                if (p != null) return p.GetValue(null, null);
            }
            catch (Exception e) { Warn("gets:" + name + e.GetType().Name, "GetS " + name + ": " + e.Message); }
            return null;
        }

        public static object GetS(string typeName, string name) { return GetS(T(typeName), name); }

        public static float F(object o, string name, float def = 0f)
        {
            object v = Get(o, name);
            if (v == null) return def;
            try { return Convert.ToSingle(v); } catch { return def; }
        }

        public static int I(object o, string name, int def = 0)
        {
            object v = Get(o, name);
            if (v == null) return def;
            try { return Convert.ToInt32(v); } catch { return def; }
        }

        public static bool B(object o, string name, bool def = false)
        {
            object v = Get(o, name);
            if (v is bool) return (bool)v;
            return def;
        }

        public static string S(object o, string name)
        {
            object v = Get(o, name);
            return v == null ? null : v.ToString();
        }

        // ---------- запись ----------
        public static bool Set(object o, string name, object value)
        {
            if (o == null) return false;
            try
            {
                MemberInfo m = Member(o.GetType(), name);
                FieldInfo f = m as FieldInfo;
                if (f != null) { f.SetValue(f.IsStatic ? null : o, Conv(value, f.FieldType)); return true; }
                PropertyInfo p = m as PropertyInfo;
                if (p != null && p.CanWrite) { p.SetValue(o, Conv(value, p.PropertyType), null); return true; }
            }
            catch (Exception e) { Warn("set:" + name + e.GetType().Name, "Set " + name + ": " + e.Message); }
            return false;
        }

        private static object Conv(object v, Type to)
        {
            if (v == null) return null;
            if (to.IsInstanceOfType(v)) return v;
            if (to.IsEnum)
            {
                string s = v as string;
                return s != null ? Enum.Parse(to, s, true) : Enum.ToObject(to, v);
            }
            return Convert.ChangeType(v, to);
        }

        // ---------- вызов методов ----------
        public static object Call(object o, string name, params object[] args)
        {
            if (o == null) return null;
            return Invoke(o.GetType(), o, name, args);
        }

        public static object CallS(string typeName, string name, params object[] args)
        {
            Type t = T(typeName);
            if (t == null) return null;
            return Invoke(t, null, name, args);
        }

        private static object Invoke(Type t, object target, string name, object[] args)
        {
            try
            {
                string key = t.FullName + "." + name + "/" + args.Length;
                MethodInfo mi;
                if (!methods.TryGetValue(key, out mi))
                {
                    for (Type c = t; c != null && mi == null; c = c.BaseType)
                    {
                        foreach (MethodInfo cand in c.GetMethods(All | BindingFlags.DeclaredOnly))
                        {
                            if (cand.Name != name) continue;
                            ParameterInfo[] ps = cand.GetParameters();
                            if (ps.Length == args.Length) { mi = cand; break; }
                        }
                    }
                    if (mi == null) Warn(key, "Метод не найден: " + key);
                    methods[key] = mi;
                }
                if (mi == null) return null;
                ParameterInfo[] pi = mi.GetParameters();
                object[] conv = new object[args.Length];
                for (int i = 0; i < args.Length; i++) conv[i] = Conv(args[i], pi[i].ParameterType);
                return mi.Invoke(mi.IsStatic ? null : target, conv);
            }
            catch (TargetInvocationException e)
            {
                Warn("inv:" + name + e.InnerException, name + " бросил: " + (e.InnerException != null ? e.InnerException.Message : e.Message));
            }
            catch (Exception e) { Warn("inv2:" + name + e.GetType().Name, name + ": " + e.Message); }
            return null;
        }

        // ---------- утилиты ----------
        public static List<object> List(object enumerable)
        {
            List<object> r = new List<object>();
            IEnumerable e = enumerable as IEnumerable;
            if (e == null) return r;
            foreach (object x in e) if (x != null) r.Add(x);
            return r;
        }

        public static UnityEngine.Object[] FindAll(string typeName)
        {
            Type t = T(typeName);
            if (t == null) return new UnityEngine.Object[0];
            try { return UnityEngine.Object.FindObjectsOfType(t); }
            catch (Exception e) { Warn("find:" + typeName, "Find " + typeName + ": " + e.Message); return new UnityEngine.Object[0]; }
        }

        public static Component Comp(object o)
        {
            return o as Component;
        }

        public static bool IsAlive(object o)
        {
            UnityEngine.Object u = o as UnityEngine.Object;
            return u != null;
        }

        /// <summary>true в одиночке и у хоста.</summary>
        public static bool IsHost()
        {
            object v = CallS("SemiFunc", "IsMasterClientOrSingleplayer");
            return v is bool && (bool)v;
        }

        public static bool IsMultiplayer()
        {
            object v = CallS("SemiFunc", "IsMultiplayer");
            return v is bool && (bool)v;
        }

        public static object LocalAvatar()
        {
            object a = CallS("SemiFunc", "PlayerAvatarLocal");
            if (!IsAlive(a)) a = GetS("PlayerAvatar", "instance");
            return IsAlive(a) ? a : null;
        }

        public static object LocalController()
        {
            object c = GetS("PlayerController", "instance");
            return IsAlive(c) ? c : null;
        }
    }
}
